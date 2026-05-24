namespace ReVitae.Core.Ai.Cv;

public static class AiCvPromptTemplates
{
    public const string SystemRole =
        "You are a CV writing assistant. Respond with plain text only — no markdown fences, " +
        "no labels like \"Suggested text:\", and no JSON.";

    public const string DoNotInvent =
        "Do not invent employers, dates, metrics, or project details not implied by the input.";

    public const string WorkDescriptionFieldLabel = "Work experience description";
    public const string ProfessionalSummaryFieldLabel = "Professional summary";
    public const string ProjectDescriptionFieldLabel = "Project description";

    public const string MaxWordsWorkDescription = "120";
    public const string MaxWordsProfessionalSummary = "80";
    public const string MaxWordsProjectDescription = "120";
}
