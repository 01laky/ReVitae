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
	[InlineData(CvQualityHintIds.PersonalSummaryTooLong, AiCvTaskKind.ShortenProfessionalSummary)]
	[InlineData(CvQualityHintIds.SkillsSingleLargeGroup, AiCvTaskKind.SuggestSkillGrouping)]
	[InlineData(CvQualityHintIds.SkillsSectionEmpty, AiCvTaskKind.DraftSkillsFromContext)]
	[InlineData(CvQualityHintIds.EducationSectionEmpty, AiCvTaskKind.AdviseEducationSection)]
	[InlineData(CvQualityHintIds.LanguagesSectionEmpty, AiCvTaskKind.AdviseLanguagesSection)]
	public void TryGetTaskForQualityHint_MapsSupportedHints(string hintId, AiCvTaskKind expected)
	{
		Assert.Equal(expected, AiCvTaskRegistry.TryGetTaskForQualityHint(hintId));
	}

	[Theory]
	[InlineData(CvQualityHintIds.WorkSectionEmpty)]
	[InlineData(CvQualityHintIds.ImportReviewField)]
	[InlineData(CvQualityHintIds.LinksDuplicatePersonalUrl)]
	public void SupportsQualityHint_UnsupportedHintsReturnFalse(string hintId)
	{
		Assert.False(AiCvTaskRegistry.SupportsQualityHint(hintId));
	}

	[Theory]
	[InlineData(AiCvTaskKind.SectionAdvisor)]
	[InlineData(AiCvTaskKind.SuggestSkillGrouping)]
	[InlineData(AiCvTaskKind.DraftSkillsFromContext)]
	[InlineData(AiCvTaskKind.AdviseEducationSection)]
	[InlineData(AiCvTaskKind.AdviseLanguagesSection)]
	[InlineData(AiCvTaskKind.SuggestMeasurableResults)]
	public void ProducesAdviceList_AdviceTasks_ReturnTrue(AiCvTaskKind task)
	{
		Assert.True(AiCvTaskRegistry.ProducesAdviceList(task));
		Assert.False(AiCvTaskRegistry.ProducesCvContent(task));
	}

	[Theory]
	[InlineData(AiCvTaskKind.ImproveWorkDescription)]
	[InlineData(AiCvTaskKind.ShortenProfessionalSummary)]
	[InlineData(AiCvTaskKind.DraftWorkDescription)]
	public void ProducesAdviceList_SingleValueTasks_ReturnFalse(AiCvTaskKind task)
	{
		Assert.False(AiCvTaskRegistry.ProducesAdviceList(task));
		Assert.True(AiCvTaskRegistry.ProducesCvContent(task));
	}

	[Theory]
	[InlineData(AiCvTaskKind.ImproveWorkDescription, true)]
	[InlineData(AiCvTaskKind.ImproveProfessionalSummary, true)]
	[InlineData(AiCvTaskKind.ShortenProfessionalSummary, true)]
	[InlineData(AiCvTaskKind.ImproveProjectDescription, true)]
	[InlineData(AiCvTaskKind.DraftWorkDescription, false)]
	[InlineData(AiCvTaskKind.SectionAdvisor, false)]
	public void IsRewriteTask_FlagsRewriteTasks(AiCvTaskKind task, bool expected)
	{
		Assert.Equal(expected, AiCvTaskRegistry.IsRewriteTask(task));
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
