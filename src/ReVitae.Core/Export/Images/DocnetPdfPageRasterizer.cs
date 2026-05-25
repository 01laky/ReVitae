using Docnet.Core;
using Docnet.Core.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using UglyToad.PdfPig;

namespace ReVitae.Core.Export.Images;

public interface ICvPdfPageRasterizer
{
	int GetPageCount(byte[] pdfBytes);

	Image RenderPage(byte[] pdfBytes, int pageIndexZeroBased, CvImageExportScale scale);
}

public sealed class DocnetPdfPageRasterizer : ICvPdfPageRasterizer
{
	private const double PixelsPerPointAtStandard = 96.0 / 72.0;

	public int GetPageCount(byte[] pdfBytes)
	{
		if (pdfBytes.Length == 0)
		{
			return 0;
		}

		using var document = PdfDocument.Open(pdfBytes);
		return document.NumberOfPages;
	}

	public Image RenderPage(byte[] pdfBytes, int pageIndexZeroBased, CvImageExportScale scale)
	{
		ArgumentNullException.ThrowIfNull(pdfBytes);
		if (pdfBytes.Length == 0)
		{
			throw new InvalidOperationException("PDF bytes are empty.");
		}

		using var document = PdfDocument.Open(pdfBytes);
		if (pageIndexZeroBased < 0 || pageIndexZeroBased >= document.NumberOfPages)
		{
			throw new ArgumentOutOfRangeException(nameof(pageIndexZeroBased));
		}

		var page = document.GetPage(pageIndexZeroBased + 1);
		var scaleMultiplier = scale == CvImageExportScale.High ? 2.0 : 1.0;
		var targetWidth = (int)Math.Ceiling(page.Width * PixelsPerPointAtStandard * scaleMultiplier);
		var targetHeight = (int)Math.Ceiling(page.Height * PixelsPerPointAtStandard * scaleMultiplier);

		if (targetWidth > CvImageExportLimits.MaxPixelDimension ||
			targetHeight > CvImageExportLimits.MaxPixelDimension)
		{
			throw new InvalidOperationException("Rendered page exceeds maximum pixel dimension.");
		}

		var dimension = new PageDimensions(
			Math.Max(1, targetWidth),
			Math.Max(1, targetHeight));

		using var docReader = DocLib.Instance.GetDocReader(pdfBytes, dimension);
		using var pageReader = docReader.GetPageReader(pageIndexZeroBased);
		var width = pageReader.GetPageWidth();
		var height = pageReader.GetPageHeight();
		var rawBytes = pageReader.GetImage();
		return Image.LoadPixelData<Bgra32>(rawBytes, width, height);
	}
}
