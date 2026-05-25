namespace ReVitae.Core.Import.Extraction;

/// <summary>How plain text was obtained before the shared import pipeline runs.</summary>
public enum CvTextAcquisitionStrategy
{
	PdfTextLayer,
	Ocr,
	HybridPdf
}
