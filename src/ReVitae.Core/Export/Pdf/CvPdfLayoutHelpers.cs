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

	/// <summary>
	/// Renders a two-column page whose coloured sidebar band spans the <b>full height of every
	/// page</b> (including continuation pages), fixing the gap left when a short sidebar is paired
	/// with longer main content. The band is painted via <c>page.Background()</c> so it always
	/// reaches the page edges; page margin is overridden to 0 and replaced by per-column padding
	/// so the band aligns exactly with the sidebar content column.
	/// </summary>
	public static void ComposeFullHeightSidebarPage(
		PageDescriptor page,
		int sidebarWeight,
		int mainWeight,
		string sidebarColor,
		bool sidebarOnLeft,
		Action<ColumnDescriptor> composeSidebar,
		Action<ColumnDescriptor> composeMain,
		float sidebarPadding = 16f,
		float mainPadding = 22f)
	{
		page.Margin(0);

		page.Background().Row(bg =>
		{
			if (sidebarOnLeft)
			{
				bg.RelativeItem(sidebarWeight).Background(sidebarColor);
				bg.RelativeItem(mainWeight);
			}
			else
			{
				bg.RelativeItem(mainWeight);
				bg.RelativeItem(sidebarWeight).Background(sidebarColor);
			}
		});

		page.Content().Row(row =>
		{
			if (sidebarOnLeft)
			{
				row.RelativeItem(sidebarWeight).Padding(sidebarPadding).Column(composeSidebar);
				row.RelativeItem(mainWeight).Padding(mainPadding).Column(composeMain);
			}
			else
			{
				row.RelativeItem(mainWeight).Padding(mainPadding).Column(composeMain);
				row.RelativeItem(sidebarWeight).Padding(sidebarPadding).Column(composeSidebar);
			}
		});
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
