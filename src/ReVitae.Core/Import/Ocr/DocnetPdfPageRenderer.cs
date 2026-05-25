using Docnet.Core;
using Docnet.Core.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ReVitae.Core.Import.Ocr;

public sealed class DocnetPdfPageRenderer : IPdfPageRenderer
{
	public IReadOnlyList<Image> RenderPages(string filePath, int dpi)
	{
		if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
		{
			CvImportDiagnosticsLogger.LogStep("pdf-render", "File not found — returning 0 pages");
			return [];
		}

		var pdfBytes = File.ReadAllBytes(filePath);
		CvImportDiagnosticsLogger.LogStep(
			"pdf-render",
			$"Loading PDF ({pdfBytes.Length} bytes), max dimension={OcrLimits.MaxPixelDimension}px");

		var dimension = new PageDimensions(OcrLimits.MaxPixelDimension, OcrLimits.MaxPixelDimension);
		using var docReader = DocLib.Instance.GetDocReader(pdfBytes, dimension);
		var totalPages = docReader.GetPageCount();
		var pageCount = Math.Min(totalPages, OcrLimits.MaxPageCount);
		var pages = new List<Image>(pageCount);

		CvImportDiagnosticsLogger.LogStep(
			"pdf-render",
			$"PDF has {totalPages} page(s), rendering {pageCount} (cap={OcrLimits.MaxPageCount})");

		for (var pageIndex = 0; pageIndex < pageCount; pageIndex++)
		{
			using var pageReader = docReader.GetPageReader(pageIndex);
			var width = pageReader.GetPageWidth();
			var height = pageReader.GetPageHeight();
			var rawBytes = pageReader.GetImage();
			var image = Image.LoadPixelData<Bgra32>(rawBytes, width, height);
			pages.Add(image);

			CvImportDiagnosticsLogger.LogStep(
				"pdf-render",
				$"Page {pageIndex + 1}: {width}x{height}px, {rawBytes.Length} raw bytes");
		}

		return pages;
	}
}
