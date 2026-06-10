using ReVitae.Core.Quality;

namespace ReVitae.Tests.Quality;

public sealed class GenericWorkDescriptionHeuristicTests
{
	// ── Short/null/empty inputs ─────────────────────────────────────────────

	[Fact]
	public void IsGeneric_Null_ReturnsFalse()
	{
		Assert.False(GenericWorkDescriptionHeuristic.IsGeneric(null));
	}

	[Fact]
	public void IsGeneric_Empty_ReturnsFalse()
	{
		Assert.False(GenericWorkDescriptionHeuristic.IsGeneric(string.Empty));
	}

	[Theory]
	[InlineData(1)]
	[InlineData(20)]
	[InlineData(40)]
	public void IsGeneric_AtMost40NonWhitespace_ReturnsFalse(int charCount)
	{
		// The heuristic only fires when >40 non-whitespace chars, regardless of verb presence.
		var text = new string('x', charCount);
		Assert.False(GenericWorkDescriptionHeuristic.IsGeneric(text));
	}

	[Fact]
	public void IsGeneric_41CharsNoStrongVerbs_ReturnsTrue()
	{
		var text = new string('x', 41);
		Assert.True(GenericWorkDescriptionHeuristic.IsGeneric(text));
	}

	// ── Digit / percent exemptions ──────────────────────────────────────────

	[Fact]
	public void IsGeneric_ContainsDigit_ReturnsFalse()
	{
		var text = "Responsible for managing the department team operations daily throughout the year 2023.";
		Assert.False(GenericWorkDescriptionHeuristic.IsGeneric(text));
	}

	[Fact]
	public void IsGeneric_ContainsPercent_ReturnsFalse()
	{
		var text = "Responsible for various duties and ongoing team operations across the full department scope %.";
		Assert.False(GenericWorkDescriptionHeuristic.IsGeneric(text));
	}

	// ── Strong verbs ────────────────────────────────────────────────────────

	[Theory]
	[InlineData("increased")]
	[InlineData("reduced")]
	[InlineData("delivered")]
	[InlineData("led")]
	[InlineData("built")]
	[InlineData("improved")]
	[InlineData("achieved")]
	[InlineData("implemented")]
	[InlineData("designed")]
	[InlineData("optimized")]
	[InlineData("migrated")]
	[InlineData("automated")]
	[InlineData("scaled")]
	[InlineData("created")]
	[InlineData("launched")]
	[InlineData("managed")]
	public void IsGeneric_TextContainsStrongVerb_ReturnsFalse(string verb)
	{
		var text = $"Responsible for various tasks and daily operations across team in order to {verb} outcomes.";
		Assert.False(GenericWorkDescriptionHeuristic.IsGeneric(text));
	}

	[Fact]
	public void IsGeneric_StrongVerbUppercase_ReturnsFalse()
	{
		var text = "Responsible for various duties and ongoing operations MANAGED by the senior team leads.";
		Assert.False(GenericWorkDescriptionHeuristic.IsGeneric(text));
	}

	[Fact]
	public void IsGeneric_StrongVerbMixedCase_ReturnsFalse()
	{
		var text = "Responsible for various duties and ongoing team operations Implemented by senior engineers.";
		Assert.False(GenericWorkDescriptionHeuristic.IsGeneric(text));
	}

	[Fact]
	public void IsGeneric_StrongVerbEmbeddedInLargerWord_ReturnsTrue()
	{
		// "remanaged" contains "managed" but not as a whole word — should still be considered generic.
		var text = "Responsible for daily tasks and ongoing duties that were remanaged by the broader team continuously.";
		Assert.True(GenericWorkDescriptionHeuristic.IsGeneric(text));
	}

	// ── Canonical generic text ──────────────────────────────────────────────

	[Fact]
	public void IsGeneric_TypicalGenericBullet_ReturnsTrue()
	{
		var text = "Responsible for various duties and ongoing team operations across the department in a collaborative way.";
		Assert.True(GenericWorkDescriptionHeuristic.IsGeneric(text));
	}

	[Fact]
	public void IsGeneric_SpecificTextWithMetric_ReturnsFalse()
	{
		var text = "Redesigned the data pipeline reducing processing time from 4 hours to 20 minutes for daily ETL jobs.";
		Assert.False(GenericWorkDescriptionHeuristic.IsGeneric(text));
	}
}
