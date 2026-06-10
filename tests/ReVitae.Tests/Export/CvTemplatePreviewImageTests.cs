using ReVitae.Core.Export;
using ReVitae.Core.Export.Fixtures;

namespace ReVitae.Tests.Export;

/// <summary>047 T1 — the preview rasterizes the real export PDF.</summary>
public sealed class CvTemplatePreviewImageTests
{
	private static readonly byte[] PngMagic = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

	[Theory]
	[InlineData(CvExportTemplateId.ClassicSidebar)]
	[InlineData(CvExportTemplateId.CenteredMinimal)]
	[InlineData(CvExportTemplateId.TealProfessional)]
	public void RenderPagesPng_ProducesValidPngPerPage(CvExportTemplateId templateId)
	{
		var document = JohnDoeMinimalArchitectCvDataset.CreateDocument(templateId);

		var pages = CvTemplatePreviewImage.RenderPagesPng(document);

		Assert.NotEmpty(pages);
		Assert.All(pages, page =>
		{
			Assert.True(page.Length > 100);
			Assert.Equal(PngMagic, page.Take(PngMagic.Length).ToArray());
		});
	}

	[Fact]
	public void RenderPagesPng_PageCountMatchesPdf()
	{
		// The minimal architect ClassicSidebar export is multi-page; the preview renders each.
		var document = JohnDoeMinimalArchitectCvDataset.CreateDocument(CvExportTemplateId.ClassicSidebar);
		var pdf = new ReVitae.Core.Export.Pdf.QuestPdfCvExporter().Export(document);
		var expected = new ReVitae.Core.Export.Images.DocnetPdfPageRasterizer().GetPageCount(pdf);

		var pages = CvTemplatePreviewImage.RenderPagesPng(document);

		Assert.Equal(expected, pages.Count);
	}

	[Fact]
	public void RenderPagesPng_NullDocument_Throws()
	{
		Assert.Throws<ArgumentNullException>(() => CvTemplatePreviewImage.RenderPagesPng(null!));
	}

	[Fact]
	public void RenderPagesPng_DifferentTemplates_BothRender()
	{
		var a = CvTemplatePreviewImage.RenderPagesPng(
			JohnDoeMinimalArchitectCvDataset.CreateDocument(CvExportTemplateId.CenteredMinimal));
		var b = CvTemplatePreviewImage.RenderPagesPng(
			JohnDoeMinimalArchitectCvDataset.CreateDocument(CvExportTemplateId.NavyProfileSplit));

		Assert.NotEmpty(a);
		Assert.NotEmpty(b);
	}
}
