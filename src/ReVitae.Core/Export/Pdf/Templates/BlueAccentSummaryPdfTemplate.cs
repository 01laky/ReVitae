namespace ReVitae.Core.Export.Pdf.Templates;

using QuestPDF.Fluent;
using ReVitae.Core.Export.Pdf;

internal static class BlueAccentSummaryPdfTemplate
{
    private const string Blue = "#2C4A93";

    public static byte[] Render(CvExportDocument document)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                CvPdfLayoutHelpers.ConfigureA4Page(page);
                page.Content().Column(root =>
                {
                    root.Spacing(10);
                    root.Item().Row(header =>
                    {
                        header.ConstantItem(12).Height(12).Background(Blue);
                        header.RelativeItem().PaddingLeft(8).Column(title =>
                        {
                            title.Item().Text(document.FullName).FontSize(24).Bold();
                            title.Item().Text($"{document.Phone} // {document.Email}").FontSize(CvPdfLayoutHelpers.BaseFontSize);
                        });
                        header.ConstantItem(72).Element(c =>
                            CvPdfPhotoHelpers.ComposeSidebarPhotoOrInitials(c, document, 68, Blue, "#FFFFFF"));
                    });
                    root.Item().Border(1).BorderColor(Blue).Padding(12)
                        .Text(CvExportPreviewContentBuilder.BuildSummary(document));
                    root.Item().Row(body =>
                    {
                        body.RelativeItem(34).Column(left =>
                        {
                            CvPdfLayoutHelpers.ComposeSection(left, document.Labels.PreviewSkills,
                                CvExportPreviewContentBuilder.BuildSkillsPreviewContent(document), Blue);
                            CvPdfSectionContent.ComposeEducationPublic(left, document);
                        });
                        body.RelativeItem(66).PaddingLeft(10).Column(right =>
                        {
                            CvPdfSectionContent.ComposeWorkExperienceOnly(right, document);
                        });
                    });
                });
            });
        }).GeneratePdf();
    }
}
