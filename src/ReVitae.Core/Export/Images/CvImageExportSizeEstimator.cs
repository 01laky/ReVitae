namespace ReVitae.Core.Export.Images;

public static class CvImageExportSizeEstimator
{
	private const long BaseBytesPerPageAt1xPng = 350_000;
	private const double HighScaleFactor = 3.5;
	private const double JpegQualityFactorBase = 0.55;

	public static string FormatMegabytes(long bytes)
	{
		if (bytes <= 0)
		{
			return "0 MB";
		}

		var megabytes = bytes / (1024.0 * 1024.0);
		return megabytes >= 10
			? $"{megabytes:0} MB"
			: $"{megabytes:0.0} MB";
	}

	public static long EstimateBytes(int pageCount, CvImageExportFormat format, CvImageExportScale scale, int quality)
	{
		if (pageCount <= 0)
		{
			return 0;
		}

		var scaleFactor = scale == CvImageExportScale.High ? HighScaleFactor : 1.0;
		var perPage = (long)(BaseBytesPerPageAt1xPng * scaleFactor);

		perPage = format switch
		{
			CvImageExportFormat.Png => perPage,
			CvImageExportFormat.Jpeg => (long)(perPage * JpegQualityFactorBase * QualityFactor(quality)),
			CvImageExportFormat.WebP => (long)(perPage * 0.45 * QualityFactor(quality)),
			_ => perPage
		};

		return Math.Max(1, perPage * pageCount);
	}

	public static string FormatLabel(CvImageExportFormat format, CvImageExportScale scale)
	{
		var formatLabel = format switch
		{
			CvImageExportFormat.Png => "PNG",
			CvImageExportFormat.Jpeg => "JPEG",
			CvImageExportFormat.WebP => "WebP",
			_ => format.ToString()
		};

		var scaleLabel = scale == CvImageExportScale.High ? "2×" : "1×";
		return $"{formatLabel}, {scaleLabel}";
	}

	private static double QualityFactor(int quality)
	{
		var clamped = Math.Clamp(quality, CvImageExportLimits.MinQuality, CvImageExportLimits.MaxQuality);
		return clamped / 100.0;
	}
}
