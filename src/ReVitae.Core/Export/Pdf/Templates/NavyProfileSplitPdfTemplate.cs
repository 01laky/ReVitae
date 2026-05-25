namespace ReVitae.Core.Export.Pdf.Templates;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using ReVitae.Core.Export.Pdf;

internal static class NavyProfileSplitPdfTemplate
{
    private const string Navy = "#1B2A41";
    private const string Orange = "#E67E22";

    public static byte[] Render(CvExportDocument document)
    {
        return CvPdfRenderHelper.Generate(document, container =>
        {
            container.Page(page =>
            {
                CvPdfLayoutHelpers.ConfigureA4Page(page);
                page.Content().Column(root =>
                {
                    root.Spacing(10);
                    root.Item().Background(Navy).Padding(16).AlignCenter().Column(header =>
                    {
                        header.Item().Text(text =>
                        {
                            text.Span(document.FirstName + " ").FontSize(24).Bold().FontColor(Orange);
                            text.Span(document.LastName).FontSize(24).Bold().FontColor(Colors.White);
                        });
                        CvPdfExtendedHelpers.ComposeContactLine(header.Item().AlignCenter(), document, Colors.White);
                    });
                    root.Item().Row(summaryRow =>
                    {
                        summaryRow.RelativeItem().Text(text =>
                        {
                            text.Span(document.ProfessionalTitle + " — ").Bold();
                            text.Span(CvExportPreviewContentBuilder.BuildSummary(document));
                        });
                        summaryRow.ConstantItem(72).Element(c =>
                            CvPdfPhotoHelpers.ComposeSidebarPhotoOrInitials(c, document, 68, "#CCCCCC", Navy));
                    });
                    root.Item().Row(body =>
                    {
                        body.RelativeItem(64).Column(left =>
                        {
                            CvPdfLayoutHelpers.ComposeSection(left, document.Labels.PreviewWorkExperience,
                                CvExportPreviewContentBuilder.BuildWorkExperiencePreviewContent(document), Orange);
                        });
                        body.RelativeItem(36).PaddingLeft(10).Column(right =>
                        {
                            CvPdfLayoutHelpers.ComposeSection(right, document.Labels.PreviewSkills,
                                CvExportPreviewContentBuilder.BuildSkillsPreviewContent(document), Orange);
                            CvPdfLayoutHelpers.ComposeSection(right, document.Labels.PreviewEducation,
                                CvExportPreviewContentBuilder.BuildEducationPreviewContent(document), Orange);
                        });
                    });
                });
            });
        });
    }
}
