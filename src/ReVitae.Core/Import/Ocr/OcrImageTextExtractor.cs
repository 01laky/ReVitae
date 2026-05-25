using ReVitae.Core.Import.Extraction;
using ReVitae.Core.Localization;
using SixLabors.ImageSharp;

namespace ReVitae.Core.Import.Ocr;

public sealed class OcrImageTextExtractor(IOcrEngine ocrEngine) : ICvTextExtractor
{
    private OcrOptions CreateOptions() =>
        new(OcrLanguageResolver.ResolveLanguages(CvImportSessionOptions.Session.UiLanguageCode));

    public CvTextExtractionResult Extract(string filePath)
    {
        CvImportDiagnosticsLogger.LogStep("ocr-image", $"Extract started: {Path.GetFileName(filePath)}");

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            CvImportDiagnosticsLogger.LogStep("ocr-image", "File not found");
            return new CvTextExtractionResult(false, string.Empty, TranslationKeys.ImportErrorFileNotFound);
        }

        CvImportDiagnosticsLogger.LogStep(
            "ocr-image",
            $"OCR engine: {ocrEngine.EngineName}, available={ocrEngine.IsAvailable}");

        if (!ocrEngine.IsAvailable)
        {
            CvImportDiagnosticsLogger.LogStep("ocr-image", "OCR engine not available");
            return OcrExtractionSupport.Unavailable();
        }

        CvImportProgress.Report(TranslationKeys.ImportRunningOcr);

        try
        {
            using var image = Image.Load(filePath);
            CvImportDiagnosticsLogger.LogStep(
                "ocr-image",
                $"Loaded image: {image.Width}x{image.Height}px, format={image.Metadata.DecodedImageFormat?.Name ?? "unknown"}");

            var options = CreateOptions();
            var recognition = ocrEngine.Recognize(image, options);
            var text = OcrLayoutNormalizer.Normalize(recognition, logSection: "ocr-image");
            if (string.IsNullOrWhiteSpace(text))
            {
                CvImportDiagnosticsLogger.LogStep("ocr-image", "OCR returned empty text");
                return OcrExtractionSupport.Failed();
            }

            CvImportDiagnosticsLogger.LogStep(
                "ocr-image",
                $"OCR success: {text.Length} chars, {text.Count(c => !char.IsWhiteSpace(c))} non-whitespace");

            return OcrExtractionSupport.BuildSuccess(text, pageCount: 1, ocrEngine, options);
        }
        catch (UnknownImageFormatException ex)
        {
            CvImportDiagnosticsLogger.LogStep("ocr-image", $"Unsupported image format: {ex.Message}");
            return new CvTextExtractionResult(false, string.Empty, TranslationKeys.ImportErrorUnsupportedFormat);
        }
        catch (Exception ex)
        {
            CvImportDiagnosticsLogger.LogStep("ocr-image", $"Image OCR failed: {ex.GetType().Name}: {ex.Message}");
            return new CvTextExtractionResult(false, string.Empty, TranslationKeys.ImportErrorUnreadableDocument);
        }
    }
}
