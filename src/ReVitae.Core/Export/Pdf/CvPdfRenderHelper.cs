using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ReVitae.Core.Export.Pdf;

internal static class CvPdfRenderHelper
{
    internal static byte[] Generate(CvExportDocument document, Action<IDocumentContainer> compose) =>
        Document.Create(compose)
            .WithMetadata(CvPdfExportMetadata.ForTemplate(document.TemplateId))
            .GeneratePdf();
}
