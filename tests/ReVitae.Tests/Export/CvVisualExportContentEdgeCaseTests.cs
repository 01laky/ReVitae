using System.Text;
using ReVitae.Core.Export;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Export;

public sealed class CvVisualExportContentEdgeCaseTests
{
	private static readonly AppLocalizer Localizer = new(AppLocalizer.FallbackLanguageCode);

	[Fact]
	public void Export_Html_EscapesSpecialCharacters()
	{
		var document = CreateDocumentWithSpecialChars();
		var bytes = ExportVisual(CvExportFormat.Html, document);

		var html = Encoding.UTF8.GetString(bytes);
		Assert.Contains("&lt;script&gt;", html, StringComparison.Ordinal);
		Assert.Contains("&amp;", html, StringComparison.Ordinal);
		Assert.DoesNotContain("<script>alert", html, StringComparison.Ordinal);
	}

	[Fact]
	public void Export_Markdown_PreservesHeadingMarkers()
	{
		var document = CreateDocumentWithSpecialChars();
		var bytes = ExportVisual(CvExportFormat.Markdown, document);

		var markdown = Encoding.UTF8.GetString(bytes);
		Assert.Contains("#", markdown, StringComparison.Ordinal);
		Assert.Contains("Jane", markdown, StringComparison.Ordinal);
	}

	[Fact]
	public void Export_Latex_EscapesSpecialCharacters()
	{
		var document = CreateDocumentWithSpecialChars();
		var bytes = ExportVisual(CvExportFormat.Latex, document);

		var latex = Encoding.UTF8.GetString(bytes);
		Assert.Contains("\\", latex, StringComparison.Ordinal);
		Assert.DoesNotContain("#", latex.Split('\n').First(), StringComparison.Ordinal);
	}

	[Fact]
	public void Export_Rtf_ContainsEscapedContent()
	{
		var document = CreateDocumentWithSpecialChars();
		var bytes = ExportVisual(CvExportFormat.Rtf, document);

		var rtf = Encoding.UTF8.GetString(bytes);
		Assert.StartsWith("{\\rtf", rtf, StringComparison.Ordinal);
		Assert.Contains("Jane", rtf, StringComparison.Ordinal);
	}

	[Fact]
	public void Export_PlainText_IncludesSummaryWithoutHtmlEscaping()
	{
		var document = CreateDocumentWithSpecialChars();
		var bytes = ExportVisual(CvExportFormat.Txt, document);

		var text = Encoding.UTF8.GetString(bytes);
		Assert.Contains("Expert in C# & .NET", text, StringComparison.Ordinal);
		Assert.DoesNotContain("&lt;", text, StringComparison.Ordinal);
	}

	[Fact]
	public void Export_Html_IncludesContactSection()
	{
		var document = CvExportTestFixtures.CreateRepresentativeDocument(localizer: Localizer);
		var bytes = ExportVisual(CvExportFormat.Html, document);

		var html = Encoding.UTF8.GetString(bytes);
		Assert.Contains(Localizer.Get(TranslationKeys.Contact), html, StringComparison.Ordinal);
	}

	[Fact]
	public void Export_Markdown_IncludesWorkExperienceSection()
	{
		var document = CvExportTestFixtures.CreateRepresentativeDocument(localizer: Localizer);
		var bytes = ExportVisual(CvExportFormat.Markdown, document);

		var markdown = Encoding.UTF8.GetString(bytes);
		Assert.Contains(Localizer.Get(TranslationKeys.PreviewWorkExperience), markdown, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void Export_Html_WithEmptySummary_DoesNotThrow()
	{
		var document = CvExportTestFixtures.CreatePersonalInfoOnlyDocument();
		var bytes = ExportVisual(CvExportFormat.Html, document);

		Assert.True(bytes.Length > 0);
	}

	private static CvExportDocument CreateDocumentWithSpecialChars()
	{
		var baseDocument = CvExportTestFixtures.CreatePersonalInfoOnlyDocument();
		return baseDocument with
		{
			FirstName = "Jane <script>alert(\"xss\")</script>",
			LastName = "Doe",
			ShortSummary = "Expert in C# & .NET with 100% focus.",
		};
	}

	private static byte[] ExportVisual(CvExportFormat format, CvExportDocument document)
	{
		using var stream = new MemoryStream();
		var source = CvExportTestFixtures.CreatePersonalOnlySourceData();
		var result = CvDocumentExporter.Export(document, source, format, stream);
		Assert.True(result.Success, format.ToString());
		return stream.ToArray();
	}
}
