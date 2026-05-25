using System.Text;
using ReVitae.Core.Export;
using ReVitae.Core.Import.Pdf;

namespace ReVitae.Tests.Export;

public sealed class PdfTemplateSmokeEdgeCaseTests
{
	public static IEnumerable<object[]> LegacyTemplateIds =>
		new[]
		{
			CvExportTemplateId.ClassicSidebar,
			CvExportTemplateId.ModernSidebar,
			CvExportTemplateId.CleanTopHeader,
			CvExportTemplateId.DarkSidebarAccent,
			CvExportTemplateId.CenteredMinimal,
			CvExportTemplateId.PhotoLeftBand,
			CvExportTemplateId.ExecutiveBlueSidebar,
			CvExportTemplateId.PeachDesigner,
		}.Select(id => new object[] { id });

	[Theory]
	[MemberData(nameof(LegacyTemplateIds))]
	public void Export_LegacyTemplate_ProducesPdfHeader(CvExportTemplateId templateId)
	{
		var document = CvExportTestFixtures.CreateRepresentativeDocument(templateId);
		var source = CvExportTestFixtures.CreateRepresentativeSourceData();
		using var stream = new MemoryStream();

		var result = CvDocumentExporter.Export(document, source, CvExportFormat.Pdf, stream);

		Assert.True(result.Success, templateId.ToString());
		var header = Encoding.ASCII.GetString(stream.ToArray()[..4]);
		Assert.Equal("%PDF", header);
	}

	[Theory]
	[InlineData(CvExportTemplateId.NavyProfileSplit)]
	[InlineData(CvExportTemplateId.ForestGreenSidebar)]
	[InlineData(CvExportTemplateId.YellowSkillDots)]
	[InlineData(CvExportTemplateId.RoyalBlueSidebar)]
	[InlineData(CvExportTemplateId.OrangeTimeline)]
	[InlineData(CvExportTemplateId.BlueAccentSummary)]
	[InlineData(CvExportTemplateId.PillHeaderSplit)]
	[InlineData(CvExportTemplateId.NavyOverlapPhoto)]
	public void Export_RemainingLegacyTemplates_ProduceNonEmptyPdf(CvExportTemplateId templateId)
	{
		var document = CvExportTestFixtures.CreateRepresentativeDocument(templateId);
		var source = CvExportTestFixtures.CreatePersonalOnlySourceData();
		using var stream = new MemoryStream();

		var result = CvDocumentExporter.Export(document, source, CvExportFormat.Pdf, stream);

		Assert.True(result.Success, templateId.ToString());
		Assert.True(stream.Length > 100);
	}

	[Fact]
	public void Export_PersonalOnly_DoesNotThrowForAllLegacyTemplates()
	{
		var legacyIds = CvExportTemplateCatalog.All.Take(16).Select(t => t.Id).ToArray();
		var document = CvExportTestFixtures.CreateRepresentativeDocument();
		var source = CvExportTestFixtures.CreateRepresentativeSourceData();

		foreach (var templateId in legacyIds)
		{
			using var stream = new MemoryStream();
			var result = CvDocumentExporter.Export(
				document with { TemplateId = templateId },
				source,
				CvExportFormat.Pdf,
				stream);
			Assert.True(result.Success, templateId.ToString());
		}
	}
}
