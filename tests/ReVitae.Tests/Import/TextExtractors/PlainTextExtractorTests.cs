using System.Text;
using ReVitae.Core.Import.Extraction;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Import.TextExtractors;

public sealed class PlainTextExtractorTests
{
    [Fact]
    public void Extract_ReturnsFileNotFoundForMissingPath()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
        var result = new PlainTextExtractor().Extract(missing);

        Assert.False(result.Success);
        Assert.Equal(TranslationKeys.ImportErrorFileNotFound, result.ErrorMessageKey);
    }

    [Fact]
    public void Extract_DecodesUtf8BomAndPreservesCharacters()
    {
        using var dir = new TempImportDirectory();
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes("Župa\r\nČaj")).ToArray();
        var path = dir.FilePath("unicode.txt", bytes);

        var result = new PlainTextExtractor().Extract(path);

        Assert.True(result.Success);
        Assert.Contains("Župa", result.Text, StringComparison.Ordinal);
        Assert.Contains("Čaj", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Extract_ReturnsEmptyDocumentForZeroByteFile()
    {
        using var dir = new TempImportDirectory();
        var path = dir.FilePath("empty.txt", []);

        var result = new PlainTextExtractor().Extract(path);

        Assert.False(result.Success);
        Assert.Equal(TranslationKeys.ImportErrorEmptyDocument, result.ErrorMessageKey);
    }

    [Fact]
    public void Extract_ReturnsEmptyDocumentWhenOnlyWhitespace()
    {
        using var dir = new TempImportDirectory();
        var path = dir.FilePath("spaces.txt", " \n ");

        var result = new PlainTextExtractor().Extract(path);

        Assert.False(result.Success);
        Assert.Equal(TranslationKeys.ImportErrorEmptyDocument, result.ErrorMessageKey);
    }
}
