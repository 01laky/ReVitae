using ReVitae.Core.Localization;

namespace ReVitae.Core.Import.Ocr;

/// <summary>UI rules for optional Force OCR retry on PDF imports.</summary>
public static class OcrImportUiPolicy
{
	public static bool CanOfferForceOcr(CvImportFormat format, string? errorMessageKey) =>
		format == CvImportFormat.Pdf
		&& !string.Equals(errorMessageKey, TranslationKeys.ImportErrorPasswordProtected, StringComparison.Ordinal)
		&& !string.Equals(errorMessageKey, TranslationKeys.ImportErrorOcrUnavailable, StringComparison.Ordinal);
}
