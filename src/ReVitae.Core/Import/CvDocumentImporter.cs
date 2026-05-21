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
        CvImportDiagnosticsLogger.BeginSession(filePath, formatGuess, payloadLength);
        CvImportDiagnosticsLogger.LogStep("router", $"Detected format: {formatGuess}");

        var driver = CvFormatImporterRegistry.Get(formatGuess);
        if (driver is null)
        {
            CvImportDiagnosticsLogger.LogFailure("format detection", "Unsupported format.");
            CvImportDiagnosticsLogger.EndSession(success: false);
            return CvImportResult.Failed(TranslationKeys.ImportErrorUnsupportedFormat);
        }

        CvImportDiagnosticsLogger.LogStep("router", $"Importer: {driver.GetType().Name}");

        CvImportResult response;

        try
        {
            response = driver.Import(filePath);
        }
        catch (Exception ex)
        {
            CvImportDiagnosticsLogger.LogFailure("importer", $"{ex.GetType().Name}: {ex.Message}");
            CvImportDiagnosticsLogger.EndSession(success: false);
            return CvImportResult.Failed(TranslationKeys.ImportErrorUnreadableDocument);
        }

        if (!response.Success)
        {
            CvImportDiagnosticsLogger.LogFailure("import result", response.ErrorMessageKey);
            CvImportDiagnosticsLogger.EndSession(success: false);
            return CvImportResult.Failed(NormalizeKey(response.ErrorMessageKey));
        }

        if (driver is not TextCvFormatImporterBase)
        {
            CvImportDiagnosticsLogger.LogStructuredResult(response);
        }

        CvImportDiagnosticsLogger.EndSession(success: true);
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
