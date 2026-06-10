using ReVitae.Core.Export;
using ReVitae.Core.Export.Fixtures;
using ReVitae.Core.Export.Pdf;

namespace ReVitae.Tests.Export;

public sealed class CvTemplateRenderSignatureEdgeCaseTests
{
	[Fact]
	public void Compute_IsHexUppercase64Chars()
	{
		var sig = CvTemplateRenderSignature.Compute(CvExportTemplateId.ClassicSidebar);
		Assert.Equal(64, sig.Length);
		Assert.Matches("^[0-9A-F]+$", sig);
	}

	[Fact]
	public void ComputeFromPdf_SamePdfBytes_SameSignature()
	{
		var pdf = new QuestPdfCvExporter().Export(
			JohnDoeMinimalArchitectCvDataset.CreateDocument(CvExportTemplateId.CenteredMinimal));

		Assert.Equal(
			CvTemplateRenderSignature.ComputeFromPdf(pdf),
			CvTemplateRenderSignature.ComputeFromPdf(pdf));
	}

	[Fact]
	public void ComputeFromPdf_DifferentTextContent_DifferentSignature()
	{
		var exporter = new QuestPdfCvExporter();
		var a = exporter.Export(JohnDoeMinimalArchitectCvDataset.CreateDocument(CvExportTemplateId.CenteredMinimal));
		var changed = JohnDoeMinimalArchitectCvDataset.CreateDocument(CvExportTemplateId.CenteredMinimal)
			with
		{ FirstName = "Zzyzx", LastName = "Quux" };
		var b = exporter.Export(changed);

		Assert.NotEqual(
			CvTemplateRenderSignature.ComputeFromPdf(a),
			CvTemplateRenderSignature.ComputeFromPdf(b));
	}

	[Fact]
	public void ComputeAll_CoversEveryTemplateExactlyOnce()
	{
		var all = CvTemplateRenderSignature.ComputeAll();
		var ids = all.Select(s => s.TemplateId).ToHashSet();

		Assert.Equal(Enum.GetValues<CvExportTemplateId>().Length, all.Count);
		Assert.Equal(Enum.GetValues<CvExportTemplateId>().Length, ids.Count);
	}

	[Fact]
	public void ComputeAll_SignaturesAreNonEmptyHex()
	{
		Assert.All(CvTemplateRenderSignature.ComputeAll(), entry =>
		{
			Assert.Equal(64, entry.Signature.Length);
			Assert.Matches("^[0-9A-F]+$", entry.Signature);
		});
	}

	[Fact]
	public void Compute_DistinctLayouts_ProduceDistinctSignatures()
	{
		var sidebar = CvTemplateRenderSignature.Compute(CvExportTemplateId.ClassicSidebar);
		var minimal = CvTemplateRenderSignature.Compute(CvExportTemplateId.CenteredMinimal);
		var timeline = CvTemplateRenderSignature.Compute(CvExportTemplateId.OrangeTimeline);

		Assert.NotEqual(sidebar, minimal);
		Assert.NotEqual(sidebar, timeline);
		Assert.NotEqual(minimal, timeline);
	}

	[Fact]
	public void Compute_MatchesComputeFromPdf_ForSameTemplate()
	{
		var pdf = new QuestPdfCvExporter().Export(
			JohnDoeMinimalArchitectCvDataset.CreateDocument(CvExportTemplateId.ClassicSidebar));

		Assert.Equal(
			CvTemplateRenderSignature.Compute(CvExportTemplateId.ClassicSidebar),
			CvTemplateRenderSignature.ComputeFromPdf(pdf));
	}
}
