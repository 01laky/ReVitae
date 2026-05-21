using System.IO.Compression;
using ReVitae.Core.Import.Extraction;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Import.TextExtractors;

public sealed class AbwTextExtractorTests
{
    [Fact]
    public void Extract_ParsesMinimalAbiWordParagraphs()
    {
        using var dir = new TempImportDirectory();
        const string abw = """
            <?xml version="1.0"?>
            <abiword version="2.0">
              <p>Jane Doe</p>
              <p>Software Engineer at Acme</p>
            </abiword>
            """;
        var path = dir.FilePath("cv.abw", abw);

        var result = new AbwTextExtractor().Extract(path);

        Assert.True(result.Success);
        Assert.Contains("Jane Doe", result.Text, StringComparison.Ordinal);
        Assert.Contains("Software Engineer", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Extract_ReturnsEmptyDocumentForParagraphFreeXml()
    {
        using var dir = new TempImportDirectory();
        const string abw = """<?xml version="1.0"?><abiword version="2.0"></abiword>""";
        var path = dir.FilePath("empty.abw", abw);

        var result = new AbwTextExtractor().Extract(path);

        Assert.False(result.Success);
        Assert.Equal(TranslationKeys.ImportErrorEmptyDocument, result.ErrorMessageKey);
    }
}

public sealed class PagesTextExtractorTests
{
    [Fact]
    public void Extract_ReadsEmbeddedPreviewPdf()
    {
        using var dir = new TempImportDirectory();
        var path = dir.PagesBundle("cv.pages", ["Jane Doe", "Software Engineer at Acme"]);

        var result = new PagesTextExtractor().Extract(path);

        Assert.True(result.Success);
        Assert.Contains("Jane Doe", result.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Extract_ReturnsUnsupportedWhenPreviewPdfMissing()
    {
        using var dir = new TempImportDirectory();
        var path = Path.Combine(dir.RootPath, "missing-preview.pages");
        using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            archive.CreateEntry("Index/Document.iwa");
        }

        var result = new PagesTextExtractor().Extract(path);

        Assert.False(result.Success);
        Assert.Equal(TranslationKeys.ImportErrorUnsupportedFormat, result.ErrorMessageKey);
    }
}

public sealed class WpsTextExtractorTests
{
    [Fact]
    public void Extract_ReturnsUnsupportedForExistingFile()
    {
        using var dir = new TempImportDirectory();
        var path = dir.FilePath("legacy.wps", [0x01, 0x02, 0x03, 0x04]);

        var result = new WpsTextExtractor().Extract(path);

        Assert.False(result.Success);
        Assert.Equal(TranslationKeys.ImportErrorUnsupportedFormat, result.ErrorMessageKey);
    }
}
