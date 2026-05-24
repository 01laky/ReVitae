using ReVitae.Core.Ai.Cv;
using ReVitae.Core.Quality;

namespace ReVitae.Tests.Ai.Cv;

public sealed class AiCvTaskRegistryTests
{
    [Theory]
    [InlineData(CvQualityHintIds.WorkGenericDescription, AiCvTaskKind.ImproveWorkDescription)]
    [InlineData(CvQualityHintIds.WorkEntryMissingDescription, AiCvTaskKind.DraftWorkDescription)]
    [InlineData(CvQualityHintIds.PersonalSummaryTooShort, AiCvTaskKind.ImproveProfessionalSummary)]
    [InlineData(CvQualityHintIds.PersonalSummaryMissing, AiCvTaskKind.DraftProfessionalSummary)]
    [InlineData(CvQualityHintIds.ProjectsEntryMissingDescription, AiCvTaskKind.ImproveProjectDescription)]
    public void TryGetTaskForQualityHint_MapsSupportedHints(string hintId, AiCvTaskKind expected)
    {
        Assert.Equal(expected, AiCvTaskRegistry.TryGetTaskForQualityHint(hintId));
    }

    [Theory]
    [InlineData(CvQualityHintIds.WorkSectionEmpty)]
    [InlineData(CvQualityHintIds.SkillsSectionEmpty)]
    [InlineData(CvQualityHintIds.ImportReviewField)]
    public void SupportsQualityHint_UnsupportedHintsReturnFalse(string hintId)
    {
        Assert.False(AiCvTaskRegistry.SupportsQualityHint(hintId));
    }

    [Fact]
    public void ResolveFieldTarget_ReturnsHintFieldTarget()
    {
        var hint = new CvQualityHint(
            CvQualityHintIds.WorkGenericDescription,
            "key",
            CvQualityHintSeverity.Suggestion,
            Core.Import.CvImportSectionId.WorkExperience,
            "workExperience.entry1.description",
            "entry1");

        var target = AiCvTaskRegistry.ResolveFieldTarget(hint);

        Assert.Equal(Core.Import.CvImportSectionId.WorkExperience, target.Section);
        Assert.Equal("workExperience.entry1.description", target.FieldKey);
        Assert.Equal("entry1", target.EntryId);
    }
}
