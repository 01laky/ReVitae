using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;

namespace ReVitae.Core.Export.Images;

public static class CvImageEncoder
{
	public static byte[] Encode(Image image, CvImageExportFormat format, int quality)
	{
		ArgumentNullException.ThrowIfNull(image);

		using var composited = CvImageBackgroundCompositor.CompositeOnWhite(image);
		using var stream = new MemoryStream();

		var clampedQuality = Math.Clamp(quality, CvImageExportLimits.MinQuality, CvImageExportLimits.MaxQuality);

		switch (format)
		{
			case CvImageExportFormat.Png:
				composited.Save(stream, new PngEncoder());
				break;
			case CvImageExportFormat.Jpeg:
				composited.Save(stream, new JpegEncoder { Quality = clampedQuality });
				break;
			case CvImageExportFormat.WebP:
				try
				{
					composited.Save(stream, new WebpEncoder { Quality = clampedQuality });
				}
				catch
				{
					stream.SetLength(0);
					composited.Save(stream, new PngEncoder());
				}

				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(format), format, null);
		}

		return stream.ToArray();
	}

	public static string GetFileExtension(CvImageExportFormat format) => format switch
	{
		CvImageExportFormat.Png => ".png",
		CvImageExportFormat.Jpeg => ".jpg",
		CvImageExportFormat.WebP => ".webp",
		_ => ".png"
	};

	public static bool IsPngFallback(byte[] bytes, CvImageExportFormat format) =>
		format == CvImageExportFormat.WebP && bytes.Length >= 8 &&
		bytes[0] == 0x89 && bytes[1] == (byte)'P' && bytes[2] == (byte)'N' && bytes[3] == (byte)'G';
}
