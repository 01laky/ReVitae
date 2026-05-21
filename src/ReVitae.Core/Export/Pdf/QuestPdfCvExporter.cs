namespace ReVitae.Core.Export.Pdf;

using ReVitae.Core.Export.Pdf.Templates;
using QuestPDF.Infrastructure;

public sealed class QuestPdfCvExporter : ICvPdfExporter
{
    static QuestPdfCvExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Export(CvExportDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return document.TemplateId switch
        {
            CvExportTemplateId.ClassicSidebar => ClassicSidebarPdfTemplate.Render(document),
            CvExportTemplateId.ModernSidebar => ModernSidebarPdfTemplate.Render(document),
            CvExportTemplateId.CleanTopHeader => CleanTopHeaderPdfTemplate.Render(document),
            CvExportTemplateId.DarkSidebarAccent => DarkSidebarAccentPdfTemplate.Render(document),
            _ => throw new ArgumentOutOfRangeException(nameof(document.TemplateId), document.TemplateId, null)
        };
    }

    public void Export(CvExportDocument document, Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.CanWrite)
        {
            throw new InvalidOperationException("The destination stream must be writable.");
        }

        var pdfBytes = Export(document);
        destination.Write(pdfBytes, 0, pdfBytes.Length);
    }
}
