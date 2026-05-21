namespace ReVitae.Core.Import.Extraction;

/// <summary>WPS binaries have no dependable cross‑platform extractor in dotnet today.</summary>
public sealed class WpsTextExtractor : ICvTextExtractor
{
    public CvTextExtractionResult Extract(string filePath)
    {
        if (ImportExtractorGuards.TryRejectMissing(filePath, out var failure))
        {
            return failure;
        }

        return new CvTextExtractionResult(false, string.Empty, ReVitae.Core.Localization.TranslationKeys.ImportErrorUnsupportedFormat);
    }
}
