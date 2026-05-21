using ReVitae.Core.Export;

namespace ReVitae.Tests.Export;

public sealed class CvExportFilenameHelperTests
{
    [Fact]
    public void SuggestFilename_UsesFirstAndLastName()
    {
        var filename = CvExportFilenameHelper.SuggestFilename("Ladislav", "Kostolny");

        Assert.Equal("Ladislav_Kostolny_CV.pdf", filename);
    }

    [Fact]
    public void SuggestFilename_PreservesUnicodeLetters()
    {
        var filename = CvExportFilenameHelper.SuggestFilename("Ladislav", "Kostolný");

        Assert.Equal("Ladislav_Kostolný_CV.pdf", filename);
    }

    [Theory]
    [InlineData(null, "Doe", "ReVitae_CV.pdf")]
    [InlineData("Jane", null, "ReVitae_CV.pdf")]
    [InlineData(" ", "Doe", "ReVitae_CV.pdf")]
    [InlineData("Jane", " ", "ReVitae_CV.pdf")]
    public void SuggestFilename_FallsBackWhenNamePartMissing(string? firstName, string? lastName, string expected)
    {
        var filename = CvExportFilenameHelper.SuggestFilename(firstName, lastName);

        Assert.Equal(expected, filename);
    }

    [Fact]
    public void SuggestFilename_ReplacesInvalidCharactersAndWhitespace()
    {
        var filename = CvExportFilenameHelper.SuggestFilename(" Jane ", "Doe/Jr");

        Assert.Equal("Jane_Doe_Jr_CV.pdf", filename);
    }

    [Fact]
    public void SuggestFilename_TrimsLeadingAndTrailingSeparators()
    {
        var filename = CvExportFilenameHelper.SuggestFilename(".Jane.", ".Doe.");

        Assert.Equal("Jane_Doe_CV.pdf", filename);
    }
}
