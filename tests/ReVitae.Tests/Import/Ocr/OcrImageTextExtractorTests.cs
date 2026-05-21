using ReVitae.Core.Import;
using ReVitae.Core.Import.Extraction;
using ReVitae.Core.Import.Ocr;
using ReVitae.Core.Localization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ReVitae.Tests.Import.Ocr;

public sealed class OcrImageTextExtractorTests
{
    [Fact]
    public void Extract_UsesFakeOcrEngineAndAddsWarning()
    {
        var path = CreateBlankPng();
        var extractor = new OcrImageTextExtractor(new FixtureOcrEngine());

        var result = extractor.Extract(path);

        Assert.True(result.Success);
        Assert.Equal(CvTextAcquisitionStrategy.Ocr, result.Strategy);
        Assert.Contains(result.Warnings ?? [], warning => warning.MessageKey == TranslationKeys.ImportWarningOcrUsed);
        Assert.Contains("Jane Doe", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Extract_ParsedThroughTextPipelineProducesWorkSection()
    {
        var path = CreateBlankPng();
        var importer = new ReVitae.Core.Import.Importers.RasterImageCvFormatImporter(new OcrImageTextExtractor(new FixtureOcrEngine()));
        var result = importer.Import(path);

        Assert.True(result.Success);
        Assert.Contains(result.WorkExperienceEntries, entry =>
            entry.Company.Contains("Acme", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DetectFormat_RecognizesPngAsRasterImage()
    {
        var path = CreateBlankPng();
        Assert.Equal(CvImportFormat.RasterImage, CvImportFormatDetector.DetectFormat(path));
    }

    private static string CreateBlankPng()
    {
        var path = Path.Combine(Path.GetTempPath(), $"revitae-ocr-test-{Guid.NewGuid():N}.png");
        using var image = new Image<Rgba32>(32, 32);
        image.SaveAsPng(path);
        return path;
    }
}

public sealed class OcrLayoutNormalizerTests
{
    [Fact]
    public void Normalize_SortsLinesByBoundingBoxes()
    {
        var recognition = new OcrRecognitionResult(
            Text: "wrong order",
            Lines:
            [
                new OcrTextLine("Bottom line", Top: 200, Left: 10, Bottom: 220, Right: 100),
                new OcrTextLine("Top line", Top: 10, Left: 10, Bottom: 30, Right: 80)
            ]);

        var normalized = OcrLayoutNormalizer.Normalize(recognition);

        Assert.StartsWith("Top line", normalized, StringComparison.Ordinal);
        Assert.Contains("Bottom line", normalized, StringComparison.Ordinal);
    }
}
