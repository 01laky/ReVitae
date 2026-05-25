using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;

namespace ReVitae.Tests;

internal static class ProfilePhotoTestHelpers
{
	public static string CreateTempDirectory()
	{
		var path = Path.Combine(Path.GetTempPath(), "revitae-photo-tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(path);
		return path;
	}

	public static string WriteMinimalPng(string directory)
	{
		var path = Path.Combine(directory, "sample.png");
		using var image = new Image<Rgba32>(8, 8);
		image.SaveAsPng(path);
		return path;
	}

	public static string WriteMinimalJpeg(string directory, ushort? orientation = null)
	{
		var path = Path.Combine(directory, "sample.jpg");
		using var image = new Image<Rgba32>(40, 20);
		image.ProcessPixelRows(accessor =>
		{
			for (var y = 0; y < accessor.Height; y++)
			{
				var row = accessor.GetRowSpan(y);
				for (var x = 0; x < row.Length; x++)
				{
					row[x] = x < row.Length / 2 ? Color.Red : Color.Blue;
				}
			}
		});

		if (orientation.HasValue)
		{
			image.Metadata.ExifProfile ??= new ExifProfile();
			image.Metadata.ExifProfile.SetValue(ExifTag.Orientation, orientation.Value);
		}

		image.SaveAsJpeg(path, new JpegEncoder { Quality = 90 });
		return path;
	}

	public static string WriteMinimalWebp(string directory)
	{
		var path = Path.Combine(directory, "sample.webp");
		using var image = new Image<Rgba32>(16, 16);
		image.SaveAsWebp(path);
		return path;
	}

	public static string WriteOversizedPlaceholder(string directory, long byteCount, string extension = ".jpg")
	{
		var path = Path.Combine(directory, "oversized" + extension);
		using var stream = File.Create(path);
		var buffer = new byte[8192];
		long written = 0;
		while (written < byteCount)
		{
			var chunk = (int)Math.Min(buffer.Length, byteCount - written);
			stream.Write(buffer, 0, chunk);
			written += chunk;
		}

		return path;
	}

	public static (int Width, int Height) ReadImageDimensions(string path)
	{
		var info = Image.Identify(path);
		return (info.Width, info.Height);
	}
}
