namespace ADExplorer.Models;

public class AdUser
{
    public string DisplayName { get; set; } = string.Empty;
    public string SamAccountName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Manager { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string DistinguishedName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool IsLockedOut { get; set; }
    public int BadPasswordCount { get; set; }
    public DateTime? LastLogon { get; set; }
    public DateTime? PasswordExpiryDate { get; set; }
    public DateTime? Created { get; set; }
    public List<string> Groups { get; set; } = new();

    public string LastLogonDisplay =>
        LastLogon.HasValue ? LastLogon.Value.ToString("yyyy-MM-dd HH:mm") : "Never";

    /// <summary>
    /// Whole days until the password expires (negative once expired), or null if it never expires.
    /// </summary>
    public int? PasswordExpiryDays(DateTime? referenceDate = null)
    {
        if (!PasswordExpiryDate.HasValue) return null;
        var reference = referenceDate ?? DateTime.Now;
        return (int)Math.Floor((PasswordExpiryDate.Value - reference).TotalDays);
    }

    public string PasswordExpiryDisplay(DateTime? referenceDate = null)
    {
        var days = PasswordExpiryDays(referenceDate);
        if (days is not int d) return "No expiry";

        var date = PasswordExpiryDate!.Value.ToString("yyyy-MM-dd");
        return d switch
        {
            < 0  => $"Expired ({date})",
            0    => $"Expires in under a day ({date})",
            1    => $"1 day ({date})",
            _    => $"{d} days ({date})",
        };
    }

    public string StatusDisplay => IsEnabled ? "✅ Enabled" : "❌ Disabled";
    public string LockedDisplay => IsLockedOut ? "⚠️ Yes" : "No";
}
