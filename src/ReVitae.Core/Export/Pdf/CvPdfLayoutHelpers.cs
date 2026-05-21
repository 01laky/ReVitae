namespace ReVitae.Core.Export.Pdf;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

public static class CvPdfLayoutHelpers
{
    public const float BaseFontSize = 10.5f;

    public static void ConfigureA4Page(PageDescriptor page, string backgroundColor = "#FFFFFF")
    {
        page.Size(PageSizes.A4);
        page.Margin(26);
        page.PageColor(backgroundColor);
        page.DefaultTextStyle(style => style
            .FontFamily("Arial")
            .FontSize(BaseFontSize)
            .LineHeight(1.25f));
    }

    public static void ComposeSection(
        ColumnDescriptor column,
        string title,
        string content,
        string headingColor = "#000000")
    {
        column.Item().Column(section =>
        {
            section.Spacing(5);
            section.Item().Text(title).FontSize(14).SemiBold().FontColor(headingColor);
            section.Item().LineHorizontal(1).LineColor("#B8B8B8");
            section.Item().Text(content).FontSize(BaseFontSize).FontColor(Colors.Black);
        });
    }

    public static void ComposeUppercaseSection(
        ColumnDescriptor column,
        string title,
        string content,
        string headingColor = "#000000")
    {
        ComposeSection(column, title.ToUpperInvariant(), content, headingColor);
    }
}
