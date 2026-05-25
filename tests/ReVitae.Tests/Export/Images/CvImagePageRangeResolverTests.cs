using ReVitae.Core.Export.Images;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Export.Images;

public sealed class CvImagePageRangeResolverTests
{
    [Fact]
    public void Resolve_AllPages_ReturnsFullRange()
    {
        var result = CvImagePageRangeResolver.Resolve(5, CvImagePageRange.AllPages);
        Assert.True(result.IsValid);
        Assert.Equal([1, 2, 3, 4, 5], result.PageIndices);
    }

    [Fact]
    public void Resolve_PartialRange_ReturnsSubset()
    {
        var result = CvImagePageRangeResolver.Resolve(5, new CvImagePageRange(2, 3));
        Assert.True(result.IsValid);
        Assert.Equal([2, 3], result.PageIndices);
    }

    [Fact]
    public void Resolve_SinglePageRange()
    {
        var result = CvImagePageRangeResolver.Resolve(3, new CvImagePageRange(1, 1));
        Assert.True(result.IsValid);
        Assert.Equal([1], result.PageIndices);
    }

    [Fact]
    public void Resolve_FromGreaterThanTo_IsInvalid()
    {
        var result = CvImagePageRangeResolver.Resolve(5, new CvImagePageRange(4, 2));
        Assert.False(result.IsValid);
        Assert.Equal(TranslationKeys.ExportImageRangeInvalid, result.ErrorMessageKey);
    }

    [Fact]
    public void Resolve_ZeroFrom_IsInvalid()
    {
        var result = CvImagePageRangeResolver.Resolve(5, new CvImagePageRange(0, 2));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Resolve_ZeroTo_IsInvalid()
    {
        var result = CvImagePageRangeResolver.Resolve(5, new CvImagePageRange(1, 0));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Resolve_ToBeyondTotal_IsInvalid()
    {
        var result = CvImagePageRangeResolver.Resolve(5, new CvImagePageRange(1, 6));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Resolve_TotalPagesBeyondCap_IsInvalid()
    {
        var result = CvImagePageRangeResolver.Resolve(51, CvImagePageRange.AllPages);
        Assert.False(result.IsValid);
        Assert.Equal(TranslationKeys.ExportImageTooManyPages, result.ErrorMessageKey);
    }

    [Fact]
    public void Resolve_ZeroTotalPages_IsInvalid()
    {
        var result = CvImagePageRangeResolver.Resolve(0, CvImagePageRange.AllPages);
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(2, 3, 2)]
    [InlineData(5, 5, 1)]
    [InlineData(1, 4, 4)]
    public void Resolve_PartialRangeCount(int from, int to, int expectedCount)
    {
        var result = CvImagePageRangeResolver.Resolve(5, new CvImagePageRange(from, to));
        Assert.True(result.IsValid);
        Assert.Equal(expectedCount, result.PageIndices.Count);
    }

    [Fact]
    public void Resolve_PreservesOriginalPageNumbers()
    {
        var result = CvImagePageRangeResolver.Resolve(5, new CvImagePageRange(2, 3));
        Assert.Equal(2, result.PageIndices[0]);
        Assert.Equal(3, result.PageIndices[1]);
    }
}
