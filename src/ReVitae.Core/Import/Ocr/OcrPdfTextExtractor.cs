using ReVitae.Core.Import.Extraction;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Import.Ocr;

public sealed class OcrPdfTextExtractor(IOcrEngine ocrEngine, IPdfPageRenderer pageRenderer) : ICvTextExtractor
{
    private readonly OcrOptions _options = new();

    public CvTextExtractionResult Extract(string filePath)
    {
        CvImportDiagnosticsLogger.LogStep("ocr-pdf", $"Extract started: {Path.GetFileName(filePath)}");

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            CvImportDiagnosticsLogger.LogStep("ocr-pdf", "File not found");
            return new CvTextExtractionResult(false, string.Empty, TranslationKeys.ImportErrorFileNotFound);
        }

        CvImportDiagnosticsLogger.LogStep(
            "ocr-pdf",
            $"OCR engine: {ocrEngine.EngineName}, available={ocrEngine.IsAvailable}");

        if (!ocrEngine.IsAvailable)
        {
            CvImportDiagnosticsLogger.LogStep("ocr-pdf", "OCR engine not available");
            return OcrExtractionSupport.Unavailable();
        }

        CvImportProgress.Report(TranslationKeys.ImportRunningOcr);

        IReadOnlyList<SixLabors.ImageSharp.Image> pages;
        try
        {
            CvImportDiagnosticsLogger.LogStep(
                "ocr-pdf",
                $"Rendering PDF pages at {OcrLimits.DefaultRenderDpi} DPI (max {OcrLimits.MaxPageCount} pages)");
            pages = pageRenderer.RenderPages(filePath, OcrLimits.DefaultRenderDpi);
            CvImportDiagnosticsLogger.LogStep("ocr-pdf", $"Rendered {pages.Count} page(s)");
        }
        catch (Exception ex) when (IsPasswordProtected(ex))
        {
            CvImportDiagnosticsLogger.LogStep("ocr-pdf", $"Password-protected PDF: {ex.GetType().Name}");
            return new CvTextExtractionResult(false, string.Empty, TranslationKeys.ImportErrorPasswordProtected);
        }
        catch (Exception ex)
        {
            CvImportDiagnosticsLogger.LogStep("ocr-pdf", $"PDF render failed: {ex.GetType().Name}: {ex.Message}");
            return new CvTextExtractionResult(false, string.Empty, TranslationKeys.ImportErrorUnreadableDocument);
        }

        if (pages.Count == 0)
        {
            CvImportDiagnosticsLogger.LogStep("ocr-pdf", "No pages rendered — OCR failed");
            return OcrExtractionSupport.Failed();
        }

        var text = OcrExtractionSupport.RecognizePages(pages, ocrEngine, _options, "ocr-pdf");
        if (string.IsNullOrWhiteSpace(text))
        {
            CvImportDiagnosticsLogger.LogStep("ocr-pdf", "OCR returned empty text");
            return OcrExtractionSupport.Failed();
        }

        CvImportDiagnosticsLogger.LogStep(
            "ocr-pdf",
            $"OCR success: {text.Length} chars, {text.Count(c => !char.IsWhiteSpace(c))} non-whitespace");

        return OcrExtractionSupport.BuildSuccess(text, pages.Count, ocrEngine, _options);
    }

    private static bool IsPasswordProtected(Exception exception)
    {
        var message = exception.Message;
        return message.Contains("password", StringComparison.OrdinalIgnoreCase)
            || message.Contains("encrypted", StringComparison.OrdinalIgnoreCase);
    }
}
