namespace ADExplorer.Models;

public class AdGroup
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DistinguishedName { get; set; } = string.Empty;
    public string GroupType { get; set; } = string.Empty;
    public List<string> Members { get; set; } = new();
    public int MemberCount => Members.Count;
}
