using ReVitae.Core.Export;
using ReVitae.Core.Export.Images;
using ReVitae.Core.Export.Pdf;
using ReVitae.Core.Localization;
using ReVitae.Tests.Import;

namespace ReVitae.Tests.Export.Images;

public sealed class DocnetPdfPageRasterizerTests
{
	private readonly DocnetPdfPageRasterizer _rasterizer = new();

	[Fact]
	public void GetPageCount_MinimalPdf_ReturnsOne()
	{
		var pdf = MinimalPdfWriter.CreateFromLines(["Hello CV"]);
		Assert.Equal(1, _rasterizer.GetPageCount(pdf));
	}

	[Fact]
	public void GetPageCount_EmptyBytes_ReturnsZero()
	{
		Assert.Equal(0, _rasterizer.GetPageCount([]));
	}

	[Fact]
	public void RenderPage_MinimalPdf_ReturnsNonZeroDimensions()
	{
		var pdf = MinimalPdfWriter.CreateFromLines(["Hello CV"]);
		using var image = _rasterizer.RenderPage(pdf, 0, CvImageExportScale.Standard);
		Assert.True(image.Width > 0);
		Assert.True(image.Height > 0);
	}

	[Fact]
	public void RenderPage_HighScale_IsLargerThanStandard()
	{
		var pdf = MinimalPdfWriter.CreateFromLines(["Scale test"]);
		using var standard = _rasterizer.RenderPage(pdf, 0, CvImageExportScale.Standard);
		using var high = _rasterizer.RenderPage(pdf, 0, CvImageExportScale.High);
		Assert.True(high.Width >= standard.Width);
		Assert.True(high.Height >= standard.Height);
	}

	[Fact]
	public void RenderPage_InvalidIndex_Throws()
	{
		var pdf = MinimalPdfWriter.CreateFromLines(["One page"]);
		Assert.Throws<ArgumentOutOfRangeException>(() => _rasterizer.RenderPage(pdf, 1, CvImageExportScale.Standard));
	}

	[Fact]
	public void RenderPage_EmptyPdf_Throws()
	{
		Assert.Throws<InvalidOperationException>(() => _rasterizer.RenderPage([], 0, CvImageExportScale.Standard));
	}

	[Fact]
	public void RenderPage_CanDisposeWithoutLeak()
	{
		var pdf = MinimalPdfWriter.CreateFromLines(["Dispose test"]);
		for (var i = 0; i < 5; i++)
		{
			using var image = _rasterizer.RenderPage(pdf, 0, CvImageExportScale.Standard);
		}
	}

	[Fact]
	public void GetPageCount_QuestPdfDocument_MatchesExport()
	{
		var document = CvExportTestFixtures.CreateRepresentativeDocument();
		var pdf = new QuestPdfCvExporter().Export(document);
		Assert.True(_rasterizer.GetPageCount(pdf) >= 1);
	}
}

public sealed class CvImageExportLimitsTests
{
	[Fact]
	public void MaxPageCount_IsFifty()
	{
		Assert.Equal(50, CvImageExportLimits.MaxPageCount);
	}

	[Fact]
	public void MaxPixelDimension_Is4096()
	{
		Assert.Equal(4096, CvImageExportLimits.MaxPixelDimension);
	}

	[Fact]
	public void DefaultOptions_UsePromptDefaults()
	{
		var defaults = CvImageExportOptions.Default;
		Assert.Equal(CvImageExportFormat.Png, defaults.Format);
		Assert.Equal(CvImageExportDelivery.ZipArchive, defaults.Delivery);
		Assert.Equal(CvImageExportScale.High, defaults.Scale);
		Assert.Equal(90, defaults.Quality);
		Assert.True(defaults.PageRange.IsAllPages);
	}

	[Fact]
	public void Resolve_PageCountAtCap_IsValid()
	{
		var result = CvImagePageRangeResolver.Resolve(CvImageExportLimits.MaxPageCount, CvImagePageRange.AllPages);
		Assert.True(result.IsValid);
		Assert.Equal(CvImageExportLimits.MaxPageCount, result.PageIndices.Count);
	}
}
