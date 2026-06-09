using ReVitae.Core.Import;

namespace ReVitae.Core.Ai.Cv;

public static class AiCvPromptBuilder
{
	public static AiCvPromptMessages Build(
		AiCvTaskKind task,
		AiCvCompletionContext context,
		string uiCulture)
	{
		var adviceList = AiCvTaskRegistry.ProducesAdviceList(task);
		var languageInstruction = ResolveLanguageInstruction(uiCulture);

		var parts = new List<string>
		{
			adviceList ? AiCvPromptTemplates.AdvisorSystemRole : AiCvPromptTemplates.SystemRole,
			languageInstruction,
		};

		if (!adviceList)
		{
			parts.Add(AiCvPromptTemplates.DoNotInvent);
		}

		if (context.TargetContext is { HasValue: true } target)
		{
			parts.Add(BuildTargetContextInstruction(target));
		}

		var systemPrompt = string.Join(" ", parts);

		var userPrompt = task switch
		{
			AiCvTaskKind.ImproveWorkDescription => BuildImproveWorkDescription(context),
			AiCvTaskKind.DraftWorkDescription => BuildDraftWorkDescription(context),
			AiCvTaskKind.ImproveProfessionalSummary => BuildImproveProfessionalSummary(context),
			AiCvTaskKind.DraftProfessionalSummary => BuildDraftProfessionalSummary(context),
			AiCvTaskKind.ImproveProjectDescription => BuildImproveProjectDescription(context),
			AiCvTaskKind.ShortenProfessionalSummary => BuildShortenProfessionalSummary(context),
			AiCvTaskKind.SuggestMeasurableResults => BuildSuggestMeasurableResults(context),
			AiCvTaskKind.SuggestSkillGrouping => BuildSuggestSkillGrouping(context),
			AiCvTaskKind.DraftSkillsFromContext => BuildDraftSkillsFromContext(context),
			AiCvTaskKind.AdviseEducationSection => BuildAdviseEmptySection(context, AiCvPromptTemplates.EducationSectionLabel),
			AiCvTaskKind.AdviseLanguagesSection => BuildAdviseEmptySection(context, AiCvPromptTemplates.LanguagesSectionLabel),
			AiCvTaskKind.SectionAdvisor => BuildSectionAdvisor(context),
			_ => throw new ArgumentOutOfRangeException(nameof(task), task, null),
		};

		return new AiCvPromptMessages(systemPrompt, userPrompt);
	}

	internal static string ResolveLanguageInstruction(string uiCulture)
	{
		if (uiCulture.StartsWith("sk", StringComparison.OrdinalIgnoreCase))
		{
			return "Write the response in Slovak.";
		}

		return "Write the response in English.";
	}

	internal static string BuildTargetContextInstruction(AiCvTargetContext target)
	{
		var lines = new List<string> { AiCvPromptTemplates.TargetContextPreamble };
		if (!string.IsNullOrWhiteSpace(target.Role))
		{
			lines.Add($"Target role: {target.Role!.Trim()}.");
		}

		if (!string.IsNullOrWhiteSpace(target.JobDescriptionExcerpt))
		{
			lines.Add($"Job description: {target.JobDescriptionExcerpt!.Trim()}");
		}

		return string.Join(" ", lines);
	}

	internal static string SectionLabel(CvImportSectionId? section) =>
		section switch
		{
			CvImportSectionId.Summary => AiCvPromptTemplates.SummarySectionLabel,
			CvImportSectionId.WorkExperience => AiCvPromptTemplates.WorkSectionLabel,
			CvImportSectionId.Skills => AiCvPromptTemplates.SkillsSectionLabel,
			CvImportSectionId.Education => AiCvPromptTemplates.EducationSectionLabel,
			CvImportSectionId.Languages => AiCvPromptTemplates.LanguagesSectionLabel,
			CvImportSectionId.Projects => AiCvPromptTemplates.ProjectsSectionLabel,
			_ => "CV section",
		};

	private static string BuildSectionAdvisor(AiCvCompletionContext context)
	{
		var label = SectionLabel(context.Section);
		if (context.SectionIsEmpty)
		{
			return $"""
            Section: {label}
            {AiCvPromptTemplates.AdviceOnlyNoFabrication}
            """;
		}

		return $"""
        Section: {label}
        Current content:
        {context.SectionContent}

        Suggest concrete improvements to this section.
        """;
	}

	private static string BuildAdviseEmptySection(AiCvCompletionContext context, string label) =>
		$"""
        Section: {label}
        {AiCvPromptTemplates.AdviceOnlyNoFabrication}
        """;

	private static string BuildSuggestSkillGrouping(AiCvCompletionContext context) =>
		$"""
        Section: {AiCvPromptTemplates.SkillsSectionLabel}
        Current skills:
        {context.SectionContent}

        Suggest how to group these skills into clear categories and which to emphasise. Do not invent skills the user did not list.
        """;

