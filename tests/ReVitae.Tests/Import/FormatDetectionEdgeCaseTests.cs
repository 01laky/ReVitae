using System.Text;
using ReVitae.Core.Import;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Import;

public sealed class FormatDetectionEdgeCaseTests
{
    [Fact]
    public void DetectFormat_ReturnsUnknownForBlankPath()
    {
        Assert.Equal(CvImportFormat.Unknown, CvImportFormatDetector.DetectFormat(""));
        Assert.Equal(CvImportFormat.Unknown, CvImportFormatDetector.DetectFormat("   "));
    }

    [Fact]
    public void DetectFormat_ReturnsUnknownForUnsupportedExtension()
    {
        using var dir = new TempImportDirectory();
        var path = dir.FilePath("cv.bin", []);

        Assert.Equal(CvImportFormat.Unknown, CvImportFormatDetector.DetectFormat(path));
    }

    [Theory]
    [InlineData(".pdf", CvImportFormat.Pdf)]
    [InlineData(".DOCX", CvImportFormat.Docx)]
    [InlineData(".txt", CvImportFormat.PlainText)]
    [InlineData(".markdown", CvImportFormat.Markdown)]
    [InlineData(".md", CvImportFormat.Markdown)]
    [InlineData(".html", CvImportFormat.Html)]
    [InlineData(".htm", CvImportFormat.Html)]
    [InlineData(".csv", CvImportFormat.CsvTabular)]
    [InlineData(".tsv", CvImportFormat.CsvTabular)]
    [InlineData(".yaml", CvImportFormat.YamlCv)]
    [InlineData(".yml", CvImportFormat.YamlCv)]
    public void DetectFormat_ClassifiesKnownExtensions(string extension, CvImportFormat expected)
    {
        using var dir = new TempImportDirectory();
        var path = dir.FilePath($"stub{extension}", []);

        Assert.Equal(expected, CvImportFormatDetector.DetectFormat(path));
    }

    [Fact]
    public void DetectFormat_ReVitaeSuffix_OnJson_SelectsNativeWithoutOpeningFile()
    {
        using var dir = new TempImportDirectory();
        var path = dir.FilePath("backup.revitae.json", Encoding.UTF8.GetBytes("{not-json"));

        Assert.Equal(CvImportFormat.ReVitaeJson, CvImportFormatDetector.DetectFormat(path));
    }

    [Fact]
    public void DetectFormat_StructuredJson_PeeksBasicsToken()
    {
        using var dir = new TempImportDirectory();
        const string json = """
            {
              "basics": { "name": "Jane Doe", "email": "jane@example.com" }
            }
            """;
        var path = dir.FilePath("resume.json", Encoding.UTF8.GetBytes(json));

        Assert.Equal(CvImportFormat.JsonResume, CvImportFormatDetector.DetectFormat(path));
    }

    [Fact]
    public void DetectFormat_StructuredJson_ReVitaeVersionInsidePlainJsonFilename()
    {
        using var dir = new TempImportDirectory();
        var json = """
            {
              "revitaeVersion": 1,
              "personalInformation": { "email": "j@e.com" }
            }
            """;
        var path = dir.FilePath("export.json", Encoding.UTF8.GetBytes(json));

        Assert.Equal(CvImportFormat.ReVitaeJson, CvImportFormatDetector.DetectFormat(path));
    }

    [Fact]
    public void DetectFormat_StructuredJson_SchemaHintJsonResume()
    {
        using var dir = new TempImportDirectory();
        var json = """
            {
              "$schema": "https://example.org/jsonresume/schema.json",
              "basics": {}
            }
            """;
        var path = dir.FilePath("schema.json", Encoding.UTF8.GetBytes(json));

        Assert.Equal(CvImportFormat.JsonResume, CvImportFormatDetector.DetectFormat(path));
    }

    [Fact]
    public void DetectFormat_StructuredJson_InvalidMarkup_ReturnsUnknown()
    {
        using var dir = new TempImportDirectory();
        var path = dir.FilePath("broken.json", Encoding.UTF8.GetBytes("{"));

        Assert.Equal(CvImportFormat.Unknown, CvImportFormatDetector.DetectFormat(path));
    }

    [Fact]
    public void DetectFormat_StructuredJson_ValidButUnknownShape_ReturnsUnknown()
    {
        using var dir = new TempImportDirectory();
        var path = dir.FilePath("config.json", Encoding.UTF8.GetBytes("""{"foo":true}"""));

        Assert.Equal(CvImportFormat.Unknown, CvImportFormatDetector.DetectFormat(path));
    }

    [Fact]
    public void DetectFormat_StructuredXml_EuropassNamespaceHint()
    {
        using var dir = new TempImportDirectory();
        var xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <root xmlns="http://europass.cedefop.europa.eu/Europass">
              <Identification/>
            </root>
            """;
        var path = dir.FilePath("europass.xml", Encoding.UTF8.GetBytes(xml));

        Assert.Equal(CvImportFormat.EuropassXml, CvImportFormatDetector.DetectFormat(path));
    }

    [Fact]
    public void DetectFormat_StructuredXml_HrXmlResumeTokenHint()
    {
        using var dir = new TempImportDirectory();
        var xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <CandidateResume>
              <Resume>
                <EmploymentHistory/>
              </Resume>
            </CandidateResume>
            """;
        var path = dir.FilePath("hr.xml", Encoding.UTF8.GetBytes(xml));

        Assert.Equal(CvImportFormat.HrXml, CvImportFormatDetector.DetectFormat(path));
    }

    [Fact]
    public void DetectFormat_StructuredXml_NonCvMarkup_ReturnsUnknown()
    {
        using var dir = new TempImportDirectory();
        var xml = """<?xml version="1.0"?><configuration><setting value="x"/></configuration>""";
        var path = dir.FilePath("app.xml", Encoding.UTF8.GetBytes(xml));

        Assert.Equal(CvImportFormat.Unknown, CvImportFormatDetector.DetectFormat(path));
    }

    [Fact]
    public void DetectFormat_StructuredXml_InvalidMarkup_ReturnsUnknown()
    {
        using var dir = new TempImportDirectory();
        var path = dir.FilePath("broken.xml", Encoding.UTF8.GetBytes("<root><"));

        Assert.Equal(CvImportFormat.Unknown, CvImportFormatDetector.DetectFormat(path));
    }
}
