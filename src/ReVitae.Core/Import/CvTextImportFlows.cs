using System;
using ReVitae.Core.Import.Extraction;

namespace ReVitae.Core.Import;

internal static class CvTextImportFlows
{
    public static CvImportResult FromExtractor(ICvTextExtractor extractor, string filePath)
    {
        CvImportDiagnosticsLogger.LogStep(
            "import-flow",
            $"Extractor: {extractor.GetType().Name}, file={Path.GetFileName(filePath)}");

        CvTextExtractionResult extraction;
        try
        {
            extraction = extractor.Extract(filePath);
        }
        catch (Exception ex)
        {
            CvImportDiagnosticsLogger.LogStep(
                "import-flow",
                $"Extractor threw {ex.GetType().Name}: {ex.Message}");
            return CvImportResult.Failed(ReVitae.Core.Localization.TranslationKeys.ImportErrorUnreadableDocument);
        }

        if (!extraction.Success)
        {
            CvImportDiagnosticsLogger.LogStep(
                "import-flow",
                $"Extraction failed: {extraction.ErrorMessageKey ?? "unknown"}");
            CvImportDiagnosticsLogger.LogExtraction(extraction);
            return CvImportResult.Failed(extraction.ErrorMessageKey ?? ReVitae.Core.Localization.TranslationKeys.ImportErrorUnreadableDocument);
        }

        CvImportDiagnosticsLogger.LogStep(
            "import-flow",
            $"Extraction OK — strategy={extraction.Strategy?.ToString() ?? "legacy"}, " +
            $"entering text pipeline ({extraction.Text.Length} chars)");
        CvImportDiagnosticsLogger.LogExtraction(extraction);
        return CvTextImportPipeline.Import(extraction.Text, extraction.HyperlinkUrls, extraction.Warnings);
    }
}
