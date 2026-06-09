using ReVitae.Core.Ai.Cv;
using ReVitae.Core.Import;

namespace ReVitae.Tests.Ai.Cv;

public sealed class AiCvTargetContextTests
{
	[Fact]
	public void Empty_HasNoValue()
	{
		Assert.False(AiCvTargetContext.Empty.HasValue);
	}

	[Theory]
	[InlineData("Engineer", null, true)]
	[InlineData(null, "We need Go", true)]
	[InlineData("Engineer", "We need Go", true)]
	[InlineData(null, null, false)]
	[InlineData("  ", "  ", false)]
	[InlineData("", "", false)]
	public void HasValue_ReflectsNonEmptyFields(string? role, string? jd, bool expected)
	{
		Assert.Equal(expected, new AiCvTargetContext(role, jd).HasValue);
	}

	[Fact]
	public void AdvisorResult_Fail_CarriesSectionAndKey()
	{
		var result = AiCvAdvisorResult.Fail("err.key", CvImportSectionId.Skills);
		Assert.False(result.Succeeded);
		Assert.Equal("err.key", result.ErrorMessageKey);
		Assert.Equal(CvImportSectionId.Skills, result.Section);
		Assert.Empty(result.Suggestions);
	}

	[Fact]
	public void AdvisorResult_Cancelled_HasCancelledFlag()
	{
		var result = AiCvAdvisorResult.CancelledResult(CvImportSectionId.Education);
		Assert.True(result.Cancelled);
		Assert.False(result.Succeeded);
		Assert.Equal(CvImportSectionId.Education, result.Section);
	}

	[Fact]
	public void AdvisorSuggestion_DefaultsAreNull()
	{
		var s = new AiCvAdvisorSuggestion("text");
		Assert.Null(s.ApplyTarget);
		Assert.Null(s.ApplyValue);
		Assert.Null(s.Rationale);
	}
}
