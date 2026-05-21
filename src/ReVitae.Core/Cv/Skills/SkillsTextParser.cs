namespace ReVitae.Core.Cv.Skills;

public static class SkillsTextParser
{
    public static IReadOnlyList<string> ParseSkillNames(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<string>();
        }

        return text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split([',', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Trim())
            .Where(item => item.Length > 0)
            .ToArray();
    }
}
