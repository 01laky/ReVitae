namespace ReVitae.Core.Cv.ProfilePhoto;

public static class ProfilePhotoFormats
{
    public const long MaxFileSizeBytes = 15L * 1024 * 1024;

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    public static bool IsSupportedExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return false;
        }

        var normalized = extension.StartsWith('.') ? extension : "." + extension;
        return SupportedExtensions.Contains(normalized);
    }

    public static bool TryGetExtensionForContentType(string? contentType, out string extension)
    {
        extension = contentType?.ToLowerInvariant() switch
        {
            "image/jpeg" or "image/jpg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => string.Empty
        };

        return extension.Length > 0;
    }

    public static string GetContentTypeForExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    public static bool ShouldTranscodeWebpForExport(string extension)
    {
        return extension.Equals(".webp", StringComparison.OrdinalIgnoreCase);
    }
}
