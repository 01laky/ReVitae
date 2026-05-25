using ReVitae.Core.Import.Extraction;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Import.Ocr;

public sealed class CompositePdfTextExtractor(ICvTextExtractor pdfTextLayerExtractor, ICvTextExtractor ocrPdfExtractor)
	: ICvTextExtractor
{
	public CvTextExtractionResult Extract(string filePath)
	{
		CvImportDiagnosticsLogger.LogStep("composite-pdf", $"Extract started: {Path.GetFileName(filePath)}");

		if (CvImportSessionOptions.Session.ForceOcr)
		{
			CvImportDiagnosticsLogger.LogStep("composite-pdf", "ForceOcr=true — skipping PdfPig text layer");
			var forcedOcr = ocrPdfExtractor.Extract(filePath);
			LogDecision(forcedOcr.Success ? "force-ocr-success" : "force-ocr-failed", forcedOcr);
			return forcedOcr.Success
				? forcedOcr with { Strategy = CvTextAcquisitionStrategy.Ocr }
				: forcedOcr;
		}

		CvImportDiagnosticsLogger.LogStep("composite-pdf", "Trying PdfPig text layer");
		var pdfTextLayer = pdfTextLayerExtractor.Extract(filePath);
		LogPdfTextLayerResult(pdfTextLayer);

		if (IsPasswordProtected(pdfTextLayer))
		{
			CvImportDiagnosticsLogger.LogStep("composite-pdf", "Password-protected PDF — aborting (no OCR)");
			return pdfTextLayer;
		}

		var gate = CvTextQualityGate.Evaluate(pdfTextLayer.Text, pdfTextLayer.PageCount);
		LogQualityGate(gate);

		if (pdfTextLayer.Success && gate.IsUsable)
		{
			CvImportDiagnosticsLogger.LogStep("composite-pdf", "Using PdfPig text layer (quality gate passed)");
			return pdfTextLayer with { Strategy = CvTextAcquisitionStrategy.PdfTextLayer };
		}

		CvImportDiagnosticsLogger.LogStep(
			"composite-pdf",
			gate.IsUsable
				? "PdfPig extraction failed — falling back to OCR"
				: $"Quality gate failed ({gate.RejectReason}) — falling back to OCR");

		var ocr = ocrPdfExtractor.Extract(filePath);
		LogDecision(ocr.Success ? "ocr-fallback-success" : "ocr-fallback-failed", ocr);

		if (ocr.Success)
		{
			CvImportDiagnosticsLogger.LogStep("composite-pdf", "Using OCR fallback result");
			return ocr with { Strategy = CvTextAcquisitionStrategy.Ocr };
		}

		if (!pdfTextLayer.Success)
		{
			CvImportDiagnosticsLogger.LogStep(
				"composite-pdf",
				$"OCR failed ({ocr.ErrorMessageKey}) — returning original PdfPig error ({pdfTextLayer.ErrorMessageKey})");
			return pdfTextLayer;
		}

		if (!string.IsNullOrWhiteSpace(pdfTextLayer.Text)
			&& string.Equals(ocr.ErrorMessageKey, TranslationKeys.ImportErrorOcrUnavailable, StringComparison.Ordinal))
		{
			CvImportDiagnosticsLogger.LogStep(
				"composite-pdf",
				"OCR unavailable — using PdfPig text despite quality gate failure");
			return pdfTextLayer with { Strategy = CvTextAcquisitionStrategy.PdfTextLayer };
		}

		CvImportDiagnosticsLogger.LogStep("composite-pdf", $"Returning OCR error: {ocr.ErrorMessageKey}");
		return ocr;
	}

	private static void LogPdfTextLayerResult(CvTextExtractionResult result)
	{
		var nonWs = result.Text.Count(static c => !char.IsWhiteSpace(c));
		CvImportDiagnosticsLogger.LogStep(
			"composite-pdf",
			$"PdfPig result: success={result.Success}, pages={result.PageCount?.ToString() ?? "?"}, " +
			$"chars={result.Text.Length}, nonWs={nonWs}, error={result.ErrorMessageKey ?? "none"}");
	}

	private static void LogQualityGate(CvTextQualityGateResult gate)
	{
		var average = gate.AverageNonWhitespacePerPage?.ToString("F1") ?? "n/a";
		CvImportDiagnosticsLogger.LogStep(
			"quality-gate",
			gate.IsUsable
				? $"PASSED — nonWs={gate.NonWhitespaceCount}, avg/page={average}"
				: $"FAILED — {gate.RejectReason} (nonWs={gate.NonWhitespaceCount}, avg/page={average})");
	}

	private static void LogDecision(string label, CvTextExtractionResult result)
	{
		CvImportDiagnosticsLogger.LogStep(
			"composite-pdf",
			$"{label}: success={result.Success}, strategy={result.Strategy?.ToString() ?? "?"}, " +
			$"chars={result.Text.Length}, error={result.ErrorMessageKey ?? "none"}");
	}

	private static bool IsPasswordProtected(CvTextExtractionResult pdfText) =>
		string.Equals(pdfText.ErrorMessageKey, TranslationKeys.ImportErrorPasswordProtected, StringComparison.Ordinal);
}
