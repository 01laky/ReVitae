namespace ReVitae.Core.Ai.Cv;

public static class AiCvPromptTemplates
{
	public const string SystemRole =
		"You are a CV writing assistant. Respond with plain text only — no markdown fences, " +
		"no labels like \"Suggested text:\", and no JSON.";

	public const string DoNotInvent =
		"Do not invent employers, dates, metrics, or project details not implied by the input.";

	// 045 A.3 / C.5 — system role for advice-list tasks (advisor, grouping, measurable, etc.).
	public const string AdvisorSystemRole =
		"You are a CV reviewer. Give short, concrete, actionable suggestions about the section. " +
		"Return between 1 and 4 suggestions, one per line, each starting with \"- \". " +
		"After each suggestion you may add a brief reason on the same line using \" — \" " +
		"(an em dash) followed by the reason. Do not invent facts, employers, degrees, dates, " +
		"or proficiency levels the user did not provide. Plain text only — no markdown headings, " +
		"no numbering, no JSON.";

	// 045 A.1 — empty-section advice must never fabricate entries.
	public const string AdviceOnlyNoFabrication =
		"This section is empty. Suggest what the user should add and how to phrase it, but do " +
		"NOT write fabricated degrees, institutions, dates, language levels, or certificates as " +
		"if they were real. Give guidance only.";

	// 045 C.1 — optional target-role bias; appended only when provided.
	public const string TargetContextPreamble =
		"Tailor the suggestions toward this target role. Use the job description only to decide " +
		"emphasis and wording — do NOT copy skills, titles, or employers from it into the CV as " +
		"if they were the user's own.";

	public const string WorkDescriptionFieldLabel = "Work experience description";
	public const string ProfessionalSummaryFieldLabel = "Professional summary";
	public const string ProjectDescriptionFieldLabel = "Project description";

	public const string MaxWordsWorkDescription = "120";
	public const string MaxWordsProfessionalSummary = "80";
	public const string MaxWordsProjectDescription = "120";
	public const string MaxWordsShortSummary = "45";

	// 045 — section advisor labels.
	public const string SummarySectionLabel = "Professional summary";
	public const string WorkSectionLabel = "Work experience";
	public const string SkillsSectionLabel = "Skills";
	public const string EducationSectionLabel = "Education";
	public const string LanguagesSectionLabel = "Languages";
	public const string ProjectsSectionLabel = "Projects";
}
