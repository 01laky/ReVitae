using ReVitae.Core.Export.Images;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ReVitae.Tests.Export.Images;

public sealed class CvImageBackgroundCompositorTests
{
    [Fact]
    public void CompositeOnWhite_ProducesOpaqueRgbImage()
    {
        using var source = new Image<Rgba32>(20, 20);
        source.Mutate(ctx => ctx.BackgroundColor(Color.Red));

        using var composited = CvImageBackgroundCompositor.CompositeOnWhite(source);
        Assert.Equal(20, composited.Width);
        Assert.Equal(20, composited.Height);
    }

    [Fact]
    public void Encode_AfterComposite_ProducesOpaqueJpeg()
    {
        using var source = new Image<Rgba32>(24, 24);
        source.Mutate(ctx => ctx.BackgroundColor(Color.Red));

        var bytes = CvImageEncoder.Encode(source, CvImageExportFormat.Jpeg, 85);
        Assert.NotEmpty(bytes);
        Assert.Equal(0xFF, bytes[0]);
        Assert.Equal(0xD8, bytes[1]);
    }

    [Fact]
    public void Encode_Png_ProducesValidHeader()
    {
        using var source = new Image<Rgba32>(16, 16);
        source.Mutate(ctx => ctx.BackgroundColor(Color.Blue));

        var bytes = CvImageEncoder.Encode(source, CvImageExportFormat.Png, 90);
        Assert.True(bytes.Length > 8);
        Assert.Equal(0x89, bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
    }

    [Fact]
    public void Encode_WebP_ProducesNonEmptyOutput()
    {
        using var source = new Image<Rgba32>(16, 16);
        source.Mutate(ctx => ctx.BackgroundColor(Color.Green));

        var bytes = CvImageEncoder.Encode(source, CvImageExportFormat.WebP, 80);
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void GetFileExtension_JpegUsesDotJpg()
    {
        Assert.Equal(".jpg", CvImageEncoder.GetFileExtension(CvImageExportFormat.Jpeg));
    }

    [Fact]
    public void Encode_JpegQualityChangesSize()
    {
        using var source = CreateStripedImage(64, 64);

        var low = CvImageEncoder.Encode(source, CvImageExportFormat.Jpeg, 70);
        var high = CvImageEncoder.Encode(source, CvImageExportFormat.Jpeg, 95);
        Assert.True(high.Length >= low.Length);
    }

    private static Image<Rgba32> CreateStripedImage(int width, int height)
    {
        var image = new Image<Rgba32>(width, height);
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    row[x] = x % 2 == 0 ? Color.Black : Color.Gray;
                }
            }
        });
        return image;
    }
}

public sealed class CvImageEncoderTests
{
    [Theory]
    [InlineData(CvImageExportFormat.Png, ".png")]
    [InlineData(CvImageExportFormat.Jpeg, ".jpg")]
    [InlineData(CvImageExportFormat.WebP, ".webp")]
    public void GetFileExtension_MatchesFormat(CvImageExportFormat format, string extension)
    {
        Assert.Equal(extension, CvImageEncoder.GetFileExtension(format));
    }

    [Fact]
    public void Encode_ClampedQuality_DoesNotThrow()
    {
        using var source = new Image<Rgba32>(8, 8);
        source.Mutate(ctx => ctx.BackgroundColor(Color.Red));
        var bytes = CvImageEncoder.Encode(source, CvImageExportFormat.Jpeg, 200);
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void Encode_Png_IsDeterministicForSameInput()
    {
        using var source = new Image<Rgba32>(12, 12);
        source.Mutate(ctx => ctx.BackgroundColor(Color.CadetBlue));
        var first = CvImageEncoder.Encode(source, CvImageExportFormat.Png, 90);
        var second = CvImageEncoder.Encode(source, CvImageExportFormat.Png, 90);
        Assert.Equal(first, second);
    }

    [Fact]
    public void Encode_WebPFallback_StillReturnsBytes()
    {
        using var source = new Image<Rgba32>(10, 10);
        source.Mutate(ctx => ctx.BackgroundColor(Color.Orange));
        var bytes = CvImageEncoder.Encode(source, CvImageExportFormat.WebP, 75);
        Assert.NotEmpty(bytes);
    }
}
