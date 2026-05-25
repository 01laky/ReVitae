using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ReVitae.Core.Export;
using SkiaSharp;
using Svg.Skia;
using System;
using System.Collections.Generic;
using System.IO;

namespace ReVitae.Export;

internal static class CvExportFormatIconLoader
{
	private const int IconSize = 48;
	private static readonly Dictionary<string, IImage?> Cache = new(StringComparer.Ordinal);

	public static IImage? LoadIcon(CvExportFormat format)
	{
		var slug = CvExportFormatCatalog.Get(format).IconSlug;
		if (Cache.TryGetValue(slug, out var cached))
		{
			return cached;
		}

		var uri = new Uri($"avares://ReVitae/Assets/ExportFormats/export-format-{slug}.svg");
		if (!AssetLoader.Exists(uri))
		{
			Cache[slug] = null;
			return null;
		}

		using var stream = AssetLoader.Open(uri);
		var svg = new SKSvg();
		svg.Load(stream);
		if (svg.Picture is null)
		{
			Cache[slug] = null;
			return null;
		}

		var bounds = svg.Picture.CullRect;
		var scale = Math.Min(IconSize / bounds.Width, IconSize / bounds.Height);
		var matrix = SKMatrix.CreateScale(scale, scale);
		using var bitmap = new SKBitmap(IconSize, IconSize, SKColorType.Rgba8888, SKAlphaType.Premul);
		using var canvas = new SKCanvas(bitmap);
		canvas.Clear(SKColors.Transparent);
		canvas.Translate((IconSize - bounds.Width * scale) / 2f, (IconSize - bounds.Height * scale) / 2f);
		canvas.DrawPicture(svg.Picture, ref matrix);

		using var image = SKImage.FromBitmap(bitmap);
		using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
		using var memoryStream = new MemoryStream(encoded.ToArray());
		var avaloniaBitmap = new Bitmap(memoryStream);
		Cache[slug] = avaloniaBitmap;
		return avaloniaBitmap;
	}
}
