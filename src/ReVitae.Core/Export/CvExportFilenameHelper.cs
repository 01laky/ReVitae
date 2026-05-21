namespace ReVitae.Core.Export;

using System.IO;
using System.Text;

public static class CvExportFilenameHelper
{
    public static string SuggestFilename(string? firstName, string? lastName)
    {
        var sanitizedFirstName = SanitizePart(firstName);
        var sanitizedLastName = SanitizePart(lastName);

        if (string.IsNullOrWhiteSpace(sanitizedFirstName) || string.IsNullOrWhiteSpace(sanitizedLastName))
        {
            return "ReVitae_CV.pdf";
        }

        return $"{sanitizedFirstName}_{sanitizedLastName}_CV.pdf";
    }

    private static string SanitizePart(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var invalidChars = Path.GetInvalidFileNameChars().ToHashSet();
        var builder = new StringBuilder(value.Trim().Length);

        foreach (var character in value.Trim())
        {
            if (char.IsWhiteSpace(character))
            {
                builder.Append('_');
                continue;
            }

            builder.Append(invalidChars.Contains(character) ? '_' : character);
        }

        return builder.ToString().Trim('_', '.');
    }
}
