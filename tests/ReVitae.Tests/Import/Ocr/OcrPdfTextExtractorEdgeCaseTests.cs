using ReVitae.Core.Import.Extraction;
using ReVitae.Core.Import.Ocr;
using ReVitae.Core.Localization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ReVitae.Tests.Import.Ocr;

public sealed class OcrPdfTextExtractorEdgeCaseTests
{
    [Fact]
    public void Extract_MissingFile_ReturnsImportErrorFileNotFound()
    {
        var extractor = new OcrPdfTextExtractor(new FixtureOcrEngine(), new StubPdfPageRenderer([]));
        var missing = Path.Combine(Path.GetTempPath(), $"revitae-missing-{Guid.NewGuid():N}.pdf");

        var result = extractor.Extract(missing);

        Assert.False(result.Success);
        Assert.Equal(TranslationKeys.ImportErrorFileNotFound, result.ErrorMessageKey);
    }

    [Fact]
    public void Extract_WhenEngineUnavailable_ReturnsImportErrorOcrUnavailable()
    {
        var path = OcrFixturePaths.MinimalCvEmptyTextPdf;
        var extractor = new OcrPdfTextExtractor(new UnavailableOcrEngine(), new StubPdfPageRenderer([CreatePage()]));

        var result = extractor.Extract(path);

        Assert.False(result.Success);
        Assert.Equal(TranslationKeys.ImportErrorOcrUnavailable, result.ErrorMessageKey);
    }

    [Fact]
    public void Extract_WhenRendererThrowsPasswordException_ReturnsImportErrorPasswordProtected()
    {
        var path = OcrFixturePaths.MinimalCvEmptyTextPdf;
        var renderer = new StubPdfPageRenderer([], new InvalidOperationException("Document is password protected"));
        var extractor = new OcrPdfTextExtractor(new FixtureOcrEngine(), renderer);

        var result = extractor.Extract(path);

        Assert.False(result.Success);
        Assert.Equal(TranslationKeys.ImportErrorPasswordProtected, result.ErrorMessageKey);
    }

    [Fact]
    public void Extract_WhenRendererReturnsZeroPages_ReturnsImportErrorOcrFailed()
    {
        var path = OcrFixturePaths.MinimalCvEmptyTextPdf;
        var extractor = new OcrPdfTextExtractor(new FixtureOcrEngine(), new StubPdfPageRenderer([]));

        var result = extractor.Extract(path);

        Assert.False(result.Success);
        Assert.Equal(TranslationKeys.ImportErrorOcrFailed, result.ErrorMessageKey);
    }

    [Fact]
    public void Extract_MultiPage_MergesPageChunksWithBlankLineSeparator()
    {
        var path = OcrFixturePaths.MinimalCvEmptyTextPdf;
        var pages = new[] { CreatePage(), CreatePage() };
        var extractor = new OcrPdfTextExtractor(new PerPageOcrEngine(), new StubPdfPageRenderer(pages));

        var result = extractor.Extract(path);

        Assert.True(result.Success);
        Assert.Contains("Page1", result.Text, StringComparison.Ordinal);
        Assert.Contains("Page2", result.Text, StringComparison.Ordinal);
        Assert.Contains("\n\n", result.Text, StringComparison.Ordinal);
        Assert.Equal(2, result.PageCount);
    }

    [Fact]
    public void Extract_ReportsImportRunningOcrViaCvImportProgress()
    {
        var path = OcrFixturePaths.MinimalCvEmptyTextPdf;
        string? reportedKey = null;
        void Handler(string key) => reportedKey = key;
        ReVitae.Core.Import.CvImportProgress.StatusChanged += Handler;

        try
        {
            var extractor = new OcrPdfTextExtractor(new FixtureOcrEngine(), new StubPdfPageRenderer([CreatePage()]));
            _ = extractor.Extract(path);
        }
        finally
        {
            ReVitae.Core.Import.CvImportProgress.StatusChanged -= Handler;
        }

        Assert.Equal(TranslationKeys.ImportRunningOcr, reportedKey);
    }

    private static Image<Rgba32> CreatePage()
    {
        return new Image<Rgba32>(16, 16);
    }

    private sealed class StubPdfPageRenderer(IReadOnlyList<Image> pages, Exception? failure = null) : IPdfPageRenderer
    {
        public IReadOnlyList<Image> RenderPages(string filePath, int dpi)
        {
            if (failure is not null)
            {
                throw failure;
            }

            return pages;
        }
    }

    private sealed class PerPageOcrEngine : IOcrEngine
    {
        private int _pageIndex;

        public string EngineName => "PerPageOcrEngine";

        public bool IsAvailable => true;

        public OcrRecognitionResult Recognize(Image image, OcrOptions options)
        {
            _pageIndex++;
            return new OcrRecognitionResult($"Page{_pageIndex} content with enough characters for OCR success.");
        }
    }
}
