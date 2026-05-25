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

	[Fact]
	public void Extract_MissingFile_ReturnsImportErrorFileNotFound()
	{
		var missing = Path.Combine(Path.GetTempPath(), $"revitae-missing-{Guid.NewGuid():N}.png");
		var extractor = new OcrImageTextExtractor(new FixtureOcrEngine());

		var result = extractor.Extract(missing);

		Assert.False(result.Success);
		Assert.Equal(TranslationKeys.ImportErrorFileNotFound, result.ErrorMessageKey);
	}

	[Fact]
	public void Extract_WhitespacePath_ReturnsImportErrorFileNotFound()
	{
		var extractor = new OcrImageTextExtractor(new FixtureOcrEngine());

		var result = extractor.Extract("   ");

		Assert.False(result.Success);
		Assert.Equal(TranslationKeys.ImportErrorFileNotFound, result.ErrorMessageKey);
	}

	[Fact]
	public void Extract_CorruptBytes_ReturnsImportErrorUnsupportedFormat()
	{
		var path = Path.Combine(Path.GetTempPath(), $"revitae-corrupt-{Guid.NewGuid():N}.png");
		File.WriteAllBytes(path, [0x00, 0x01, 0x02, 0x03]);

		var extractor = new OcrImageTextExtractor(new FixtureOcrEngine());
		var result = extractor.Extract(path);

		Assert.False(result.Success);
		Assert.Equal(TranslationKeys.ImportErrorUnsupportedFormat, result.ErrorMessageKey);
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

	[Fact]
	public void Normalize_WhenLinesNull_ReturnsTrimmedRawText()
	{
		var recognition = new OcrRecognitionResult(Text: "  raw OCR text  ");

		var normalized = OcrLayoutNormalizer.Normalize(recognition);

		Assert.Equal("raw OCR text", normalized);
	}

	[Fact]
	public void Normalize_WhenAllLineTextsEmpty_FallsBackToRawText()
	{
		var recognition = new OcrRecognitionResult(
			Text: " fallback raw ",
			Lines:
			[
				new OcrTextLine("   ", Top: 10, Left: 10, Bottom: 30, Right: 80),
				new OcrTextLine("\t", Top: 40, Left: 10, Bottom: 60, Right: 80)
			]);

		var normalized = OcrLayoutNormalizer.Normalize(recognition);

		Assert.Equal("fallback raw", normalized);
	}
}
