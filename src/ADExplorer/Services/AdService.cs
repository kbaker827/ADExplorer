using ADExplorer.Models;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.Text;

namespace ADExplorer.Services;

public class AdService
{
    public const int MaxResults = 200;

    private const int UacAccountDisabled = 0x2;
    private const int UacLockout = 0x10;
    private const int UacPasswordNeverExpires = 0x10000;

    private static readonly string[] UserProperties =
    {
        "displayName", "sAMAccountName", "mail", "title", "department", "manager",
        "telephoneNumber", "distinguishedName", "description", "userAccountControl",
        "badPwdCount", "lastLogon", "lastLogonTimestamp", "whenCreated", "memberOf",
        "lockoutTime",
    };

    // Constructed attributes: only returned by a base-scope search of a single object.
    private static readonly string[] UserDetailProperties = UserProperties
        .Concat(new[] { "msDS-User-Account-Control-Computed", "msDS-UserPasswordExpiryTimeComputed" })
        .ToArray();

    private static readonly string[] GroupProperties =
    {
        "name", "description", "distinguishedName", "groupType", "member",
    };

    private static readonly string[] ComputerProperties =
    {
        "name", "operatingSystem", "operatingSystemVersion", "distinguishedName",
        "description", "userAccountControl", "lastLogon", "lastLogonTimestamp",
    };

    private readonly string _ldapRoot;
    private readonly string _domainName;

    public AdService()
    {
        try
        {
            using var domain = Domain.GetCurrentDomain();
            _ldapRoot = $"LDAP://{domain.Name}";
            _domainName = domain.Name;
            IsConnected = true;
        }
        catch
        {
            _ldapRoot = "LDAP://";
            _domainName = "Not connected";
        }
    }

    public string DomainName => _domainName;
    public bool IsConnected { get; }

    // ── User Search ────────────────────────────────────────────────

