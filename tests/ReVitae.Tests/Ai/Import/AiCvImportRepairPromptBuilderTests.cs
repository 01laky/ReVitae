using ReVitae.Core.Ai.Import;
using ReVitae.Core.Import;

namespace ReVitae.Tests.Ai.Import;

public sealed class AiCvImportRepairPromptBuilderTests
{
	private static AiImportFieldRepairTarget Target(string key, string value) =>
		new(CvImportSectionId.PersonalInformation, key, null, value, CvImportConfidence.Low);

	[Fact]
	public void Build_IncludesSectionAndNumberedFields()
	{
		var targets = new[] { Target("firstName", "Jon"), Target("email", "j@x.co") };
		var messages = AiCvImportRepairPromptBuilder.Build(
			CvImportSectionId.PersonalInformation, targets, "source text", "en");

		Assert.Contains("Section: PersonalInformation", messages.UserPrompt, StringComparison.Ordinal);
		Assert.Contains("1: Jon", messages.UserPrompt, StringComparison.Ordinal);
		Assert.Contains("2: j@x.co", messages.UserPrompt, StringComparison.Ordinal);
	}

	[Fact]
	public void Build_TruncatesSourceWindow()
	{
		var big = new string('x', AiImportLimits.RepairSourceWindowChars + 500);
		var messages = AiCvImportRepairPromptBuilder.Build(
			CvImportSectionId.PersonalInformation, [Target("firstName", "Jon")], big, "en");

		// The source window must be truncated to the configured limit.
		Assert.DoesNotContain(new string('x', AiImportLimits.RepairSourceWindowChars + 1), messages.UserPrompt, StringComparison.Ordinal);
	}

	[Fact]
	public void Build_SlovakCulture_AddsSlovakInstruction()
	{
		var messages = AiCvImportRepairPromptBuilder.Build(
			CvImportSectionId.PersonalInformation, [Target("firstName", "Jon")], "src", "sk");
		Assert.Contains("Slovak", messages.SystemPrompt, StringComparison.Ordinal);
	}

	[Fact]
	public void Build_SystemRole_ForbidsInventingData()
	{
		var messages = AiCvImportRepairPromptBuilder.Build(
			CvImportSectionId.PersonalInformation, [Target("firstName", "Jon")], "src", "en");
		Assert.Contains("Do not add new fields", messages.SystemPrompt, StringComparison.Ordinal);
	}
}
