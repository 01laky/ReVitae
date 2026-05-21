using ReVitae.Core.Import.Extraction;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Import.TextExtractors;

public sealed class HtmlTextExtractorTests
{
    [Fact]
    public void Extract_ReturnsFileNotFoundForMissingPath()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".html");
        var result = new HtmlTextExtractor().Extract(missing);

        Assert.False(result.Success);
        Assert.Equal(TranslationKeys.ImportErrorFileNotFound, result.ErrorMessageKey);
    }

    [Fact]
    public void Extract_StripsScriptsAndKeepsVisibleCopy()
    {
        using var dir = new TempImportDirectory();
        var markup = """
            <html><body>
              <script>document.write('evil')</script>
              <p>Jane Doe</p>
              <a href="https://example.com/profile">Portfolio</a>
            </body></html>
            """;
        var path = dir.FilePath("page.html", markup);

        var result = new HtmlTextExtractor().Extract(path);

        Assert.True(result.Success);
        Assert.DoesNotContain("evil", result.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Jane Doe", result.Text, StringComparison.Ordinal);
        Assert.NotNull(result.HyperlinkUrls);
        Assert.Contains("https://example.com/profile", result.HyperlinkUrls!, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void FromHtmlMarkup_NormalizesWhitespaceAroundAnchors()
    {
        var result = HtmlTextExtractor.FromHtmlMarkup("""<p><a href="https://github.com/jane">GitHub</a></p>""");

        Assert.True(result.Success);
        Assert.Contains("GitHub", result.Text, StringComparison.Ordinal);
        Assert.Contains("https://github.com/jane", result.HyperlinkUrls!, StringComparer.OrdinalIgnoreCase);
    }
}
