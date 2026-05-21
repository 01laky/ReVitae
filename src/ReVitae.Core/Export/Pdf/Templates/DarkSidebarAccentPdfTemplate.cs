namespace ReVitae.Core.Export.Pdf.Templates;

using QuestPDF.Fluent;
using QuestPDF.Helpers;

internal static class DarkSidebarAccentPdfTemplate
{
    public static byte[] Render(CvExportDocument document)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                CvPdfLayoutHelpers.ConfigureA4Page(page, "#F2F2F2");

                page.Content().Row(row =>
                {
                    row.RelativeItem(34).Background("#2F3A45").Padding(16).Column(sidebar =>
                    {
                        sidebar.Spacing(10);
                        sidebar.Item().Text(document.Labels.Contact.ToUpperInvariant()).FontSize(16).Bold().FontColor(Colors.White);
                        sidebar.Item().Text(CvExportPreviewContentBuilder.BuildContactLines(document)).FontColor(Colors.White);
                    });

                    row.RelativeItem(66).PaddingLeft(14).Column(content =>
                    {
                        content.Item().Background("#5B9BB0").Padding(16).Column(header =>
                        {
                            header.Spacing(4);
                            header.Item().Text(document.FullName.ToUpperInvariant()).FontSize(26).Bold().FontColor(Colors.White);
                            header.Item().Text(document.ProfessionalTitle.ToUpperInvariant()).FontSize(13).SemiBold().FontColor(Colors.White);
                        });

                        content.Item().PaddingTop(10).Column(sections =>
                        {
                            CvPdfSectionContent.ComposeAllSections(
                                sections,
                                document,
                                document.Labels.Objective,
                                document.Labels.Online,
                                CvExportPreviewContentBuilder.BuildOnlineLines(document));
                        });
                    });
                });
            });
        }).GeneratePdf();
    }
}
