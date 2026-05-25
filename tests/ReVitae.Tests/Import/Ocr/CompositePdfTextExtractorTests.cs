using ReVitae.Core.Import;
using ReVitae.Core.Import.Extraction;
using ReVitae.Core.Import.Ocr;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Import.Ocr;

public sealed class CompositePdfTextExtractorTests
{
	[Fact]
	public void Extract_UsesPdfTextLayerWhenQualityGatePasses()
	{
		const string pdfText = """
            Jane Doe
            jane@example.com
            Senior Engineer with ten years of experience building products.
            Work Experience
            Engineer at Acme
            """;

		var pig = new StubTextExtractor(new CvTextExtractionResult(
			true,
			pdfText,
			null,
			PageCount: 1,
			Strategy: CvTextAcquisitionStrategy.PdfTextLayer));
		var ocr = new StubTextExtractor(new CvTextExtractionResult(
			true,
			"OCR should not run",
			null,
			Strategy: CvTextAcquisitionStrategy.Ocr));

		var composite = new CompositePdfTextExtractor(pig, ocr);
		var result = composite.Extract("sample.pdf");

		Assert.Equal(CvTextAcquisitionStrategy.PdfTextLayer, result.Strategy);
		Assert.Equal(0, ocr.CallCount);
		Assert.Contains("Jane Doe", result.Text, StringComparison.Ordinal);
	}

	[Fact]
	public void Extract_FallsBackToOcrWhenPdfTextIsEmpty()
	{
		var pig = new StubTextExtractor(new CvTextExtractionResult(
			false,
			string.Empty,
			TranslationKeys.ImportErrorEmptyPdf,
			PageCount: 2));
		var ocr = new StubTextExtractor(new CvTextExtractionResult(
			true,
			"Recognized OCR text with enough content for quality gate to pass easily.",
			null,
			PageCount: 2,
			Strategy: CvTextAcquisitionStrategy.Ocr));

		var composite = new CompositePdfTextExtractor(pig, ocr);
		var result = composite.Extract("scan.pdf");

		Assert.Equal(CvTextAcquisitionStrategy.Ocr, result.Strategy);
		Assert.Equal(1, ocr.CallCount);
	}

	[Fact]
	public void Extract_DoesNotInvokeOcrForPasswordProtectedPdf()
	{
		var pig = new StubTextExtractor(new CvTextExtractionResult(
			false,
			string.Empty,
			TranslationKeys.ImportErrorPasswordProtected));
		var ocr = new StubTextExtractor(new CvTextExtractionResult(
			true,
			"Should not run",
			null,
			Strategy: CvTextAcquisitionStrategy.Ocr));

		var composite = new CompositePdfTextExtractor(pig, ocr);
		var result = composite.Extract("locked.pdf");

		Assert.Equal(TranslationKeys.ImportErrorPasswordProtected, result.ErrorMessageKey);
		Assert.Equal(0, ocr.CallCount);
	}

	[Fact]
	public void Extract_ForceOcrSkipsPdfTextLayer()
	{
		using var session = CvImportSessionOptions.Begin(new CvImportSessionOptions(ForceOcr: true));

		const string pdfText = """
            Jane Doe
            jane@example.com
            Senior Engineer with ten years of experience building products.
            """;

		var pig = new StubTextExtractor(new CvTextExtractionResult(true, pdfText, null, PageCount: 1));
		var ocr = new StubTextExtractor(new CvTextExtractionResult(
			true,
			"Forced OCR text with enough characters to represent a scanned CV body.",
			null,
			PageCount: 1,
			Strategy: CvTextAcquisitionStrategy.Ocr));

		var composite = new CompositePdfTextExtractor(pig, ocr);
		var result = composite.Extract("text.pdf");

		Assert.Equal(CvTextAcquisitionStrategy.Ocr, result.Strategy);
		Assert.Equal(0, pig.CallCount);
		Assert.Equal(1, ocr.CallCount);
	}

