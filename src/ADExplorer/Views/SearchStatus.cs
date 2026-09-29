using ADExplorer.Services;

namespace ADExplorer.Views;

/// <summary>Shared status messages for the search views' results pane.</summary>
internal static class SearchStatus
{
    public const string Prompt = "Enter a search term above.";
    public const string Searching = "Searching…";

    public static string ForResults(int count) => count switch
    {
        0 => "No matches found.",
        >= AdService.MaxResults => $"Showing the first {AdService.MaxResults} matches. Refine your search to narrow the list.",
        _ => string.Empty,
    };

    public static string ForError(Exception ex) => $"Search failed: {ex.Message}";
}
