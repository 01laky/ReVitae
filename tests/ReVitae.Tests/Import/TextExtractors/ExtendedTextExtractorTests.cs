using ReVitae.Core.Import.Extraction;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Import.TextExtractors;

public sealed class RtfTextExtractorTests
{
    [Fact]
    public void Extract_ReturnsFileNotFoundForMissingPath()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".rtf");
        var result = new RtfTextExtractor().Extract(missing);

        Assert.False(result.Success);
        Assert.Equal(TranslationKeys.ImportErrorFileNotFound, result.ErrorMessageKey);
    }

    [Fact]
    public void Extract_ParsesMinimalRtfDocument()
    {
        using var dir = new TempImportDirectory();
        const string rtf = """
            {\rtf1\ansi\deff0
            {\fonttbl{\f0 Times New Roman;}}
            \f0\fs24 Jane Doe\par
            Software Engineer at Acme\par
            }
            """;
        var path = dir.FilePath("cv.rtf", rtf);

        var result = new RtfTextExtractor().Extract(path);

        Assert.True(result.Success);
        Assert.Contains("Jane Doe", result.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Software Engineer", result.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Extract_ReturnsUnreadableForInvalidRtfBytes()
    {
        using var dir = new TempImportDirectory();
        var path = dir.FilePath("broken.rtf", [0x00, 0x01, 0x02, 0x03]);

        var result = new RtfTextExtractor().Extract(path);

        Assert.False(result.Success);
        Assert.Equal(TranslationKeys.ImportErrorUnreadableDocument, result.ErrorMessageKey);
    }
}

public sealed class LatexTextExtractorTests
{
    [Fact]
    public void Extract_NormalizesSectionCommandsAndEmitsWarning()
    {
        using var dir = new TempImportDirectory();
        const string latex = """
            \documentclass{article}
            \begin{document}
            \section{Work Experience}
            \textbf{Engineer} at Acme Corp
            \end{document}
            """;
        var path = dir.FilePath("cv.tex", latex);

        var result = new LatexTextExtractor().Extract(path);

        Assert.True(result.Success);
        Assert.Contains("Work Experience", result.Text, StringComparison.Ordinal);
        Assert.Contains("Engineer", result.Text, StringComparison.Ordinal);
        Assert.NotNull(result.Warnings);
        Assert.Contains(result.Warnings, warning => warning.MessageKey == TranslationKeys.ImportWarningLatexPartiallyNormalized);
    }

    [Fact]
    public void Extract_ReturnsEmptyDocumentForCommentsOnly()
    {
        using var dir = new TempImportDirectory();
        var path = dir.FilePath("comments.tex", "% only a comment\n% another");

        var result = new LatexTextExtractor().Extract(path);

        Assert.False(result.Success);
        Assert.Equal(TranslationKeys.ImportErrorEmptyDocument, result.ErrorMessageKey);
    }
}

public sealed class OdtTextExtractorTests
{
    [Fact]
    public void Extract_ParsesMinimalOdtParagraphs()
    {
        using var dir = new TempImportDirectory();
        var path = dir.FilePath("cv.odt", MinimalOdtWriter.CreateWithParagraphs("Jane Doe", "Software Engineer"));

        var result = new OdtTextExtractor().Extract(path);

        Assert.True(result.Success);
        Assert.Contains("Jane Doe", result.Text, StringComparison.Ordinal);
        Assert.Contains("Software Engineer", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Extract_ReturnsUnreadableForCorruptZip()
    {
        using var dir = new TempImportDirectory();
        var path = dir.FilePath("broken.odt", "not-a-zip");

        var result = new OdtTextExtractor().Extract(path);

        Assert.False(result.Success);
        Assert.Equal(TranslationKeys.ImportErrorUnreadableDocument, result.ErrorMessageKey);
    }
}

public sealed class DocTextExtractorTests
{
    [Fact]
    public void Extract_RecoversPrintableAsciiRunsFromBinaryPayload()
    {
        using var dir = new TempImportDirectory();
        var payload = new byte[128];
        Array.Fill(payload, (byte)0x00);
        var ascii = "Jane Doe Software Engineer at Acme Corporation"u8.ToArray();
        Array.Copy(ascii, 0, payload, 40, ascii.Length);
        var path = dir.FilePath("legacy.doc", payload);

        var result = new DocTextExtractor().Extract(path);

        Assert.True(result.Success);
        Assert.Contains("Jane Doe", result.Text, StringComparison.Ordinal);
        Assert.NotNull(result.Warnings);
        Assert.Contains(result.Warnings, warning => warning.MessageKey == TranslationKeys.ImportWarningPartialDocumentContent);
    }
}