	private static string BuildDraftSkillsFromContext(AiCvCompletionContext context) =>
		$"""
        Section: {AiCvPromptTemplates.SkillsSectionLabel}
        The skills section is empty. Based only on the roles below, suggest skill categories and example skills the user likely has. Do not assert skills that the roles do not imply.
        Roles:
        {context.SectionContent}
        """;

	private static string BuildSuggestMeasurableResults(AiCvCompletionContext context)
	{
		var metadata = FormatWorkMetadata(context);
		return $"""
        Field: {AiCvPromptTemplates.WorkDescriptionFieldLabel}
        {metadata}Current text:
        {context.CurrentText}

        Suggest where and what the user could quantify (team size, %, time saved, revenue, scope). Ask what to measure — do NOT invent specific numbers or metrics.
        """;
	}

	private static string BuildShortenProfessionalSummary(AiCvCompletionContext context)
	{
		var titleLine = string.IsNullOrWhiteSpace(context.ProfessionalTitle)
			? string.Empty
			: $"Professional title: {context.ProfessionalTitle.Trim()}{Environment.NewLine}";

		return $"""
        Field: {AiCvPromptTemplates.ProfessionalSummaryFieldLabel}
        {titleLine}Current text:
        {context.CurrentText}

        Shorten this professional summary to at most {AiCvPromptTemplates.MaxWordsShortSummary} words while keeping the key facts. Do not add new claims.
        """;
	}

	private static string BuildImproveWorkDescription(AiCvCompletionContext context)
	{
		var metadata = FormatWorkMetadata(context);
		return $"""
        Field: {AiCvPromptTemplates.WorkDescriptionFieldLabel}
        {metadata}Current text:
        {context.CurrentText}

        Improve this work experience description. Keep it concise (at most {AiCvPromptTemplates.MaxWordsWorkDescription} words), specific, and professional.
        """;
	}

	private static string BuildDraftWorkDescription(AiCvCompletionContext context)
	{
		var metadata = FormatWorkMetadata(context);
		var current = string.IsNullOrWhiteSpace(context.CurrentText)
			? "(empty — draft from role context only; do not invent a company name if none is provided)"
			: context.CurrentText;

		return $"""
        Field: {AiCvPromptTemplates.WorkDescriptionFieldLabel}
        {metadata}Current text:
        {current}

        Draft a work experience description based on the available role context. Keep it concise (at most {AiCvPromptTemplates.MaxWordsWorkDescription} words).
        """;
	}

	private static string BuildImproveProfessionalSummary(AiCvCompletionContext context)
	{
		var titleLine = string.IsNullOrWhiteSpace(context.ProfessionalTitle)
			? string.Empty
			: $"Professional title: {context.ProfessionalTitle.Trim()}{Environment.NewLine}";

		return $"""
        Field: {AiCvPromptTemplates.ProfessionalSummaryFieldLabel}
        {titleLine}Current text:
        {context.CurrentText}

        Improve this professional summary. Keep it concise (at most {AiCvPromptTemplates.MaxWordsProfessionalSummary} words) and suitable for a CV.
        """;
	}

	private static string BuildDraftProfessionalSummary(AiCvCompletionContext context)
	{
		var titleLine = string.IsNullOrWhiteSpace(context.ProfessionalTitle)
			? string.Empty
			: $"Professional title: {context.ProfessionalTitle.Trim()}{Environment.NewLine}";

		var current = string.IsNullOrWhiteSpace(context.CurrentText)
			? "(empty — draft a summary from the professional title if provided)"
			: context.CurrentText;

		return $"""
        Field: {AiCvPromptTemplates.ProfessionalSummaryFieldLabel}
        {titleLine}Current text:
        {current}

        Draft a professional summary. Keep it concise (at most {AiCvPromptTemplates.MaxWordsProfessionalSummary} words).
        """;
	}

	private static string BuildImproveProjectDescription(AiCvCompletionContext context)
	{
		var nameLine = string.IsNullOrWhiteSpace(context.ProjectName)
			? string.Empty
			: $"Project name: {context.ProjectName.Trim()}{Environment.NewLine}";

		var current = string.IsNullOrWhiteSpace(context.CurrentText)
			? "(empty — improve or draft from project name if provided)"
			: context.CurrentText;

		return $"""
        Field: {AiCvPromptTemplates.ProjectDescriptionFieldLabel}
        {nameLine}Current text:
        {current}

        Improve this project description. Keep it concise (at most {AiCvPromptTemplates.MaxWordsProjectDescription} words).
        """;
	}

	private static string FormatWorkMetadata(AiCvCompletionContext context)
	{
		var lines = new List<string>();
		if (!string.IsNullOrWhiteSpace(context.JobTitle))
		{
			lines.Add($"Job title: {context.JobTitle.Trim()}");
		}

		if (!string.IsNullOrWhiteSpace(context.Company))
		{
			lines.Add($"Company: {context.Company.Trim()}");
		}

		return lines.Count == 0
			? string.Empty
			: string.Join(Environment.NewLine, lines) + Environment.NewLine;
	}
}
