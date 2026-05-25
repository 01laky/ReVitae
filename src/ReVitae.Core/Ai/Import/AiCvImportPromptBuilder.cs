using ReVitae.Core.Ai.Cv;

namespace ReVitae.Core.Ai.Import;

public static class AiCvImportPromptBuilder
{
    public static AiCvPromptMessages Build(
        AiImportPhase phase,
        string sliceText,
        string carryForwardSummary,
        string uiCulture)
    {
        var system = BuildSystemPrompt(uiCulture);
        var user = $"""
            Extract ONLY the fields in this JSON schema from the CV excerpt below.
            If a field is missing, use "" or [].
            Do not invent employers, dates, or credentials.

            Schema: {GetSchema(phase)}

            Previously extracted (summary): {carryForwardSummary}

            CV excerpt:
            {sliceText}
            """;

        return new AiCvPromptMessages(system, user);
    }

    public static string BuildSystemPrompt(string uiCulture) =>
        $"""
        Extract factual CV content exactly as written in the source document.
        Preserve original spelling of names, employers, and places.
        Do not translate content into English unless the source excerpt is already English.
        UI language ({uiCulture}) affects these instructions only — not field values.
        Respond with JSON only, no markdown fences.
        """;

    private static string GetSchema(AiImportPhase phase) =>
        phase switch
        {
            AiImportPhase.Personal => """{"personalInformation":{"firstName":"","lastName":"","professionalTitle":"","email":"","phone":"","location":"","linkedInUrl":"","portfolioUrl":"","gitHubUrl":"","shortSummary":""}}""",
            AiImportPhase.Work => """{"workExperience":[{"company":"","jobTitle":"","location":"","startMonth":0,"startYear":0,"endMonth":0,"endYear":0,"isCurrentlyWorking":false,"description":""}]}""",
            AiImportPhase.Education => """{"education":[{"institution":"","degree":"","fieldOfStudy":"","startYear":0,"endYear":0}]}""",
            AiImportPhase.Skills => """{"skills":[{"category":"","skills":[{"name":""}]}]}""",
            AiImportPhase.Languages => """{"languages":[{"language":"","level":""}]}""",
            AiImportPhase.SkillsAndLanguages => """{"skills":[{"category":"","skills":[{"name":""}]}],"languages":[{"language":"","level":""}]}""",
            AiImportPhase.Certificates => """{"certificates":[{"name":"","issuer":"","year":0}]}""",
            AiImportPhase.Projects => """{"projects":[{"name":"","description":"","technologies":[]}]}""",
            AiImportPhase.Links => """{"links":[{"label":"","url":""}]}""",
            AiImportPhase.Additional => """{"additionalInformation":{"content":""}}""",
            _ => "{}",
        };
}
