using ReVitae.Core.Import.Extraction;
using ReVitae.Core.Import.Importers;
using ReVitae.Core.Import.Ocr;
using ReVitae.Core.Import.Pdf;

namespace ReVitae.Core.Import;

public static class CvOcrImportDefaults
{
	public static ICvTextExtractor CreateDefaultPdfExtractor()
	{
		var ocrEngine = CreateDefaultOcrEngine();
		var pdfTextLayer = new PdfTextExtractorAdapter(new PdfPigTextExtractor());
		var ocrPdf = new OcrPdfTextExtractor(ocrEngine, new DocnetPdfPageRenderer());
		return new CompositePdfTextExtractor(pdfTextLayer, ocrPdf);
	}

	public static ICvTextExtractor CreateDefaultImageExtractor()
	{
		return new OcrImageTextExtractor(CreateDefaultOcrEngine());
	}

	public static IOcrEngine CreateDefaultOcrEngine() => new TesseractOcrEngine();
}
