using ReVitae.Core.Import.Ocr;

namespace ReVitae.Tests.Import.Ocr;

public sealed class CvTextQualityGateTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsUsable_RejectsEmptyText(string? text)
    {
        Assert.False(CvTextQualityGate.IsUsable(text, pageCount: 1));
    }

    [Fact]
    public void IsUsable_RejectsVeryShortText()
    {
        Assert.False(CvTextQualityGate.IsUsable("Too short for a CV document.", pageCount: 1));
    }

    [Fact]
    public void IsUsable_RejectsSparseMultiPageText()
    {
        const string sparse = "abc def";
        Assert.False(CvTextQualityGate.IsUsable(sparse, pageCount: 10));
    }

    [Fact]
    public void IsUsable_AcceptsTypicalCvText()
    {
        const string text = """
            Jane Doe
            jane@example.com
            Senior Engineer with ten years of experience building products.
            """;

        Assert.True(CvTextQualityGate.IsUsable(text, pageCount: 1));
    }

    [Fact]
    public void IsUsable_AcceptsReVitaeExportedSidebarPdfText()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Import", "Fixtures", "ReVitaeExportedSidebarCv.pdf");
        if (!File.Exists(path))
        {
            return;
        }

        var pig = new ReVitae.Core.Import.Pdf.PdfPigTextExtractor();
        var legacy = pig.Extract(path);
        Assert.True(legacy.Success);

        Assert.True(CvTextQualityGate.IsUsable(legacy.Text, legacy.PageCount));
    }
}
