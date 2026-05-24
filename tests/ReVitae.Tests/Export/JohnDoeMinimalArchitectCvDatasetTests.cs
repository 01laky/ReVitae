using ReVitae.Core.Export.Fixtures;

namespace ReVitae.Tests.Export;

public sealed class JohnDoeMinimalArchitectCvDatasetTests
{
    [Fact]
    public void CreateDocument_HasCompactRealisticSections()
    {
        var document = JohnDoeMinimalArchitectCvDataset.CreateDocument();

        Assert.Equal("John", document.FirstName);
        Assert.Equal("Doe", document.LastName);
        Assert.Contains("Senior Software Architect", document.ProfessionalTitle, StringComparison.Ordinal);
        Assert.Equal(2, document.WorkExperienceEntries.Count);
        Assert.Single(document.EducationEntries);
        Assert.Equal(2, document.SkillsGroups.Count);
        Assert.Equal(2, document.LanguageEntries.Count);
        Assert.Single(document.CertificateEntries);
        Assert.Single(document.ProjectEntries);
        Assert.Equal(2, document.CustomLinkLines.Count);
        Assert.False(string.IsNullOrWhiteSpace(document.ShortSummary));
        Assert.False(string.IsNullOrWhiteSpace(document.AdditionalInformationContent));
        Assert.Contains("Meridian Payments", document.WorkExperienceEntries[0].Company, StringComparison.Ordinal);
    }
}
