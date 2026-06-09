using ReVitae.Core.Ai.Cv;
using ReVitae.Core.Import;

namespace ReVitae.Tests.Ai.Cv;

public sealed class AiCvAdvisorPromptBuilderEdgeCaseTests
{
	private static AiCvCompletionContext Advisor(
		CvImportSectionId section,
		string content,
		bool empty = false,
		AiCvTargetContext? target = null) =>
		new(AiCvTaskKind.SectionAdvisor, content, Section: section, SectionContent: content, SectionIsEmpty: empty, TargetContext: target);

	[Fact]
	public void Build_SkillGrouping_ForbidsInventingSkills()
	{
		var ctx = new AiCvCompletionContext(
			AiCvTaskKind.SuggestSkillGrouping, "C#, SQL", Section: CvImportSectionId.Skills, SectionContent: "C#, SQL");
		var m = AiCvPromptBuilder.Build(AiCvTaskKind.SuggestSkillGrouping, ctx, "en");
		Assert.Contains("Do not invent skills", m.UserPrompt, StringComparison.Ordinal);
	}

	[Fact]
	public void Build_AdviseLanguages_IsAdviceOnly()
	{
		var ctx = new AiCvCompletionContext(
			AiCvTaskKind.AdviseLanguagesSection, string.Empty, Section: CvImportSectionId.Languages, SectionIsEmpty: true);
		var m = AiCvPromptBuilder.Build(AiCvTaskKind.AdviseLanguagesSection, ctx, "en");
		Assert.Contains("Languages", m.UserPrompt, StringComparison.Ordinal);
		Assert.Contains("guidance only", m.UserPrompt, StringComparison.Ordinal);
	}

	[Fact]
	public void Build_TargetContext_RoleOnly_OmitsJobDescription()
	{
		var m = AiCvPromptBuilder.Build(
			AiCvTaskKind.SectionAdvisor,
			Advisor(CvImportSectionId.Skills, "C#", target: new AiCvTargetContext("Engineer", null)),
			"en");
		Assert.Contains("Target role: Engineer", m.SystemPrompt, StringComparison.Ordinal);
		Assert.DoesNotContain("Job description:", m.SystemPrompt, StringComparison.Ordinal);
	}

	[Fact]
	public void Build_TargetContext_JobDescriptionOnly_OmitsRole()
	{
		var m = AiCvPromptBuilder.Build(
			AiCvTaskKind.SectionAdvisor,
			Advisor(CvImportSectionId.Skills, "C#", target: new AiCvTargetContext(null, "We use Kubernetes")),
			"en");
		Assert.Contains("Job description: We use Kubernetes", m.SystemPrompt, StringComparison.Ordinal);
		Assert.DoesNotContain("Target role:", m.SystemPrompt, StringComparison.Ordinal);
	}

	[Fact]
	public void Build_TargetContext_AllWhitespace_TreatedAsAbsent()
	{
		var m = AiCvPromptBuilder.Build(
			AiCvTaskKind.SectionAdvisor,
			Advisor(CvImportSectionId.Skills, "C#", target: new AiCvTargetContext("  ", "  ")),
			"en");
		Assert.DoesNotContain("Target role", m.SystemPrompt, StringComparison.Ordinal);
	}

	[Fact]
	public void Build_SectionAdvisor_NonEmpty_IncludesContentAndImproveAsk()
	{
		var m = AiCvPromptBuilder.Build(
			AiCvTaskKind.SectionAdvisor,
			Advisor(CvImportSectionId.Skills, "C#, SQL, Azure"),
			"en");
		Assert.Contains("C#, SQL, Azure", m.UserPrompt, StringComparison.Ordinal);
		Assert.Contains("Suggest concrete improvements", m.UserPrompt, StringComparison.Ordinal);
	}

	[Fact]
	public void Build_AdviceTask_UsesAdvisorSystemRole_NotPlainRole()
	{
		var advice = AiCvPromptBuilder.Build(
			AiCvTaskKind.SectionAdvisor, Advisor(CvImportSectionId.Skills, "C#"), "en");
		Assert.Contains("CV reviewer", advice.SystemPrompt, StringComparison.Ordinal);

		var single = AiCvPromptBuilder.Build(
			AiCvTaskKind.ImproveWorkDescription,
			new AiCvCompletionContext(AiCvTaskKind.ImproveWorkDescription, "did work", "Engineer", "Acme"),
			"en");
		Assert.Contains("CV writing assistant", single.SystemPrompt, StringComparison.Ordinal);
	}

	[Fact]
	public void Build_DraftWorkDescription_EmptyText_WarnsAgainstInventingCompany()
	{
		var m = AiCvPromptBuilder.Build(
			AiCvTaskKind.DraftWorkDescription,
			new AiCvCompletionContext(AiCvTaskKind.DraftWorkDescription, string.Empty, "Engineer"),
			"en");
		Assert.Contains("do not invent a company", m.UserPrompt, StringComparison.Ordinal);
	}

	[Fact]
	public void Build_UnknownTask_Throws()
	{
		Assert.Throws<ArgumentOutOfRangeException>(
			() => AiCvPromptBuilder.Build(AiCvTaskKind.ExtractCvImportBatch,
				new AiCvCompletionContext(AiCvTaskKind.ExtractCvImportBatch, "x"), "en"));
	}

	[Fact]
	public void ResolveLanguageInstruction_SlovakCulture_WritesSlovak()
	{
		Assert.Contains("Slovak", AiCvPromptBuilder.ResolveLanguageInstruction("sk-SK"), StringComparison.Ordinal);
		Assert.Contains("English", AiCvPromptBuilder.ResolveLanguageInstruction("en-US"), StringComparison.Ordinal);
	}
}
