using ADExplorer.Models;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;

namespace ADExplorer.Services;

public class AdService
{
    private readonly string _ldapRoot;
    private readonly string _domainName;

    public AdService()
    {
        try
        {
            var domain = Domain.GetCurrentDomain();
            _ldapRoot = $"LDAP://{domain.Name}";
            _domainName = domain.Name;
        }
        catch
        {
            _ldapRoot = "LDAP://";
            _domainName = "Not connected";
        }
    }

    public string DomainName => _domainName;

    // ── User Search ────────────────────────────────────────────────

    public List<AdUser> SearchUsers(string query)
    {
        var q = EscapeLdap(query);
        var filter = $"(&(objectClass=user)(objectCategory=person)" +
                     $"(|(displayName=*{q}*)(sAMAccountName=*{q}*)(mail=*{q}*)))";
        return SearchDirectory<AdUser>(filter, BuildUser);
    }

    public AdUser? GetUserDetails(string distinguishedName)
    {
        try
        {
            using var entry = new DirectoryEntry($"LDAP://{distinguishedName}");
            return BuildUser(entry);
        }
        catch { return null; }
    }

    // ── Group Search ───────────────────────────────────────────────

    public List<AdGroup> SearchGroups(string query)
    {
        var q = EscapeLdap(query);
        var filter = $"(&(objectClass=group)(name=*{q}*))";
        return SearchDirectory<AdGroup>(filter, BuildGroup);
    }

    // ── Computer Search ────────────────────────────────────────────

    public List<AdComputer> SearchComputers(string query)
    {
        var q = EscapeLdap(query);
        var filter = $"(&(objectClass=computer)(name=*{q}*))";
        return SearchDirectory<AdComputer>(filter, BuildComputer);
    }

    // ── Private helpers ────────────────────────────────────────────

    public static string EscapeLdap(string value)
    {
        // RFC 4515: escape *, (, ), \, NUL in LDAP filter values
        return value
            .Replace("\\", "\\5c")
            .Replace("*",  "\\2a")
            .Replace("(",  "\\28")
            .Replace(")",  "\\29")
            .Replace("\0", "\\00");
    }

    private List<T> SearchDirectory<T>(string filter, Func<DirectoryEntry, T> builder)
    {
        var results = new List<T>();
        try
        {
            using var root = new DirectoryEntry(_ldapRoot);
            using var searcher = new DirectorySearcher(root, filter) { SizeLimit = 200 };
            foreach (SearchResult result in searcher.FindAll())
                results.Add(builder(result.GetDirectoryEntry()));
        }
        catch { /* return empty on LDAP failure */ }
        return results;
    }

    private static AdUser BuildUser(DirectoryEntry e)
    {
        var uac = GetInt(e, "userAccountControl");
        var isDisabled = (uac & 0x2) != 0;
        var isLockedOut = (uac & 0x10) != 0;

        var maxPwdAge = GetLong(e, "maxPwdAge");
        var pwdLastSet = GetLong(e, "pwdLastSet");
        DateTime? pwdExpiry = null;
        if (pwdLastSet > 0 && maxPwdAge < 0)
        {
            var setDate = DateTime.FromFileTime(pwdLastSet);
            var maxAge = TimeSpan.FromTicks(Math.Abs(maxPwdAge));
            pwdExpiry = setDate + maxAge;
        }

        var groups = e.Properties["memberOf"]
            .Cast<string>()
            .Select(ParseCN)
            .ToList();

        return new AdUser
        {
            DisplayName    = GetString(e, "displayName"),
            SamAccountName = GetString(e, "sAMAccountName"),
            Email          = GetString(e, "mail"),
            Title          = GetString(e, "title"),
            Department     = GetString(e, "department"),
            Manager        = ParseCN(GetString(e, "manager")),
            Phone          = GetString(e, "telephoneNumber"),
            DistinguishedName = GetString(e, "distinguishedName"),
            Description    = GetString(e, "description"),
            IsEnabled      = !isDisabled,
            IsLockedOut    = isLockedOut,
            BadPasswordCount = GetInt(e, "badPwdCount"),
            LastLogon      = ParseFileTime(GetLong(e, "lastLogon")),
            PasswordExpiryDate = pwdExpiry,
            Created        = e.Properties["whenCreated"].Value as DateTime?,
            Groups         = groups,
        };
    }

    private static AdGroup BuildGroup(DirectoryEntry e)
    {
        var members = e.Properties["member"]
            .Cast<string>()
            .Select(ParseCN)
            .ToList();

        return new AdGroup
        {
            Name              = GetString(e, "name"),
            Description       = GetString(e, "description"),
            DistinguishedName = GetString(e, "distinguishedName"),
            GroupType         = ParseGroupType(GetInt(e, "groupType")),
            Members           = members,
        };
    }

    private static AdComputer BuildComputer(DirectoryEntry e)
    {
        var uac = GetInt(e, "userAccountControl");
        return new AdComputer
        {
            Name                   = GetString(e, "name"),
            OperatingSystem        = GetString(e, "operatingSystem"),
            OperatingSystemVersion = GetString(e, "operatingSystemVersion"),
            DistinguishedName      = GetString(e, "distinguishedName"),
            Description            = GetString(e, "description"),
            IsEnabled              = (uac & 0x2) == 0,
            LastLogon              = ParseFileTime(GetLong(e, "lastLogon")),
        };
    }

    private static string GetString(DirectoryEntry e, string attr) =>
        e.Properties[attr].Value?.ToString() ?? string.Empty;

    private static int GetInt(DirectoryEntry e, string attr) =>
        e.Properties[attr].Value is int v ? v : 0;

    private static long GetLong(DirectoryEntry e, string attr) =>
        e.Properties[attr].Value is long v ? v : 0;

    public static DateTime? ParseFileTime(long fileTime)
    {
        if (fileTime <= 0 || fileTime == long.MaxValue) return null;
        return DateTime.FromFileTime(fileTime);
    }

    public static string ParseGroupType(int groupType) =>
        groupType < 0 ? "Security" : "Distribution";

    private static string ParseCN(string dn)
    {
        if (string.IsNullOrEmpty(dn)) return string.Empty;
        var cn = dn.Split(',').FirstOrDefault(p => p.TrimStart().StartsWith("CN="));
        return cn?.Substring(3).Trim() ?? dn;
    }
}
