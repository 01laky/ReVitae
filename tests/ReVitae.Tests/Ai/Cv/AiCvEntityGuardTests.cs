using ReVitae.Core.Ai.Cv;

namespace ReVitae.Tests.Ai.Cv;

public sealed class AiCvEntityGuardTests
{
	[Fact]
	public void Inspect_OutputAddsUnsupportedYear_FlagsIt()
	{
		var result = AiCvEntityGuard.Inspect(
			"Led the backend team and shipped the billing service.",
			"Led the backend team and shipped the billing service in 2021.");

		Assert.True(result.HasUnsupportedEntities);
		Assert.Contains("2021", result.UnsupportedEntities);
	}

	[Fact]
	public void Inspect_OutputAddsUnsupportedPercentage_FlagsIt()
	{
		var result = AiCvEntityGuard.Inspect(
			"Improved checkout reliability and reduced errors.",
			"Improved checkout reliability and reduced errors by 40%.");

		Assert.True(result.HasUnsupportedEntities);
		Assert.Contains(result.UnsupportedEntities, e => e.Contains("40", StringComparison.Ordinal));
	}

	[Fact]
	public void Inspect_OutputAddsNewEmployerName_FlagsIt()
	{
		var result = AiCvEntityGuard.Inspect(
			"Senior engineer responsible for the platform.",
			"Senior engineer at Globex Industries responsible for the platform.");

		Assert.True(result.HasUnsupportedEntities);
		Assert.Contains("Globex Industries", result.UnsupportedEntities);
	}

	[Fact]
	public void Inspect_NumbersAlreadyInSource_NotFlagged()
	{
		var result = AiCvEntityGuard.Inspect(
			"Managed a team of 12 across 3 offices in 2019.",
			"Managed a team of 12 people across 3 offices since 2019.");

		Assert.False(result.HasUnsupportedEntities);
		Assert.Empty(result.UnsupportedEntities);
	}

	[Fact]
	public void Inspect_CleanRewrite_NoUnsupportedEntities()
	{
		var result = AiCvEntityGuard.Inspect(
			"Built and maintained internal tools for the data team.",
			"Built and maintained internal tooling that supported the data team.");

		Assert.False(result.HasUnsupportedEntities);
	}

	[Fact]
	public void Inspect_SingleDigits_NotFlagged()
	{
		var result = AiCvEntityGuard.Inspect(
			"Worked on the project.",
			"Worked on the project for a year.");

		Assert.False(result.HasUnsupportedEntities);
	}

	[Fact]
	public void Inspect_EmptyOutput_IsClean()
	{
		var result = AiCvEntityGuard.Inspect("anything", string.Empty);
		Assert.False(result.HasUnsupportedEntities);
	}

	[Fact]
	public void Inspect_AddedEmail_FlagsIt()
	{
		var result = AiCvEntityGuard.Inspect(
			"Contact me through the portal.",
			"Contact me at john.doe@example.com or through the portal.");

		Assert.True(result.HasUnsupportedEntities);
		Assert.Contains("john.doe@example.com", result.UnsupportedEntities);
	}
}
