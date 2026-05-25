using ReVitae.Core.Import.Importers;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Import;

public static class CvTextImportCoordinator
{
    public static bool IsTextRoute(CvImportFormat format) =>
        CvImportSectionMetrics.IsTextRouteFormat(format);

    public static CvTextImportAttempt? TryImport(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return null;
        }

        long payloadLength;
        try
        {
            payloadLength = new FileInfo(filePath).Length;
        }
        catch (Exception)
        {
            return null;
        }

        if (payloadLength > CvImportLimits.MaxFileBytes)
        {
            return null;
        }

        var format = CvImportFormatDetector.DetectFormat(filePath);
        if (!IsTextRoute(format))
        {
            return null;
        }

        CvImportDiagnosticsLogger.BeginSession(filePath, format, payloadLength);
        CvImportDiagnosticsLogger.LogStep("coordinator", $"Text-route import: {format}");

        var driver = CvFormatImporterRegistry.Get(format);
        if (driver is not TextCvFormatImporterBase textImporter)
        {
            CvImportDiagnosticsLogger.LogFailure("coordinator", "Expected text importer.");
            CvImportDiagnosticsLogger.EndSession(success: false);
            return null;
        }

        var attempt = textImporter.TryImportDetailed(filePath);
        CvImportDiagnosticsLogger.EndSession(attempt.Deterministic.Success);
        return attempt;
    }

    public static CvImportResult ImportDeterministicOnly(string filePath)
    {
        var attempt = TryImport(filePath);
        if (attempt is null)
        {
            return CvDocumentImporter.Import(filePath);
        }

        return attempt.Deterministic.Success
            ? attempt.Deterministic
            : CvImportResult.Failed(NormalizeKey(attempt.Deterministic.ErrorMessageKey));
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