	[Fact]
	public void Extract_FallsBackToOcrWhenPdfTextPassesButQualityGateFails()
	{
		const string thinText = "Jane Doe\njane@example.com";

		var pig = new StubTextExtractor(new CvTextExtractionResult(
			true,
			thinText,
			null,
			PageCount: 1,
			Strategy: CvTextAcquisitionStrategy.PdfTextLayer));
		var ocr = new StubTextExtractor(new CvTextExtractionResult(
			true,
			"Recognized OCR text with enough content for quality gate to pass easily.",
			null,
			PageCount: 1,
			Strategy: CvTextAcquisitionStrategy.Ocr));

		var composite = new CompositePdfTextExtractor(pig, ocr);
		var result = composite.Extract("thin-text.pdf");

		Assert.Equal(CvTextAcquisitionStrategy.Ocr, result.Strategy);
		Assert.Equal(1, ocr.CallCount);
	}

	[Fact]
	public void Extract_WhenPdfExtractionFails_ReturnsPdfErrorAfterOcrFails()
	{
		var pig = new StubTextExtractor(new CvTextExtractionResult(
			false,
			string.Empty,
			TranslationKeys.ImportErrorUnreadablePdf));
		var ocr = new StubTextExtractor(new CvTextExtractionResult(
			false,
			string.Empty,
			TranslationKeys.ImportErrorOcrFailed));

		var composite = new CompositePdfTextExtractor(pig, ocr);
		var result = composite.Extract("broken.pdf");

		Assert.False(result.Success);
		Assert.Equal(TranslationKeys.ImportErrorUnreadablePdf, result.ErrorMessageKey);
	}

	[Fact]
	public void Extract_WhenQualityGateFailsAndOcrFails_ReturnsOcrError()
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
			TranslationKeys.ImportErrorOcrFailed));

		var composite = new CompositePdfTextExtractor(pig, ocr);
		var result = composite.Extract("thin.pdf");

		Assert.False(result.Success);
		Assert.Equal(TranslationKeys.ImportErrorOcrFailed, result.ErrorMessageKey);
	}

	[Fact]
	public void Extract_WhenOcrUnavailableAndPdfTextWhitespaceOnly_ReturnsOcrUnavailable()
	{
		var pig = new StubTextExtractor(new CvTextExtractionResult(
			true,
			"   \t\n  ",
			null,
			PageCount: 1,
			Strategy: CvTextAcquisitionStrategy.PdfTextLayer));
		var ocr = new StubTextExtractor(new CvTextExtractionResult(
			false,
			string.Empty,
			TranslationKeys.ImportErrorOcrUnavailable));

		var composite = new CompositePdfTextExtractor(pig, ocr);
		var result = composite.Extract("blank.pdf");

		Assert.False(result.Success);
		Assert.Equal(TranslationKeys.ImportErrorOcrUnavailable, result.ErrorMessageKey);
	}

	[Fact]
	public void Extract_ForceOcrWhenOcrFails_ReturnsOcrErrorWithoutCallingPdfPig()
	{
		using var session = CvImportSessionOptions.Begin(new CvImportSessionOptions(ForceOcr: true));

		var pig = new StubTextExtractor(new CvTextExtractionResult(
			true,
			"PdfPig text that should never be read when ForceOcr fails.",
			null,
			PageCount: 1));
		var ocr = new StubTextExtractor(new CvTextExtractionResult(
			false,
			string.Empty,
			TranslationKeys.ImportErrorOcrFailed));

		var composite = new CompositePdfTextExtractor(pig, ocr);
		var result = composite.Extract("force-fail.pdf");

		Assert.False(result.Success);
		Assert.Equal(TranslationKeys.ImportErrorOcrFailed, result.ErrorMessageKey);
		Assert.Equal(0, pig.CallCount);
		Assert.Equal(1, ocr.CallCount);
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
