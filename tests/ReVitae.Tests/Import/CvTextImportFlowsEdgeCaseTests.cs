using ReVitae.Core.Import;
using ReVitae.Core.Import.Extraction;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Import;

public sealed class CvTextImportFlowsEdgeCaseTests
{
	[Fact]
	public void TryFromExtractor_WhenExtractorThrows_ReturnsUnreadableDocumentAttempt()
	{
		var attempt = CvTextImportFlows.TryFromExtractor(
			new ThrowingExtractor(),
			"sample.pdf",
			CvImportFormat.Pdf);

		Assert.False(attempt.Deterministic.Success);
		Assert.Equal(TranslationKeys.ImportErrorUnreadableDocument, attempt.Deterministic.ErrorMessageKey);
		Assert.Equal(string.Empty, attempt.NormalizedText);
	}

	[Fact]
	public void TryFromExtractor_WhenExtractionFails_PreservesErrorKey()
	{
		var attempt = CvTextImportFlows.TryFromExtractor(
			new StubExtractor(new CvTextExtractionResult(
				false,
				string.Empty,
				TranslationKeys.ImportErrorOcrFailed)),
			"scan.pdf",
			CvImportFormat.Pdf);

		Assert.False(attempt.Deterministic.Success);
		Assert.Equal(TranslationKeys.ImportErrorOcrFailed, attempt.Deterministic.ErrorMessageKey);
	}

	[Fact]
	public void FromExtractor_WhenExtractorThrows_ReturnsUnreadableDocument()
	{
		var result = CvTextImportFlows.FromExtractor(new ThrowingExtractor(), "sample.pdf");

		Assert.False(result.Success);
		Assert.Equal(TranslationKeys.ImportErrorUnreadableDocument, result.ErrorMessageKey);
	}

	private sealed class ThrowingExtractor : ICvTextExtractor
	{
		public CvTextExtractionResult Extract(string filePath) =>
			throw new InvalidOperationException("Simulated extractor failure.");
	}

	private sealed class StubExtractor(CvTextExtractionResult result) : ICvTextExtractor
	{
		public CvTextExtractionResult Extract(string filePath) => result;
	}
}
