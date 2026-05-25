using System;
using ReVitae.Core.Import.Extraction;

namespace ReVitae.Core.Import;

internal static class CvTextImportFlows
{
    public static CvTextImportAttempt TryFromExtractor(
        ICvTextExtractor extractor,
        string filePath,
        CvImportFormat format)
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
            var failed = CvImportResult.Failed(ReVitae.Core.Localization.TranslationKeys.ImportErrorUnreadableDocument);
            return new CvTextImportAttempt(
                format,
                new CvTextExtractionResult(false, string.Empty, ReVitae.Core.Localization.TranslationKeys.ImportErrorUnreadableDocument),
                string.Empty,
                EmptySegmentation(),
                failed);
        }

        if (!extraction.Success)
        {
            CvImportDiagnosticsLogger.LogStep(
                "import-flow",
                $"Extraction failed: {extraction.ErrorMessageKey ?? "unknown"}");
            CvImportDiagnosticsLogger.LogExtraction(extraction);
            var failed = CvImportResult.Failed(extraction.ErrorMessageKey ?? ReVitae.Core.Localization.TranslationKeys.ImportErrorUnreadableDocument);
            return new CvTextImportAttempt(format, extraction, string.Empty, EmptySegmentation(), failed);
        }

        CvImportDiagnosticsLogger.LogStep(
            "import-flow",
            $"Extraction OK — strategy={extraction.Strategy?.ToString() ?? "legacy"}, " +
            $"entering text pipeline ({extraction.Text.Length} chars)");
        CvImportDiagnosticsLogger.LogExtraction(extraction);

        var normalized = CvTextNormalizer.Normalize(extraction.Text);
        var segmentation = CvSectionSegmenter.Segment(normalized);
        var deterministic = CvTextImportPipeline.Import(extraction.Text, extraction.HyperlinkUrls, extraction.Warnings);

        var storedText = Ai.Import.AiImportLimits.TruncateSourceText(normalized);
        return new CvTextImportAttempt(format, extraction, storedText, segmentation, deterministic);
    }

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

    private static CvSegmentationResult EmptySegmentation() =>
        new()
        {
            HeaderBlock = string.Empty,
            SectionBodies = new Dictionary<CvImportSectionId, string>(),
            Warnings = []
        };
}
