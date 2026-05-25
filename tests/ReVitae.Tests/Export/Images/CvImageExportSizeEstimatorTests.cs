using ReVitae.Core.Export.Images;

namespace ReVitae.Tests.Export.Images;

public sealed class CvImageExportSizeEstimatorTests
{
    [Fact]
    public void EstimateBytes_MorePages_IncreasesEstimate()
    {
        var one = CvImageExportSizeEstimator.EstimateBytes(1, CvImageExportFormat.Png, CvImageExportScale.Standard, 90);
        var three = CvImageExportSizeEstimator.EstimateBytes(3, CvImageExportFormat.Png, CvImageExportScale.Standard, 90);
        Assert.True(three > one);
    }

    [Fact]
    public void EstimateBytes_HighScale_IsLargerThanStandard()
    {
        var standard = CvImageExportSizeEstimator.EstimateBytes(2, CvImageExportFormat.Png, CvImageExportScale.Standard, 90);
        var high = CvImageExportSizeEstimator.EstimateBytes(2, CvImageExportFormat.Png, CvImageExportScale.High, 90);
        Assert.True(high >= standard);
    }

    [Fact]
    public void EstimateBytes_JpegHighQuality_IsLargerThanLow()
    {
        var low = CvImageExportSizeEstimator.EstimateBytes(2, CvImageExportFormat.Jpeg, CvImageExportScale.Standard, 70);
        var high = CvImageExportSizeEstimator.EstimateBytes(2, CvImageExportFormat.Jpeg, CvImageExportScale.Standard, 95);
        Assert.True(high >= low);
    }

    [Fact]
    public void EstimateBytes_Png_IsLargerThanJpeg()
    {
        var png = CvImageExportSizeEstimator.EstimateBytes(2, CvImageExportFormat.Png, CvImageExportScale.Standard, 90);
        var jpeg = CvImageExportSizeEstimator.EstimateBytes(2, CvImageExportFormat.Jpeg, CvImageExportScale.Standard, 90);
        Assert.True(png >= jpeg);
    }

    [Fact]
    public void EstimateBytes_ZeroPages_ReturnsZero()
    {
        Assert.Equal(0, CvImageExportSizeEstimator.EstimateBytes(0, CvImageExportFormat.Png, CvImageExportScale.Standard, 90));
    }

    [Fact]
    public void FormatMegabytes_FormatsSmallValues()
    {
        Assert.Equal("0 MB", CvImageExportSizeEstimator.FormatMegabytes(0));
        Assert.Contains("MB", CvImageExportSizeEstimator.FormatMegabytes(2_500_000));
    }

    [Fact]
    public void FormatLabel_IncludesFormatAndScale()
    {
        var label = CvImageExportSizeEstimator.FormatLabel(CvImageExportFormat.WebP, CvImageExportScale.High);
        Assert.Contains("WebP", label);
        Assert.Contains("2×", label);
    }

    [Fact]
    public void EstimateBytes_WebP_IsSmallerThanPng()
    {
        var png = CvImageExportSizeEstimator.EstimateBytes(1, CvImageExportFormat.Png, CvImageExportScale.Standard, 90);
        var webp = CvImageExportSizeEstimator.EstimateBytes(1, CvImageExportFormat.WebP, CvImageExportScale.Standard, 90);
        Assert.True(png >= webp);
    }
}
