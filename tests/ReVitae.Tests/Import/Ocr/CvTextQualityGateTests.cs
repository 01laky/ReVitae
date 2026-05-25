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
    public void Evaluate_Exactly40NonWhitespaceChars_OnOnePage_IsUsable()
    {
        var text = new string('a', 40);
        var gate = CvTextQualityGate.Evaluate(text, pageCount: 1);

        Assert.True(gate.IsUsable);
        Assert.Equal(40, gate.NonWhitespaceCount);
        Assert.Null(gate.RejectReason);
    }

    [Fact]
    public void Evaluate_39NonWhitespaceChars_RejectsWithThresholdReason()
    {
        var text = new string('a', 39);
        var gate = CvTextQualityGate.Evaluate(text, pageCount: 1);

        Assert.False(gate.IsUsable);
        Assert.Contains("39", gate.RejectReason, StringComparison.Ordinal);
        Assert.Contains("40", gate.RejectReason, StringComparison.Ordinal);
    }

    [Fact]
    public void Evaluate_PageCountNull_BehavesLikeSinglePage()
    {
        var text = new string('a', 40);
        var gate = CvTextQualityGate.Evaluate(text, pageCount: null);

        Assert.True(gate.IsUsable);
        Assert.Null(gate.AverageNonWhitespacePerPage);
    }

    [Fact]
    public void Evaluate_TwoPages_At8CharsPerPageBoundary_IsUsable()
    {
        var text = new string('a', 40);
        var gate = CvTextQualityGate.Evaluate(text, pageCount: 2);

        Assert.True(gate.IsUsable);
        Assert.Equal(20.0, gate.AverageNonWhitespacePerPage);
    }

    [Fact]
    public void Evaluate_TenPages_Below8CharsPerPage_Rejects()
    {
        var text = new string('a', 79);
        var gate = CvTextQualityGate.Evaluate(text, pageCount: 10);

        Assert.False(gate.IsUsable);
        Assert.Contains("chars/page", gate.RejectReason, StringComparison.Ordinal);
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
