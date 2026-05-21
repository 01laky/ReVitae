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
            CvExportTemplateId.CenteredMinimal => CenteredMinimalPdfTemplate.Render(document),
            CvExportTemplateId.PhotoLeftBand => PhotoLeftBandPdfTemplate.Render(document),
            CvExportTemplateId.ExecutiveBlueSidebar => ExecutiveBlueSidebarPdfTemplate.Render(document),
            CvExportTemplateId.PeachDesigner => PeachDesignerPdfTemplate.Render(document),
            CvExportTemplateId.NavyProfileSplit => NavyProfileSplitPdfTemplate.Render(document),
            CvExportTemplateId.ForestGreenSidebar => ForestGreenSidebarPdfTemplate.Render(document),
            CvExportTemplateId.YellowSkillDots => YellowSkillDotsPdfTemplate.Render(document),
            CvExportTemplateId.RoyalBlueSidebar => RoyalBlueSidebarPdfTemplate.Render(document),
            CvExportTemplateId.OrangeTimeline => OrangeTimelinePdfTemplate.Render(document),
            CvExportTemplateId.BlueAccentSummary => BlueAccentSummaryPdfTemplate.Render(document),
            CvExportTemplateId.PillHeaderSplit => PillHeaderSplitPdfTemplate.Render(document),
            CvExportTemplateId.NavyOverlapPhoto => NavyOverlapPhotoPdfTemplate.Render(document),
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
