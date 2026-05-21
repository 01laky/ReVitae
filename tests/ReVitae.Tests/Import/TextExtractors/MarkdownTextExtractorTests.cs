using ReVitae.Core.Import.Extraction;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Import.TextExtractors;

public sealed class MarkdownTextExtractorTests
{
    [Fact]
    public void Extract_ReturnsFileNotFoundForMissingPath()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".md");
        var result = new MarkdownTextExtractor().Extract(missing);

        Assert.False(result.Success);
        Assert.Equal(TranslationKeys.ImportErrorFileNotFound, result.ErrorMessageKey);
    }

    [Fact]
    public void Extract_ConvertsMarkdownHeadingsToPlainText()
    {
        using var dir = new TempImportDirectory();
        var markdown = """
            # Jane Doe
            Senior Engineer

            ## Summary
            Builds APIs.
            """;
        var path = dir.FilePath("profile.md", markdown);

        var result = new MarkdownTextExtractor().Extract(path);

        Assert.True(result.Success);
        Assert.Contains("Jane Doe", result.Text, StringComparison.Ordinal);
        Assert.Contains("Senior Engineer", result.Text, StringComparison.Ordinal);
        Assert.Contains("Summary", result.Text, StringComparison.Ordinal);
        Assert.Contains("Builds APIs", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Extract_ReturnsEmptyDocumentForWhitespaceOnlyFile()
    {
        using var dir = new TempImportDirectory();
        var path = dir.FilePath("empty.md", "   \n\t ");

        var result = new MarkdownTextExtractor().Extract(path);

        Assert.False(result.Success);
        Assert.Equal(TranslationKeys.ImportErrorEmptyDocument, result.ErrorMessageKey);
    }
}
