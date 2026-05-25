using ReVitae.Core.Ai.Import;

namespace ReVitae.Tests.Ai.Import;

public sealed class AiCvImportPromptBuilderTests
{
    [Fact]
    public void Build_PersonalPhase_ContainsPersonalSchemaOnly()
    {
        var messages = AiCvImportPromptBuilder.Build(
            AiImportPhase.Personal,
            "John Doe",
            "none",
            "en");
        Assert.Contains("personalInformation", messages.UserPrompt, StringComparison.Ordinal);
        Assert.DoesNotContain("workExperience", messages.UserPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_SystemPrompt_LengthWithinReasonableCap()
    {
        var system = AiCvImportPromptBuilder.BuildSystemPrompt("en");
        Assert.True(system.Length < 600);
        Assert.Contains("Preserve original spelling", system, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("sk")]
    [InlineData("en")]
    public void Build_UiCulture_IsIncludedInSystemPrompt(string culture)
    {
        var messages = AiCvImportPromptBuilder.Build(
            AiImportPhase.Personal,
            "Ján Horváth",
            "none",
            culture);
        Assert.Contains(culture, messages.SystemPrompt, StringComparison.Ordinal);
        Assert.Contains("Do not translate", messages.SystemPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_CarryForward_TruncatedInUserPrompt()
    {
        var carry = new string('x', 500);
        var messages = AiCvImportPromptBuilder.Build(
            AiImportPhase.Work,
            "Work excerpt",
            carry,
            "en");
        Assert.Contains(carry, messages.UserPrompt, StringComparison.Ordinal);
    }
}

public sealed class AiCvImportLocalePromptTests
{
    [Fact]
    public void Build_SlovakCulture_PreservesLanguageInstruction()
    {
        var messages = AiCvImportPromptBuilder.Build(
            AiImportPhase.Personal,
            SampleCvText.SlovakNameSample(),
            "none",
            "sk");
        Assert.Contains("sk", messages.SystemPrompt, StringComparison.Ordinal);
        Assert.Contains("Ján Horváth", messages.UserPrompt, StringComparison.Ordinal);
    }
}
