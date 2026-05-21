using ReVitae.Core.Import.Importers;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Import;

/// <summary>Imports CV files regardless of MIME type (routing, limits, standardized errors).</summary>
public static class CvDocumentImporter
{
    public static CvImportResult Import(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return CvImportResult.Failed(TranslationKeys.ImportErrorFileNotFound);
        }

        long payloadLength;

        try
        {
            payloadLength = new FileInfo(filePath).Length;
        }
        catch (Exception)
        {
            return CvImportResult.Failed(TranslationKeys.ImportErrorUnreadableDocument);
        }

        if (payloadLength > CvImportLimits.MaxFileBytes)
        {
            return CvImportResult.Failed(TranslationKeys.ImportErrorFileTooLarge);
        }

        var formatGuess = CvImportFormatDetector.DetectFormat(filePath);
        var driver = CvFormatImporterRegistry.Get(formatGuess);
        if (driver is null)
        {
            return CvImportResult.Failed(TranslationKeys.ImportErrorUnsupportedFormat);
        }

        CvImportResult response;

        try
        {
            response = driver.Import(filePath);
        }
        catch (Exception)
        {
            response = CvImportResult.Failed(TranslationKeys.ImportErrorUnreadableDocument);
        }

        if (!response.Success)
        {
            return CvImportResult.Failed(NormalizeKey(response.ErrorMessageKey));
        }

        return response;
    }

    private static string NormalizeKey(string? key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return TranslationKeys.ImportErrorUnreadableDocument;
        }

        if (string.Equals(key, TranslationKeys.ImportErrorEmptyPdf, StringComparison.Ordinal))
        {
            return TranslationKeys.ImportErrorEmptyDocument;
        }

        if (string.Equals(key, TranslationKeys.ImportErrorUnreadablePdf, StringComparison.Ordinal))
        {
            return TranslationKeys.ImportErrorUnreadableDocument;
        }

        return key;
    }
}
