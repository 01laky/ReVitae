using ReVitae.Core.Export.Images;
using ReVitae.Core.Export.Pdf;
using SixLabors.ImageSharp;

namespace ReVitae.Core.Export;

/// <summary>
/// Renders the live CV preview by rasterizing the <b>actual exported PDF</b> (047 T1), so the
/// preview is guaranteed to match the export — there is a single rendering pipeline
/// (QuestPDF → PDF → Docnet raster) instead of a parallel Avalonia re-implementation of every
/// template. Returns one PNG per page.
/// </summary>
public static class CvTemplatePreviewImage
{
	private static readonly DocnetPdfPageRasterizer Rasterizer = new();
	private static readonly QuestPdfCvExporter Exporter = new();

	public static IReadOnlyList<byte[]> RenderPagesPng(
		CvExportDocument document,
		CvImageExportScale scale = CvImageExportScale.Standard)
	{
		ArgumentNullException.ThrowIfNull(document);

		var pdfBytes = Exporter.Export(document);
		var pageCount = Rasterizer.GetPageCount(pdfBytes);
		var pages = new List<byte[]>(Math.Max(pageCount, 0));

		for (var pageIndex = 0; pageIndex < pageCount; pageIndex++)
		{
			using var image = Rasterizer.RenderPage(pdfBytes, pageIndex, scale);
			using var stream = new MemoryStream();
			image.SaveAsPng(stream);
			pages.Add(stream.ToArray());
		}

		return pages;
	}
}
