using System.Text.RegularExpressions;

namespace ReVitae.Core.Quality;

internal static class GenericWorkDescriptionHeuristic
{
    private static readonly string[] StrongVerbs =
    [
        "increased",
        "reduced",
        "delivered",
        "led",
        "built",
        "improved",
        "achieved",
        "implemented",
        "designed",
        "optimized",
        "migrated",
        "automated",
        "scaled",
        "created",
        "launched",
        "managed"
    ];

    public static bool IsGeneric(string? description)
    {
        if (CvQualityTextHelper.CountNonWhitespace(description) <= 40)
        {
            return false;
        }

        if (description is null || description.Any(char.IsDigit))
        {
            return false;
        }

        if (description.Contains('%', StringComparison.Ordinal))
        {
            return false;
        }

        foreach (var verb in StrongVerbs)
        {
            if (ContainsWholeWord(description, verb))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ContainsWholeWord(string text, string word)
    {
        return Regex.IsMatch(text, $@"\b{Regex.Escape(word)}\b", RegexOptions.IgnoreCase);
    }
}
