using ReVitae.Core.Ai.Cv;

namespace ReVitae.Tests.Ai.Cv;

public sealed class AiCvEntityGuardEdgeCaseTests
{
	[Fact]
	public void Inspect_NullSource_TreatedAsEmpty_FlagsNumber()
	{
		var result = AiCvEntityGuard.Inspect(null!, "Delivered in 2023.");
		Assert.True(result.HasUnsupportedEntities);
		Assert.Contains("2023", result.UnsupportedEntities);
	}

	[Fact]
	public void Inspect_NullOutput_IsClean()
	{
		var result = AiCvEntityGuard.Inspect("Some source text", null!);
		Assert.False(result.HasUnsupportedEntities);
	}

	[Fact]
	public void Inspect_WhitespaceOutput_IsClean()
	{
		Assert.False(AiCvEntityGuard.Inspect("source", "   \n\t ").HasUnsupportedEntities);
	}

	[Fact]
	public void Inspect_DecimalPercentage_NotInSource_Flagged()
	{
		var result = AiCvEntityGuard.Inspect(
			"Improved reliability significantly.",
			"Improved reliability by 12.5%.");
		Assert.True(result.HasUnsupportedEntities);
	}

	[Fact]
	public void Inspect_NumberWithComma_DigitsCompared()
	{
		var result = AiCvEntityGuard.Inspect(
			"Joined the company.",
			"Joined the company in 2,021.");
		Assert.True(result.HasUnsupportedEntities);
	}

	[Fact]
	public void Inspect_ManyUnsupportedNumbers_CapsAtSix()
	{
		var output = "Values 11 22 33 44 55 66 77 88 99 added.";
		var result = AiCvEntityGuard.Inspect("No numbers here at all.", output);
		Assert.True(result.HasUnsupportedEntities);
		Assert.True(result.UnsupportedEntities.Count <= 6);
	}

	[Fact]
	public void Inspect_EmployerNameCaseInsensitive_NotFlaggedWhenInSource()
	{
		var result = AiCvEntityGuard.Inspect(
			"Worked at acme corporation on payments.",
			"Worked at Acme Corporation on the payments platform.");
		Assert.False(result.HasUnsupportedEntities);
	}

	[Fact]
	public void Inspect_CurrencyAmount_NotInSource_Flagged()
	{
		var result = AiCvEntityGuard.Inspect(
			"Managed the budget.",
			"Managed a budget of $5000.");
		Assert.True(result.HasUnsupportedEntities);
		Assert.Contains(result.UnsupportedEntities, e => e.Contains("5000", StringComparison.Ordinal));
	}

	[Fact]
	public void Inspect_NumberAsSubstringInSource_NotFlagged()
	{
		var result = AiCvEntityGuard.Inspect(
			"Released build 2021abc to production.",
			"Released the 2021 build.");
		Assert.False(result.HasUnsupportedEntities);
	}

	[Fact]
	public void Inspect_RepeatedUnsupportedToken_DedupedOnce()
	{
		var result = AiCvEntityGuard.Inspect(
			"No metrics provided.",
			"Grew by 40% then by 40% again.");
		Assert.True(result.HasUnsupportedEntities);
		Assert.Single(result.UnsupportedEntities, e => e.Contains("40", StringComparison.Ordinal));
	}

	[Fact]
	public void Inspect_BothNullOrEmpty_IsClean()
	{
		Assert.False(AiCvEntityGuard.Inspect(string.Empty, string.Empty).HasUnsupportedEntities);
		Assert.False(AiCvEntityGuard.Inspect(null!, null!).HasUnsupportedEntities);
	}

	[Fact]
	public void Inspect_CleanResultSingletonHasNoEntities()
	{
		Assert.Empty(AiCvEntityGuardResult.Clean.UnsupportedEntities);
		Assert.False(AiCvEntityGuardResult.Clean.HasUnsupportedEntities);
	}
}
