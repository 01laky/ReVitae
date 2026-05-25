using QuestPDF.Infrastructure;
using ReVitae.Core.Import.Pdf;

namespace ReVitae.Core.Export.Pdf;

public static class CvPdfExportMetadata
{
    public static DocumentMetadata ForTemplate(CvExportTemplateId templateId)
    {
        var version = typeof(CvPdfExportMetadata).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
        return new DocumentMetadata
        {
            Producer = ReVitaePdfMetadataReader.ProducerName,
            Creator = $"{ReVitaePdfMetadataReader.ProducerName}/{version}",
            Keywords = $"{ReVitaePdfMetadataReader.TemplateKeywordPrefix}{templateId}"
        };
    }
}
