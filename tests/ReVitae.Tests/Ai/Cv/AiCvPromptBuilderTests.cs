using ReVitae.Core.Ai.Cv;

namespace ReVitae.Tests.Ai.Cv;

public sealed class AiCvPromptBuilderTests
{
    [Fact]
    public void Build_WorkTask_IncludesJobTitleAndCompany()
    {
        var context = new AiCvCompletionContext(
            AiCvTaskKind.ImproveWorkDescription,
            "Did things.",
            JobTitle: "Engineer",
            Company: "Acme");

        var messages = AiCvPromptBuilder.Build(context.Task, context, "en");

        Assert.Contains("Job title: Engineer", messages.UserPrompt, StringComparison.Ordinal);
        Assert.Contains("Company: Acme", messages.UserPrompt, StringComparison.Ordinal);
        Assert.Contains("Did things.", messages.UserPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_SlovakCulture_AddsSlovakInstruction()
    {
        var context = new AiCvCompletionContext(
            AiCvTaskKind.ImproveProfessionalSummary,
            "Short text",
            ProfessionalTitle: "Developer");

        var messages = AiCvPromptBuilder.Build(context.Task, context, "sk-SK");

        Assert.Contains("Slovak", messages.SystemPrompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_DraftWorkDescriptionEmpty_DoesNotInventCompanyGuidance()
    {
        var context = new AiCvCompletionContext(
            AiCvTaskKind.DraftWorkDescription,
            string.Empty,
            JobTitle: "Analyst");

        var messages = AiCvPromptBuilder.Build(context.Task, context, "en");

        Assert.Contains("do not invent a company name", messages.UserPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Company:", messages.UserPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolveLanguageInstruction_English_ReturnsEnglishInstruction()
    {
        Assert.Contains("English", AiCvPromptBuilder.ResolveLanguageInstruction("en-US"), StringComparison.Ordinal);
    }
}
