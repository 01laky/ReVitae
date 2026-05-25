using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ReVitae.Core.Export.Images;

public static class CvImageBackgroundCompositor
{
    public static Image<Rgb24> CompositeOnWhite(Image source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var canvas = new Image<Rgb24>(source.Width, source.Height);
        canvas.Mutate(ctx => ctx.BackgroundColor(Color.White));

        canvas.Mutate(ctx => ctx.DrawImage(source, 1f));
        return canvas;
    }

    public static bool IsCornerNearWhite(Image<Rgb24> image, int sampleSize = 4)
    {
        ArgumentNullException.ThrowIfNull(image);

        var width = Math.Min(sampleSize, image.Width);
        var height = Math.Min(sampleSize, image.Height);
        if (width == 0 || height == 0)
        {
            return false;
        }

        long total = 0;
        var count = 0;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var pixel = image[x, y];
                total += pixel.R + pixel.G + pixel.B;
                count++;
            }
        }

        if (count == 0)
        {
            return false;
        }

        var average = total / (count * 3.0);
        return average >= 240;
    }
}
