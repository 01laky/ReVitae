namespace ReVitae.Core.Export.Pdf.Templates;

using QuestPDF.Fluent;

internal static class ClassicSidebarPdfTemplate
{
    public static byte[] Render(CvExportDocument document)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                CvPdfLayoutHelpers.ConfigureA4Page(page);

                page.Content().Row(row =>
                {
                    row.RelativeItem(36).Background("#D8D8D8").Padding(14).Column(sidebar =>
                    {
                        sidebar.Spacing(14);
                        sidebar.Item().Text(document.FirstName).FontSize(24).Bold();
                        sidebar.Item().Text(document.LastName).FontSize(24).Bold().FontColor("#F47C2C");

                        CvPdfLayoutHelpers.ComposeSection(
                            sidebar,
                            document.Labels.Contact,
                            CvExportPreviewContentBuilder.BuildContactLines(document));
                    });

                    row.RelativeItem(64).PaddingLeft(14).Column(content =>
                    {
                        CvPdfSectionContent.ComposeAllSections(
                            content,
                            document,
                            document.Labels.Summary,
                            document.Labels.ContactLinks,
                            CvExportPreviewContentBuilder.BuildContactLinksLines(document));
                    });
                });
            });
        }).GeneratePdf();
    }
}
