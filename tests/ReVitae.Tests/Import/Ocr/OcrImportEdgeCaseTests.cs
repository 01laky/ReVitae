using ReVitae.Core.Import;
using ReVitae.Core.Import.Extraction;
using ReVitae.Core.Import.Ocr;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Import.Ocr;

public sealed class OcrImportUiPolicyTests
{
    [Fact]
    public void CanOfferForceOcr_AllowsPdfFailuresExceptPasswordAndUnavailable()
    {
        Assert.True(OcrImportUiPolicy.CanOfferForceOcr(CvImportFormat.Pdf, TranslationKeys.ImportErrorEmptyPdf));
        Assert.True(OcrImportUiPolicy.CanOfferForceOcr(CvImportFormat.Pdf, TranslationKeys.ImportErrorOcrFailed));
        Assert.True(OcrImportUiPolicy.CanOfferForceOcr(CvImportFormat.Pdf, null));
        Assert.False(OcrImportUiPolicy.CanOfferForceOcr(CvImportFormat.Pdf, TranslationKeys.ImportErrorPasswordProtected));
        Assert.False(OcrImportUiPolicy.CanOfferForceOcr(CvImportFormat.Pdf, TranslationKeys.ImportErrorOcrUnavailable));
        Assert.False(OcrImportUiPolicy.CanOfferForceOcr(CvImportFormat.RasterImage, TranslationKeys.ImportErrorEmptyPdf));
    }
}

public sealed class OcrImportEdgeCaseTests
{
    [Fact]
    public void Extract_Image_WhenOcrUnavailable_ReturnsImportErrorOcrUnavailable()
    {
        var path = OcrFixturePaths.MinimalCvScanPng;
        var extractor = new OcrImageTextExtractor(new UnavailableOcrEngine());

        var result = extractor.Extract(path);

        Assert.False(result.Success);
        Assert.Equal(TranslationKeys.ImportErrorOcrUnavailable, result.ErrorMessageKey);
    }

    [Fact]
    public void Extract_Image_WhenOcrReturnsEmpty_ReturnsImportErrorOcrFailed()
    {
        var path = OcrFixturePaths.MinimalCvScanPng;
        var extractor = new OcrImageTextExtractor(new FakeOcrEngine(string.Empty));

        var result = extractor.Extract(path);

        Assert.False(result.Success);
        Assert.Equal(TranslationKeys.ImportErrorOcrFailed, result.ErrorMessageKey);
    }

    [Fact]
    public void Extract_EmptyTextPdf_WithFixtureOcrEngine_UsesOcrFallback()
    {
        var pig = new StubTextExtractor(new CvTextExtractionResult(
            false,
            string.Empty,
            TranslationKeys.ImportErrorEmptyPdf,
            PageCount: 1));
        var ocr = new StubTextExtractor(new CvTextExtractionResult(
            true,
            FixtureOcrEngine.SampleCvText,
            null,
            PageCount: 1,
            Strategy: CvTextAcquisitionStrategy.Ocr));

        var composite = new CompositePdfTextExtractor(pig, ocr);
        var result = composite.Extract(OcrFixturePaths.MinimalCvEmptyTextPdf);

        Assert.True(result.Success);
        Assert.Equal(CvTextAcquisitionStrategy.Ocr, result.Strategy);
        Assert.Equal(1, ocr.CallCount);
    }

    [Fact]
    public void Extract_WhenOcrUnavailableButPdfHasThinText_ReturnsPdfText()
    {
        const string thinText = "Jane Doe\njane@example.com";

        var pig = new StubTextExtractor(new CvTextExtractionResult(
            true,
            thinText,
            null,
            PageCount: 1,
            Strategy: CvTextAcquisitionStrategy.PdfTextLayer));
        var ocr = new StubTextExtractor(new CvTextExtractionResult(
            false,
            string.Empty,
            TranslationKeys.ImportErrorOcrUnavailable));

        var composite = new CompositePdfTextExtractor(pig, ocr);
        var result = composite.Extract("thin.pdf");

        Assert.True(result.Success);
        Assert.Equal(CvTextAcquisitionStrategy.PdfTextLayer, result.Strategy);
        Assert.Contains("Jane Doe", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Import_MinimalCvScanFixture_WithFixtureOcrEngine_ProducesWorkSection()
    {
        var importer = new ReVitae.Core.Import.Importers.RasterImageCvFormatImporter(
            new OcrImageTextExtractor(new FixtureOcrEngine()));
        var result = importer.Import(OcrFixturePaths.MinimalCvScanPng);

        Assert.True(result.Success);
        Assert.Contains(result.WorkExperienceEntries, entry =>
            entry.Company.Contains("Acme", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("Category", "OcrIntegration")]
    public void Import_EmptyTextPdf_WithRealTesseract_WhenAvailable_ProducesDraftOrOcrError()
    {
        using var engine = new TesseractOcrEngine();
        if (!engine.IsAvailable)
        {
            return;
        }

        using var session = CvImportSessionOptions.Begin(new CvImportSessionOptions(UiLanguageCode: "en"));
        var pdfExtractor = CvOcrImportDefaults.CreateDefaultPdfExtractor();
        var extraction = pdfExtractor.Extract(OcrFixturePaths.MinimalCvEmptyTextPdf);

        Assert.True(
            extraction.Success || extraction.ErrorMessageKey == TranslationKeys.ImportErrorOcrFailed,
            $"Unexpected extraction outcome: success={extraction.Success}, error={extraction.ErrorMessageKey}");
    }

    [Fact]
    [Trait("Category", "OcrIntegration")]
    public void Import_MinimalCvScanPng_WithRealTesseract_WhenAvailable_RecognizesName()
    {
        using var engine = new TesseractOcrEngine();
        if (!engine.IsAvailable)
        {
            return;
        }

        using var session = CvImportSessionOptions.Begin(new CvImportSessionOptions(UiLanguageCode: "en"));
        var extractor = new OcrImageTextExtractor(engine);
        var result = extractor.Extract(OcrFixturePaths.MinimalCvScanPng);

        Assert.True(result.Success);
        Assert.Contains("Jane", result.Text, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class StubTextExtractor(CvTextExtractionResult result) : ICvTextExtractor
    {
        public int CallCount { get; private set; }

        public CvTextExtractionResult Extract(string filePath)
        {
            CallCount++;
            return result;
        }
    }
}

internal static class OcrFixturePaths
{
    public static string MinimalCvEmptyTextPdf =>
        Path.Combine(AppContext.BaseDirectory, "Import", "Fixtures", "Ocr", "MinimalCvEmptyText.pdf");

    public static string MinimalCvScanPng =>
        Path.Combine(AppContext.BaseDirectory, "Import", "Fixtures", "Ocr", "MinimalCvScan.png");
}