    public List<AdUser> SearchUsers(string query)
    {
        var q = EscapeLdap(query);
        var filter = $"(&(objectCategory=person)(objectClass=user)" +
                     $"(|(displayName=*{q}*)(sAMAccountName=*{q}*)(mail=*{q}*)(userPrincipalName=*{q}*)))";
        return SearchDirectory(filter, UserProperties, BuildUser)
            .OrderBy(u => u.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Re-reads a single user including constructed attributes, which give an accurate
    /// lockout state and password expiry (honours fine-grained password policies).
    /// Returns null if the user can no longer be read.
    /// </summary>
    public AdUser? GetUserDetails(string distinguishedName)
    {
        if (string.IsNullOrEmpty(distinguishedName)) return null;
        try
        {
            using var entry = new DirectoryEntry($"LDAP://{EscapeAdsPath(distinguishedName)}");
            using var searcher = new DirectorySearcher(entry, "(objectClass=*)", UserDetailProperties, SearchScope.Base);
            var result = searcher.FindOne();
            return result == null ? null : BuildUser(result);
        }
        catch { return null; }
    }

    // ── Group Search ───────────────────────────────────────────────

    public List<AdGroup> SearchGroups(string query)
    {
        var q = EscapeLdap(query);
        var filter = $"(&(objectClass=group)(|(name=*{q}*)(sAMAccountName=*{q}*)))";
        return SearchDirectory(filter, GroupProperties, BuildGroup)
            .OrderBy(g => g.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    // ── Computer Search ────────────────────────────────────────────

    public List<AdComputer> SearchComputers(string query)
    {
        var q = EscapeLdap(query);
        var filter = $"(&(objectCategory=computer)(name=*{q}*))";
        return SearchDirectory(filter, ComputerProperties, BuildComputer)
            .OrderBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    // ── Helpers ────────────────────────────────────────────────────

    public static string EscapeLdap(string value)
    {
        // RFC 4515: escape \, *, (, ), NUL in LDAP filter values (backslash first)
        return value
            .Replace("\\", "\\5c")
            .Replace("*",  "\\2a")
            .Replace("(",  "\\28")
            .Replace(")",  "\\29")
            .Replace("\0", "\\00");
    }

    /// <summary>A forward slash is the ADsPath separator, so it must be escaped inside a DN.</summary>
    public static string EscapeAdsPath(string distinguishedName) =>
        distinguishedName.Replace("/", "\\/");

    /// <summary>
    /// Runs an LDAP search. Throws on directory errors so the caller can tell the user,
    /// rather than silently showing "0 results".
    /// </summary>
    private List<T> SearchDirectory<T>(string filter, string[] properties, Func<SearchResult, T> builder)
    {
        if (!IsConnected)
            throw new InvalidOperationException(
                "This computer is not connected to an Active Directory domain.");

        using var root = new DirectoryEntry(_ldapRoot);
        using var searcher = new DirectorySearcher(root, filter, properties)
        {
            SizeLimit = MaxResults,
        };
        using var results = searcher.FindAll();

        var list = new List<T>();
        foreach (SearchResult result in results)
            list.Add(builder(result));
        return list;
    }

    private static AdUser BuildUser(SearchResult r)
    {
        var uac = GetInt(r, "userAccountControl");
        var computedUac = GetInt(r, "msDS-User-Account-Control-Computed");

        // The lockout bit in userAccountControl is not maintained by AD; the constructed
        // attribute is authoritative. Fall back to lockoutTime for search results.
        var isLockedOut = HasProperty(r, "msDS-User-Account-Control-Computed")
            ? (computedUac & UacLockout) != 0
            : GetLong(r, "lockoutTime") > 0;

        DateTime? pwdExpiry = null;
        if ((uac & UacPasswordNeverExpires) == 0)
            pwdExpiry = ParseFileTime(GetLong(r, "msDS-UserPasswordExpiryTimeComputed"));

        return new AdUser
        {
            DisplayName       = GetString(r, "displayName"),
            SamAccountName    = GetString(r, "sAMAccountName"),
            Email             = GetString(r, "mail"),
            Title             = GetString(r, "title"),
            Department        = GetString(r, "department"),
            Manager           = ParseCN(GetString(r, "manager")),
            Phone             = GetString(r, "telephoneNumber"),
            DistinguishedName = GetString(r, "distinguishedName"),
            Description       = GetString(r, "description"),
            IsEnabled         = (uac & UacAccountDisabled) == 0,
            IsLockedOut       = isLockedOut,
            BadPasswordCount  = GetInt(r, "badPwdCount"),
            LastLogon         = GetLastLogon(r),
            PasswordExpiryDate = pwdExpiry,
            Created           = GetDate(r, "whenCreated"),
            Groups            = GetStrings(r, "memberOf").Select(ParseCN)
                                    .OrderBy(g => g, StringComparer.CurrentCultureIgnoreCase)
                                    .ToList(),
        };
    }

    private static AdGroup BuildGroup(SearchResult r) => new()
    {
        Name              = GetString(r, "name"),
        Description       = GetString(r, "description"),
        DistinguishedName = GetString(r, "distinguishedName"),
        GroupType         = ParseGroupType(GetInt(r, "groupType")),
        Members           = GetStrings(r, "member").Select(ParseCN)
                                .OrderBy(m => m, StringComparer.CurrentCultureIgnoreCase)
                                .ToList(),
    };

    private static AdComputer BuildComputer(SearchResult r) => new()
    {
        Name                   = GetString(r, "name"),
        OperatingSystem        = GetString(r, "operatingSystem"),
        OperatingSystemVersion = GetString(r, "operatingSystemVersion"),
        DistinguishedName      = GetString(r, "distinguishedName"),
        Description            = GetString(r, "description"),
        IsEnabled              = (GetInt(r, "userAccountControl") & UacAccountDisabled) == 0,
        LastLogon              = GetLastLogon(r),
    };

    // lastLogon is per-DC and not replicated; lastLogonTimestamp is replicated but can lag
    // by up to ~14 days. Use whichever is newer.
    private static DateTime? GetLastLogon(SearchResult r) =>
        ParseFileTime(Math.Max(GetLong(r, "lastLogon"), GetLong(r, "lastLogonTimestamp")));

    private static bool HasProperty(SearchResult r, string attr) =>
        r.Properties[attr].Count > 0;

    private static object? GetValue(SearchResult r, string attr)
    {
        var values = r.Properties[attr];
        return values.Count > 0 ? values[0] : null;
    }

    private static string GetString(SearchResult r, string attr) =>
        GetValue(r, attr)?.ToString() ?? string.Empty;

    private static IEnumerable<string> GetStrings(SearchResult r, string attr) =>
        r.Properties[attr].Cast<object>().Select(v => v.ToString() ?? string.Empty);

    private static int GetInt(SearchResult r, string attr) =>
        GetValue(r, attr) is int v ? v : 0;

    // DirectorySearcher returns large-integer attributes (FILETIMEs) as Int64.
    private static long GetLong(SearchResult r, string attr) =>
        GetValue(r, attr) switch
        {
            long l => l,
            int i  => i,
            _      => 0,
        };

    private static DateTime? GetDate(SearchResult r, string attr) =>
        GetValue(r, attr) is DateTime d
            ? DateTime.SpecifyKind(d, DateTimeKind.Utc).ToLocalTime()
            : null;

    public static DateTime? ParseFileTime(long fileTime)
    {
        if (fileTime <= 0 || fileTime == long.MaxValue) return null;
        try
        {
            return DateTime.FromFileTime(fileTime);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    public static string ParseGroupType(int groupType) =>
        groupType < 0 ? "Security" : "Distribution";

    /// <summary>
    /// Returns the unescaped value of the first RDN of a distinguished name, e.g.
    /// "CN=Smith\, John,OU=Staff,DC=corp" → "Smith, John". Returns the input if it has
    /// no attribute=value form.
    /// </summary>
    public static string ParseCN(string dn)
    {
        if (string.IsNullOrEmpty(dn)) return string.Empty;

        var eq = dn.IndexOf('=');
        if (eq < 0) return dn;

        var sb = new StringBuilder();
        var pendingBytes = new List<byte>();

        void FlushBytes()
        {
            if (pendingBytes.Count == 0) return;
            sb.Append(Encoding.UTF8.GetString(pendingBytes.ToArray()));
            pendingBytes.Clear();
        }

        for (var i = eq + 1; i < dn.Length; i++)
        {
            var c = dn[i];
            if (c == '\\' && i + 1 < dn.Length)
            {
                // RFC 4514: "\XX" is a hex-encoded UTF-8 byte; "\X" escapes a special char.
                if (i + 2 < dn.Length && Uri.IsHexDigit(dn[i + 1]) && Uri.IsHexDigit(dn[i + 2]))
                {
                    pendingBytes.Add(Convert.ToByte(dn.Substring(i + 1, 2), 16));
                    i += 2;
                    continue;
                }
                FlushBytes();
                sb.Append(dn[++i]);
                continue;
            }

            FlushBytes();
            if (c == ',' || c == '+') break;
            sb.Append(c);
        }
        FlushBytes();
        return sb.ToString().Trim();
    }
}
