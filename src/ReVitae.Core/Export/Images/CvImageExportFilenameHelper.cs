using System.IO;
using System.Text;
using ReVitae.Core.Export;

namespace ReVitae.Core.Export.Images;

public static class CvImageExportFilenameHelper
{
    public static string SuggestImageZipFilename(string? firstName, string? lastName) =>
        $"{BuildNamePrefix(firstName, lastName)}_images.zip";

    public static string SuggestImagePageFilename(
        string? firstName,
        string? lastName,
        int pageIndex,
        CvImageExportFormat format)
    {
        var prefix = BuildNamePrefix(firstName, lastName);
        var pageToken = FormatPageIndex(pageIndex);
        return $"{prefix}_page-{pageToken}{CvImageEncoder.GetFileExtension(format)}";
    }

    public static string FormatZipEntryName(int pageIndex, CvImageExportFormat format) =>
        $"page-{FormatPageIndex(pageIndex)}{CvImageEncoder.GetFileExtension(format)}";

    public static string ResolveCollisionSafePath(string directory, string filename)
    {
        var candidate = Path.Combine(directory, filename);
        if (!File.Exists(candidate))
        {
            return candidate;
        }

        var extension = Path.GetExtension(filename);
        var baseName = Path.GetFileNameWithoutExtension(filename);
        var suffix = 2;

        while (true)
        {
            var nextName = $"{baseName}-{suffix}{extension}";
            candidate = Path.Combine(directory, nextName);
            if (!File.Exists(candidate))
            {
                return candidate;
            }

            suffix++;
        }
    }

    private static string BuildNamePrefix(string? firstName, string? lastName)
    {
        var sanitizedFirst = SanitizePart(firstName);
        var sanitizedLast = SanitizePart(lastName);

        if (string.IsNullOrWhiteSpace(sanitizedFirst) || string.IsNullOrWhiteSpace(sanitizedLast))
        {
            return "ReVitae_CV";
        }

        return $"{sanitizedFirst}_{sanitizedLast}_CV";
    }

    private static string FormatPageIndex(int pageIndex)
    {
        if (pageIndex >= 100)
        {
            return pageIndex.ToString("000", System.Globalization.CultureInfo.InvariantCulture);
        }

        return pageIndex.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
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
