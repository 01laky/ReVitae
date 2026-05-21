using SixLabors.ImageSharp;
using Tesseract;

namespace ReVitae.Core.Import.Ocr;

public sealed class TesseractOcrEngine : IOcrEngine, IDisposable
{
    private readonly object _gate = new();
    private TesseractEngine? _engine;
    private string? _loadedLanguages;
    private bool _availabilityChecked;
    private bool _isAvailable;

    public string EngineName => "Tesseract";

    public bool IsAvailable
    {
        get
        {
            lock (_gate)
            {
                if (_availabilityChecked)
                {
                    return _isAvailable;
                }

                var tessdataPath = TessdataBootstrapper.EnsureDefaultTessdata();
                _isAvailable = tessdataPath is not null;
                _availabilityChecked = true;

                CvImportDiagnosticsLogger.LogStep(
                    "tesseract",
                    _isAvailable
                        ? $"Available — tessdata at {tessdataPath}"
                        : "Not available — no eng.traineddata found in search paths");

                return _isAvailable;
            }
        }
    }

    public OcrRecognitionResult Recognize(Image image, OcrOptions options)
    {
        if (!IsAvailable)
        {
            CvImportDiagnosticsLogger.LogStep("tesseract", "Recognize skipped — engine not available");
            return new OcrRecognitionResult(string.Empty);
        }

        var tessdataPath = TessdataBootstrapper.EnsureDefaultTessdata()
            ?? throw new InvalidOperationException("Tesseract tessdata is not available.");

        lock (_gate)
        {
            EnsureEngine(tessdataPath, options.Languages);
            CvImportDiagnosticsLogger.LogStep(
                "tesseract",
                $"Recognizing {image.Width}x{image.Height}px, languages={options.Languages}");

            using var pix = Pix.LoadFromMemory(OcrImageEncoder.EncodePng(image));
            using var page = _engine!.Process(pix);
            var text = page.GetText() ?? string.Empty;
            var lines = ExtractLines(page);
            var normalized = OcrLayoutNormalizer.Normalize(new OcrRecognitionResult(text, lines), "tesseract");

            CvImportDiagnosticsLogger.LogStep(
                "tesseract",
                $"Recognized: raw={text.Length} chars, lines={lines.Count}, normalized={normalized.Length} chars");

            return new OcrRecognitionResult(normalized, lines);
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _engine?.Dispose();
            _engine = null;
        }
    }

    private void EnsureEngine(string tessdataPath, string languages)
    {
        if (_engine is not null && string.Equals(_loadedLanguages, languages, StringComparison.Ordinal))
        {
            return;
        }

        _engine?.Dispose();
        CvImportDiagnosticsLogger.LogStep(
            "tesseract",
            $"Creating engine: tessdata={tessdataPath}, languages={languages}");
        _engine = new TesseractEngine(tessdataPath, languages, EngineMode.Default);
        _loadedLanguages = languages;
    }

    private static List<OcrTextLine> ExtractLines(Page page)
    {
        var lines = new List<OcrTextLine>();
        using var iterator = page.GetIterator();
        iterator.Begin();

        do
        {
            if (!iterator.TryGetBoundingBox(PageIteratorLevel.TextLine, out var rect))
            {
                continue;
            }

            var lineText = iterator.GetText(PageIteratorLevel.TextLine)?.Trim();
            if (string.IsNullOrWhiteSpace(lineText))
            {
                continue;
            }

            lines.Add(new OcrTextLine(lineText, rect.Y1, rect.X1, rect.Y2, rect.X2));
        }
        while (iterator.Next(PageIteratorLevel.TextLine));

        return lines;
    }
}
