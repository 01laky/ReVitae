using ReVitae.Core.Quality;

namespace ReVitae.Tests.Quality;

public sealed class CvQualityTextHelperTests
{
	// ── CountNonWhitespace ──────────────────────────────────────────────────

	[Fact]
	public void CountNonWhitespace_Null_ReturnsZero()
	{
		Assert.Equal(0, CvQualityTextHelper.CountNonWhitespace(null));
	}

	[Fact]
	public void CountNonWhitespace_Empty_ReturnsZero()
	{
		Assert.Equal(0, CvQualityTextHelper.CountNonWhitespace(string.Empty));
	}

	[Fact]
	public void CountNonWhitespace_SpaceOnly_ReturnsZero()
	{
		Assert.Equal(0, CvQualityTextHelper.CountNonWhitespace("   "));
	}

	[Fact]
	public void CountNonWhitespace_TabsAndNewlines_ReturnsZero()
	{
		Assert.Equal(0, CvQualityTextHelper.CountNonWhitespace("\t\n\r "));
	}

	[Fact]
	public void CountNonWhitespace_SingleChar_ReturnsOne()
	{
		Assert.Equal(1, CvQualityTextHelper.CountNonWhitespace("a"));
	}

	[Theory]
	[InlineData("hello", 5)]
	[InlineData("  hello  ", 5)]
	[InlineData("h e l l o", 5)]
	[InlineData("a1!", 3)]
	[InlineData("café", 4)]
	public void CountNonWhitespace_KnownInputs_ReturnsExpected(string input, int expected)
	{
		Assert.Equal(expected, CvQualityTextHelper.CountNonWhitespace(input));
	}

	[Fact]
	public void CountNonWhitespace_OnlyNonBreakingSpace_ReturnsZero()
	{
		// U+00A0 is a non-breaking space — char.IsWhiteSpace returns true for it.
		Assert.Equal(0, CvQualityTextHelper.CountNonWhitespace("  "));
	}

	// ── HasText ─────────────────────────────────────────────────────────────

	[Fact]
	public void HasText_Null_ReturnsFalse()
	{
		Assert.False(CvQualityTextHelper.HasText(null));
	}

	[Fact]
	public void HasText_Empty_ReturnsFalse()
	{
		Assert.False(CvQualityTextHelper.HasText(string.Empty));
	}

	[Fact]
	public void HasText_WhitespaceOnly_ReturnsFalse()
	{
		Assert.False(CvQualityTextHelper.HasText("   \t\n"));
	}

	[Fact]
	public void HasText_SingleNonWhitespaceChar_ReturnsTrue()
	{
		Assert.True(CvQualityTextHelper.HasText("a"));
	}

	[Fact]
	public void HasText_WordSurroundedBySpaces_ReturnsTrue()
	{
		Assert.True(CvQualityTextHelper.HasText("   word   "));
	}

	[Fact]
	public void HasText_UnicodeChar_ReturnsTrue()
	{
		Assert.True(CvQualityTextHelper.HasText("ž"));
	}
}
