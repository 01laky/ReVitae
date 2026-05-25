using ReVitae.Core.Export;
using ReVitae.Core.Export.Fixtures;
using ReVitae.Core.Export.Pdf;
using ReVitae.Core.Import.Pdf;
using UglyToad.PdfPig;

namespace ReVitae.Tests.Export;

public sealed class QuestPdfExportMetadataTests
{
	[Theory]
	[InlineData(CvExportTemplateId.ModernSidebar)]
	[InlineData(CvExportTemplateId.ClassicSidebar)]
	[InlineData(CvExportTemplateId.ExecutiveBlueSidebar)]
	public void Export_WritesReVitaeInfoDictionaryFingerprint(CvExportTemplateId templateId)
	{
		var document = JohnDoeStressCvDataset.CreateDocument(templateId);
		var bytes = new QuestPdfCvExporter().Export(document);

		using var stream = new MemoryStream(bytes);
		using var pdf = PdfDocument.Open(stream);
		var information = pdf.Information;

		Assert.Equal(ReVitaePdfMetadataReader.ProducerName, information.Producer);
		Assert.StartsWith($"{ReVitaePdfMetadataReader.ProducerName}/", information.Creator, StringComparison.Ordinal);
		Assert.Contains($"{ReVitaePdfMetadataReader.TemplateKeywordPrefix}{templateId}", information.Keywords, StringComparison.Ordinal);
	}

	[Fact]
	public void ForTemplate_MatchesExportReaderContract()
	{
		var metadata = CvPdfExportMetadata.ForTemplate(CvExportTemplateId.ForestGreenSidebar);
		Assert.Equal(ReVitaePdfMetadataReader.ProducerName, metadata.Producer);
		Assert.Contains("template:ForestGreenSidebar", metadata.Keywords, StringComparison.Ordinal);
	}
}
