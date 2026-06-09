using ReVitae.Core.Ai.Cv;
using ReVitae.Core.Import;

namespace ReVitae.Tests.Ai.Cv;

public sealed class AiCvAdvisorPromptBuilderTests
{
	[Fact]
	public void Build_SectionAdvisor_UsesAdvisorSystemRole()
	{
		var context = new AiCvCompletionContext(
			AiCvTaskKind.SectionAdvisor,
			"C#, SQL, Azure",
			Section: CvImportSectionId.Skills,
			SectionContent: "C#, SQL, Azure");

		var messages = AiCvPromptBuilder.Build(AiCvTaskKind.SectionAdvisor, context, "en");

		Assert.Contains("CV reviewer", messages.SystemPrompt, StringComparison.Ordinal);
		Assert.Contains("Skills", messages.UserPrompt, StringComparison.Ordinal);
	}

	[Fact]
	public void Build_EmptyEducation_IncludesAdviceOnlyNoFabrication()
	{
		var context = new AiCvCompletionContext(
			AiCvTaskKind.AdviseEducationSection,
			string.Empty,
			Section: CvImportSectionId.Education,
			SectionIsEmpty: true);

		var messages = AiCvPromptBuilder.Build(AiCvTaskKind.AdviseEducationSection, context, "en");

		Assert.Contains("do NOT", messages.UserPrompt, StringComparison.Ordinal);
		Assert.Contains("guidance only", messages.UserPrompt, StringComparison.Ordinal);
	}

	[Fact]
	public void Build_TargetContext_BiasesSystemPrompt()
	{
		var context = new AiCvCompletionContext(
			AiCvTaskKind.SectionAdvisor,
			"C#",
			Section: CvImportSectionId.Skills,
			SectionContent: "C#",
			TargetContext: new AiCvTargetContext("Senior Platform Engineer", "We need Kubernetes and Go."));

		var messages = AiCvPromptBuilder.Build(AiCvTaskKind.SectionAdvisor, context, "en");

		Assert.Contains("Target role: Senior Platform Engineer", messages.SystemPrompt, StringComparison.Ordinal);
		Assert.Contains("do NOT copy skills", messages.SystemPrompt, StringComparison.Ordinal);
	}

	[Fact]
	public void Build_NoTargetContext_OmitsTargetInstruction()
	{
		var context = new AiCvCompletionContext(
			AiCvTaskKind.SectionAdvisor,
			"C#",
			Section: CvImportSectionId.Skills,
			SectionContent: "C#");

		var messages = AiCvPromptBuilder.Build(AiCvTaskKind.SectionAdvisor, context, "en");

		Assert.DoesNotContain("Target role", messages.SystemPrompt, StringComparison.Ordinal);
	}

	[Fact]
	public void Build_MeasurableResults_AsksToQuantifyWithoutInventing()
	{
		var context = new AiCvCompletionContext(
			AiCvTaskKind.SuggestMeasurableResults,
			"Led the migration to microservices.",
			JobTitle: "Engineer",
			Company: "Acme",
			Section: CvImportSectionId.WorkExperience);

		var messages = AiCvPromptBuilder.Build(AiCvTaskKind.SuggestMeasurableResults, context, "en");

		Assert.Contains("quantify", messages.UserPrompt, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("do NOT invent", messages.UserPrompt, StringComparison.Ordinal);
	}

	[Fact]
	public void Build_DraftSkillsFromContext_UsesRolesNotFabrication()
	{
		var context = new AiCvCompletionContext(
			AiCvTaskKind.DraftSkillsFromContext,
			"Backend Engineer — Acme",
			Section: CvImportSectionId.Skills,
			SectionContent: "Backend Engineer — Acme",
			SectionIsEmpty: true);

		var messages = AiCvPromptBuilder.Build(AiCvTaskKind.DraftSkillsFromContext, context, "en");

		Assert.Contains("Backend Engineer", messages.UserPrompt, StringComparison.Ordinal);
		Assert.Contains("Do not assert skills", messages.UserPrompt, StringComparison.Ordinal);
	}

	[Fact]
	public void Build_AdviceTask_Slovak_AddsSlovakInstruction()
	{
		var context = new AiCvCompletionContext(
			AiCvTaskKind.SectionAdvisor,
			"C#",
			Section: CvImportSectionId.Skills,
			SectionContent: "C#");

		var messages = AiCvPromptBuilder.Build(AiCvTaskKind.SectionAdvisor, context, "sk");

		Assert.Contains("Slovak", messages.SystemPrompt, StringComparison.Ordinal);
	}

	[Fact]
	public void Build_ShortenSummary_RequestsCondense()
	{
		var context = new AiCvCompletionContext(
			AiCvTaskKind.ShortenProfessionalSummary,
			"A very long professional summary that goes on and on about many things.",
			ProfessionalTitle: "Engineer");

		var messages = AiCvPromptBuilder.Build(AiCvTaskKind.ShortenProfessionalSummary, context, "en");

		Assert.Contains("Shorten", messages.UserPrompt, StringComparison.Ordinal);
		Assert.Contains("Do not invent", messages.SystemPrompt, StringComparison.Ordinal);
	}
}
