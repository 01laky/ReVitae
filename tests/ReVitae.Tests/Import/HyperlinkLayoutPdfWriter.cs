namespace ReVitae.Tests.Import;

/// <summary>Minimal PDF with a hyperlink annotation and no visible GitHub URL text.</summary>
internal static class HyperlinkLayoutPdfWriter
{
	public static byte[] CreateGitHubHyperlinkOnlySidebar()
	{
		var layout = SidebarLayoutPdfWriter.CreateSinglePageTwoColumnLayout();
		return SidebarLayoutPdfWriter.CreateWithHyperlinks(
			layout,
			[new SidebarLayoutPdfWriter.HyperlinkAnnotation(SidebarLayoutPdfWriter.MainColumnX + 10, 700, "https://github.com/johndoe")]);
	}
}
