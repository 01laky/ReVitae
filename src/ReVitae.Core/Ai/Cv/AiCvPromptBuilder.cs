using System.Globalization;

namespace ReVitae.Core.Ai.Cv;

public static class AiCvPromptBuilder
{
	public static AiCvPromptMessages Build(
		AiCvTaskKind task,
		AiCvCompletionContext context,
		string uiCulture)
	{
		var languageInstruction = ResolveLanguageInstruction(uiCulture);
		var systemPrompt = string.Join(
			" ",
			AiCvPromptTemplates.SystemRole,
			languageInstruction,
			AiCvPromptTemplates.DoNotInvent);

		var userPrompt = task switch
		{
			AiCvTaskKind.ImproveWorkDescription => BuildImproveWorkDescription(context),
			AiCvTaskKind.DraftWorkDescription => BuildDraftWorkDescription(context),
			AiCvTaskKind.ImproveProfessionalSummary => BuildImproveProfessionalSummary(context),
			AiCvTaskKind.DraftProfessionalSummary => BuildDraftProfessionalSummary(context),
			AiCvTaskKind.ImproveProjectDescription => BuildImproveProjectDescription(context),
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

	private static string BuildImproveWorkDescription(AiCvCompletionContext context)
	{
		var metadata = FormatWorkMetadata(context);
		return $"""
            Field: {AiCvPromptTemplates.WorkDescriptionFieldLabel}
            {metadata}
            Current text:
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
            {metadata}
            Current text:
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
