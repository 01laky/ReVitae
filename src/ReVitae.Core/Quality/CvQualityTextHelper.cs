namespace ReVitae.Core.Quality;

internal static class CvQualityTextHelper
{
    public static int CountNonWhitespace(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        var count = 0;
        foreach (var character in text)
        {
            if (!char.IsWhiteSpace(character))
            {
                count++;
            }
        }

        return count;
    }

    public static bool HasText(string? text) => CountNonWhitespace(text) > 0;
}
