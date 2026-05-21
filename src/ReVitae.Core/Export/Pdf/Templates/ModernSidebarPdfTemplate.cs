namespace ReVitae.Core.Export.Pdf.Templates;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using ReVitae.Core.Export.Pdf;

internal static class ModernSidebarPdfTemplate
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
                    row.RelativeItem(34).Background("#D7D7D7").Padding(14).Column(sidebar =>
                    {
                        sidebar.Spacing(14);
                        sidebar.Item().Element(container =>
                            CvPdfPhotoHelpers.ComposeSidebarPhotoOrInitials(
                                container,
                                document,
                                88,
                                "#BBBBBB",
                                "#333333"));
                        CvPdfLayoutHelpers.ComposeSection(
                            sidebar,
                            document.Labels.Contact,
                            CvExportPreviewContentBuilder.BuildLines(
                                document.Labels.Phone, document.Phone,
                                document.Labels.Email, document.Email,
                                document.Labels.LinkedInUrl, document.LinkedInUrl));
                    });

                    row.RelativeItem(66).PaddingLeft(14).Column(content =>
                    {
                        content.Item().Background("#4A4A4A").Padding(12).Text(document.FullName)
                            .FontSize(24)
                            .Bold()
                            .FontColor(Colors.White);

                        content.Item().PaddingTop(10).Column(sections =>
                        {
                            CvPdfSectionContent.ComposeAllSections(
                                sections,
                                document,
                                document.Labels.Profile,
                                document.Labels.Digital,
                                CvExportPreviewContentBuilder.BuildDigitalLines(document));
                        });
                    });
                });
            });
        }).GeneratePdf();
    }
}
