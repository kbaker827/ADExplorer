namespace ADExplorer.Models;

public class AdComputer
{
    public string Name { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string OperatingSystemVersion { get; set; } = string.Empty;
    public string DistinguishedName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public DateTime? LastLogon { get; set; }

    public string LastLogonDisplay =>
        LastLogon.HasValue ? LastLogon.Value.ToString("yyyy-MM-dd HH:mm") : "Never";

    public string StatusDisplay => IsEnabled ? "✅ Enabled" : "❌ Disabled";
}
