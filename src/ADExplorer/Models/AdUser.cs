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

    public int PasswordExpiryDays =>
        PasswordExpiryDate.HasValue
            ? (int)(PasswordExpiryDate.Value - DateTime.Now).TotalDays
            : -1;

    public string StatusDisplay => IsEnabled ? "✅ Enabled" : "❌ Disabled";
    public string LockedDisplay => IsLockedOut ? "⚠️ Yes" : "No";
}
