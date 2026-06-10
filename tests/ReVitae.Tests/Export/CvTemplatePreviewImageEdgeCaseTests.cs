using ReVitae.Core.Export;
using ReVitae.Core.Export.Fixtures;
using ReVitae.Core.Export.Images;
using SixLabors.ImageSharp;

namespace ReVitae.Tests.Export;

public sealed class CvTemplatePreviewImageEdgeCaseTests
{
	private static readonly byte[] PngMagic = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

	[Fact]
	public void EveryTemplate_RendersAtLeastOneValidPngPage()
	{
		foreach (var templateId in Enum.GetValues<CvExportTemplateId>())
		{
			var pages = CvTemplatePreviewImage.RenderPagesPng(
				JohnDoeMinimalArchitectCvDataset.CreateDocument(templateId));

			Assert.True(pages.Count >= 1, $"Template {templateId} produced no preview pages.");
			Assert.All(pages, page =>
			{
				Assert.True(page.Length > 100);
				Assert.Equal(PngMagic, page.Take(PngMagic.Length).ToArray());
			});
		}
	}

	[Fact]
	public void HighScale_ProducesWiderImageThanStandard()
	{
		var document = JohnDoeMinimalArchitectCvDataset.CreateDocument(CvExportTemplateId.CenteredMinimal);

		var standard = CvTemplatePreviewImage.RenderPagesPng(document, CvImageExportScale.Standard);
		var high = CvTemplatePreviewImage.RenderPagesPng(document, CvImageExportScale.High);

		var standardWidth = Image.Identify(standard[0]).Width;
		var highWidth = Image.Identify(high[0]).Width;

		Assert.True(highWidth > standardWidth, $"High ({highWidth}) should exceed Standard ({standardWidth}).");
	}

	[Fact]
	public void RenderPagesPng_PagesHavePositiveDimensions()
	{
		var pages = CvTemplatePreviewImage.RenderPagesPng(
			JohnDoeMinimalArchitectCvDataset.CreateDocument(CvExportTemplateId.ClassicSidebar));

		Assert.All(pages, page =>
		{
			var info = Image.Identify(page);
			Assert.True(info.Width > 0);
			Assert.True(info.Height > 0);
			// A4 portrait → taller than wide.
			Assert.True(info.Height > info.Width);
		});
	}

	[Fact]
	public void RenderPagesPng_StandardA4Width_IsInExpectedRange()
	{
		var pages = CvTemplatePreviewImage.RenderPagesPng(
			JohnDoeMinimalArchitectCvDataset.CreateDocument(CvExportTemplateId.CenteredMinimal));

		var width = Image.Identify(pages[0]).Width;
		// A4 (210mm) at ~96 DPI ≈ 794px; allow a generous band for rasterizer rounding.
		Assert.InRange(width, 600, 1000);
	}

	[Fact]
	public void RenderPagesPng_PageCountMatchesRasterizer_AcrossTemplates()
	{
		var rasterizer = new DocnetPdfPageRasterizer();
		var exporter = new ReVitae.Core.Export.Pdf.QuestPdfCvExporter();

		foreach (var templateId in new[]
		{
			CvExportTemplateId.ClassicSidebar,
			CvExportTemplateId.BurgundyExecutive,
			CvExportTemplateId.CenteredMinimal,
		})
		{
			var document = JohnDoeMinimalArchitectCvDataset.CreateDocument(templateId);
			var expected = rasterizer.GetPageCount(exporter.Export(document));
			var pages = CvTemplatePreviewImage.RenderPagesPng(document);
			Assert.Equal(expected, pages.Count);
		}
	}
}
