namespace ReVitae.Core.Cv.Links;

public static class LinkLabelSuggestions
{
    public static IReadOnlyList<string> All { get; } =
    [
        "Behance",
        "Dribbble",
        "Medium",
        "Dev.to",
        "ORCID",
        "Google Scholar",
        "Stack Overflow",
        "ResearchGate",
        "GitLab",
        "Bitbucket",
        "YouTube",
        "Twitter / X",
        "Mastodon",
        "Personal blog"
    ];

    public static IReadOnlyList<string> Filter(string? query, int maxResults = 12)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return All.Take(maxResults).ToArray();
        }

        return All
            .Where(label => label.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase))
            .Take(maxResults)
            .ToArray();
    }
}
