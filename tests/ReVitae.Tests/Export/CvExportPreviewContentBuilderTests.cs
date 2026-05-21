using ReVitae.Core.Export;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Export;

public sealed class CvExportPreviewContentBuilderTests
{
    [Fact]
    public void BuildWorkExperiencePreviewContent_IncludesPresentDateRangeWithoutEndDate()
    {
        var localizer = new AppLocalizer("en");
        var document = CvExportTestFixtures.CreateRepresentativeDocument(localizer: localizer);

        var content = CvExportPreviewContentBuilder.BuildWorkExperiencePreviewContent(document);

        Assert.Contains("2020 – súčasnosť", content, StringComparison.Ordinal);
        Assert.Contains("Senior Developer", content, StringComparison.Ordinal);
        Assert.Contains("Košice", content, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildWorkExperiencePreviewContent_ReturnsEmptyWhenNoEntries()
    {
        var document = CvExportTestFixtures.CreatePersonalInfoOnlyDocument();

        var content = CvExportPreviewContentBuilder.BuildWorkExperiencePreviewContent(document);

        Assert.Equal(string.Empty, content);
    }

    [Fact]
    public void BuildSkillsPreviewContent_GroupsSkillsByCategory()
    {
        var document = CvExportTestFixtures.CreateRepresentativeDocument();

        var content = CvExportPreviewContentBuilder.BuildSkillsPreviewContent(document);

        Assert.Contains("Programming", content, StringComparison.Ordinal);
        Assert.Contains("C#", content, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildContactLinksLines_ReturnsEmptyWhenNoLinks()
    {
        var document = CvExportTestFixtures.CreatePersonalInfoOnlyDocument();

        var content = CvExportPreviewContentBuilder.BuildContactLinksLines(document);

        Assert.Equal(string.Empty, content);
    }
}
