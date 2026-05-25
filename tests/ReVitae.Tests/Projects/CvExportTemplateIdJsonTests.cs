using System.Text.Json;
using ReVitae.Core.Cv.Education;
using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Export;
using ReVitae.Core.Export.Fixtures;
using ReVitae.Core.Import;
using ReVitae.Core.Projects;

namespace ReVitae.Tests.Projects;

public sealed class CvExportTemplateIdJsonTests
{
    [Theory]
    [InlineData(CvExportTemplateId.ModernSidebar, "modernSidebar")]
    [InlineData(CvExportTemplateId.CleanTopHeader, "cleanTopHeader")]
    [InlineData(CvExportTemplateId.NavyOverlapPhoto, "navyOverlapPhoto")]
    public void ToJsonId_UsesCamelCase(CvExportTemplateId templateId, string expected)
    {
        Assert.Equal(expected, CvExportTemplateIdJson.ToJsonId(templateId));
    }

    [Fact]
    public void ParseOrDefault_RecognizesCamelCase()
    {
        var parsed = CvExportTemplateIdJson.ParseOrDefault("modernSidebar", out var recognized);

        Assert.True(recognized);
        Assert.Equal(CvExportTemplateId.ModernSidebar, parsed);
    }

    [Fact]
    public void ParseOrDefault_UnknownValue_ReturnsDefault()
    {
        var parsed = CvExportTemplateIdJson.ParseOrDefault("unknown-template", out var recognized);

        Assert.False(recognized);
        Assert.Equal(CvExportTemplateId.CleanTopHeader, parsed);
    }

    [Fact]
    public void ParseOrDefault_Null_ReturnsDefault()
    {
        var parsed = CvExportTemplateIdJson.ParseOrDefault(null, out var recognized);

        Assert.False(recognized);
        Assert.Equal(CvExportTemplateId.CleanTopHeader, parsed);
    }
}
