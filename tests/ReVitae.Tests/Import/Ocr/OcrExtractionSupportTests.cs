using ReVitae.Core.Import.Extraction;
using ReVitae.Core.Import.Ocr;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Import.Ocr;

public sealed class OcrExtractionSupportTests
{
	[Fact]
	public void BuildSuccess_PopulatesOcrMetadataAndWarning()
	{
		var engine = new FixtureOcrEngine();
		var options = new OcrOptions("eng+slk");

		var result = OcrExtractionSupport.BuildSuccess(
			"Recognized text with enough characters for a CV import draft.",
			pageCount: 2,
			engine,
			options);

		Assert.True(result.Success);
		Assert.Equal(CvTextAcquisitionStrategy.Ocr, result.Strategy);
		Assert.Equal("FixtureOcrEngine", result.OcrEngineName);
		Assert.Equal("eng+slk", result.OcrLanguages);
		Assert.Contains(result.Warnings ?? [], warning => warning.MessageKey == TranslationKeys.ImportWarningOcrUsed);
	}

	[Fact]
	public void Unavailable_ReturnsImportErrorOcrUnavailable()
	{
		var result = OcrExtractionSupport.Unavailable();

		Assert.False(result.Success);
		Assert.Equal(TranslationKeys.ImportErrorOcrUnavailable, result.ErrorMessageKey);
	}

	[Fact]
	public void Failed_ReturnsImportErrorOcrFailed()
	{
		var result = OcrExtractionSupport.Failed();

		Assert.False(result.Success);
		Assert.Equal(TranslationKeys.ImportErrorOcrFailed, result.ErrorMessageKey);
	}
}
