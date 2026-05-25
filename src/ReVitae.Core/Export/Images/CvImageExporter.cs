using ReVitae.Core.Export.Pdf;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Export.Images;

public static class CvImageExporter
{
	private static readonly ICvPdfExporter PdfExporter = new QuestPdfCvExporter();
	private static readonly ICvPdfPageRasterizer Rasterizer = new DocnetPdfPageRasterizer();

	public static CvImageExportResult Export(
		CvExportDocument document,
		CvImageExportOptions options,
		CvImageExportDestination destination,
		IImageExportProgress? progress = null)
	{
		if (document is null)
		{
			return CvImageExportResult.Failed(TranslationKeys.ExportFailed);
		}

		if (options is null)
		{
			return CvImageExportResult.Failed(TranslationKeys.ExportImageOptionsRequired);
		}

		if (destination is null)
		{
			return CvImageExportResult.Failed(TranslationKeys.ExportFailed);
		}

		try
		{
			var pdfBytes = PdfExporter.Export(document);
			var totalPages = Rasterizer.GetPageCount(pdfBytes);
			if (totalPages <= 0)
			{
				return CvImageExportResult.Failed(TranslationKeys.ExportImageRasterFailed);
			}

			if (totalPages > CvImageExportLimits.MaxPageCount)
			{
				return CvImageExportResult.Failed(
					TranslationKeys.ExportImageTooManyPages,
					CvImageExportLimits.MaxPageCount);
			}

			var rangeResult = CvImagePageRangeResolver.Resolve(totalPages, options.PageRange);
			if (!rangeResult.IsValid)
			{
				return CvImageExportResult.Failed(rangeResult.ErrorMessageKey ?? TranslationKeys.ExportImageRangeInvalid);
			}

			var pagesToExport = rangeResult.PageIndices;
			var encodedPages = new List<CvImageExportPageBytes>(pagesToExport.Count);
			string? firstOutputPath = null;

			foreach (var pageIndex in pagesToExport)
			{
				progress?.Report(ImageExportProgressPhase.Rendering, encodedPages.Count + 1, pagesToExport.Count);

				using var image = Rasterizer.RenderPage(pdfBytes, pageIndex - 1, options.Scale);
				var bytes = CvImageEncoder.Encode(image, options.Format, options.Quality);
				encodedPages.Add(new CvImageExportPageBytes(pageIndex, bytes, options.Format));
			}

			progress?.Report(ImageExportProgressPhase.Writing, pagesToExport.Count, pagesToExport.Count);

			var packager = CvImageExportPackagerFactory.Create(options.Delivery);
			var packageResult = packager.Write(
				encodedPages,
				destination,
				document.FirstName,
				document.LastName);

			if (!packageResult.Success)
			{
				return CvImageExportResult.Failed(packageResult.ErrorMessageKey ?? TranslationKeys.ExportFailed);
			}

			firstOutputPath = ResolveFirstOutputPath(destination, document, encodedPages);
			return CvImageExportResult.Succeeded(encodedPages.Count, firstOutputPath);
		}
		catch (InvalidOperationException)
		{
			return CvImageExportResult.Failed(TranslationKeys.ExportImageRasterFailed);
		}
		catch
		{
			return CvImageExportResult.Failed(TranslationKeys.ExportFailed);
		}
	}

	public static int GetPageCount(CvExportDocument document)
	{
		ArgumentNullException.ThrowIfNull(document);
		var pdfBytes = PdfExporter.Export(document);
		return Rasterizer.GetPageCount(pdfBytes);
	}

	private static string? ResolveFirstOutputPath(
		CvImageExportDestination destination,
		CvExportDocument document,
		IReadOnlyList<CvImageExportPageBytes> pages)
	{
		if (pages.Count == 0)
		{
			return null;
		}

		return destination switch
		{
			CvImageExportDestination.ZipFile zip => zip.Path,
			CvImageExportDestination.Folder folder when Directory.Exists(folder.Path) =>
				Directory.GetFiles(folder.Path)
					.OrderBy(path => path, StringComparer.Ordinal)
					.FirstOrDefault(),
			_ => null
		};
	}
}

public sealed record CvImageExportResult(
	bool Success,
	int ExportedPageCount,
	string? OutputPath,
	string? ErrorMessageKey,
	object? ErrorMessageArg = null)
{
	public static CvImageExportResult Succeeded(int pageCount, string? outputPath) =>
		new(true, pageCount, outputPath, null);

	public static CvImageExportResult Failed(string errorMessageKey, object? arg = null) =>
		new(false, 0, null, errorMessageKey, arg);
}
