using ReVitae.Core.Import.Extraction;
using ReVitae.Core.Localization;
using SixLabors.ImageSharp;

namespace ReVitae.Core.Import.Ocr;

internal static class OcrExtractionSupport
{
	public static CvTextExtractionResult Unavailable()
	{
		CvImportDiagnosticsLogger.LogStep("ocr", "Returning ImportErrorOcrUnavailable");
		return new CvTextExtractionResult(false, string.Empty, TranslationKeys.ImportErrorOcrUnavailable);
	}

	public static CvTextExtractionResult Failed()
	{
		CvImportDiagnosticsLogger.LogStep("ocr", "Returning ImportErrorOcrFailed");
		return new(false, string.Empty, TranslationKeys.ImportErrorOcrFailed);
	}

	public static CvTextExtractionResult BuildSuccess(
		string text,
		int pageCount,
		IOcrEngine engine,
		OcrOptions options)
	{
		return new CvTextExtractionResult(
			true,
			text,
			null,
			HyperlinkUrls: null,
			Warnings: OcrWarningFactory.OcrUsed,
			PageCount: pageCount,
			Strategy: CvTextAcquisitionStrategy.Ocr,
			OcrEngineName: engine.EngineName,
			OcrLanguages: options.Languages);
	}

	public static string RecognizePages(
		IReadOnlyList<Image> pages,
		IOcrEngine engine,
		OcrOptions options,
		string logSection)
	{
		var chunks = new List<string>(pages.Count);
		for (var index = 0; index < pages.Count; index++)
		{
			var page = pages[index];
			using (page)
			{
				CvImportDiagnosticsLogger.LogStep(
					logSection,
					$"OCR page {index + 1}/{pages.Count}: {page.Width}x{page.Height}px");

				var recognition = engine.Recognize(page, options);
				var normalized = OcrLayoutNormalizer.Normalize(recognition, logSection);
				var nonWs = normalized.Count(static c => !char.IsWhiteSpace(c));

				CvImportDiagnosticsLogger.LogStep(
					logSection,
					$"Page {index + 1} OCR: {normalized.Length} chars, {nonWs} non-whitespace, " +
					$"lines={recognition.Lines?.Count.ToString() ?? "n/a"}");

				if (!string.IsNullOrWhiteSpace(normalized))
				{
					chunks.Add(normalized);
				}
			}
		}

		return string.Join("\n\n", chunks).Trim();
	}
}

internal static class OcrWarningFactory
{
	public static IReadOnlyList<CvImportWarning> OcrUsed =>
		[new CvImportWarning(TranslationKeys.ImportWarningOcrUsed)];
}
