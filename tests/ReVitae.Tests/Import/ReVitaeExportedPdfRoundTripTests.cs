using ReVitae.Core.Import;
using ReVitae.Core.Import.Pdf;

namespace ReVitae.Tests.Import;

public sealed class ReVitaeExportedPdfRoundTripTests
{
    [Fact]
    public void Import_ReVitaeExportedSidebarPdf_ParsesCoreSectionsWithoutSkillDump()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Import", "Fixtures", "ReVitaeExportedSidebarCv.pdf");
        Assert.True(File.Exists(path), $"Missing fixture: {path}");

        var result = CvDocumentImporter.Import(path);

        Assert.True(result.Success, result.ErrorMessageKey);
        Assert.Contains("Ladislav", result.Personal.FirstName, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Kostol", result.Personal.LastName, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("01laky@gmail.com", result.Personal.Email, StringComparison.OrdinalIgnoreCase);
        Assert.True(result.WorkExperienceEntries.Count >= 3);
        Assert.InRange(result.SkillsGroups.Count, 1, 8);
        Assert.True(result.SkillsGroups.Sum(group => group.Skills.Count) <= 30);
        Assert.DoesNotContain("TypeScript", result.Personal.FirstName, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Redis", result.Personal.LastName, StringComparison.OrdinalIgnoreCase);

        foreach (var skill in result.SkillsGroups.SelectMany(group => group.Skills))
        {
            Assert.DoesNotContain("reviewed", skill.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Implemented", skill.Name, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Import_ReVitaeExportedSidebarPdf_ReadsReVitaeHintsWhenMetadataPresent()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Import", "Fixtures", "ReVitaeExportedSidebarCv.pdf");
        Assert.True(File.Exists(path));

        var extraction = new PdfTextExtractorAdapter(new PdfPigTextExtractor()).Extract(path);
        Assert.True(extraction.Success);
        Assert.NotNull(extraction.ReVitaeHints);
    }
}
