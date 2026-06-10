using ReVitae.Core.Quality;

namespace ReVitae.Tests.Quality;

public sealed class CvUrlNormalizerTests
{
	// ── NormalizeForComparison ──────────────────────────────────────────────

	[Fact]
	public void NormalizeForComparison_Null_ReturnsEmpty()
	{
		Assert.Equal(string.Empty, CvUrlNormalizer.NormalizeForComparison(null));
	}

	[Fact]
	public void NormalizeForComparison_Empty_ReturnsEmpty()
	{
		Assert.Equal(string.Empty, CvUrlNormalizer.NormalizeForComparison(string.Empty));
	}

	[Fact]
	public void NormalizeForComparison_WhitespaceOnly_ReturnsEmpty()
	{
		Assert.Equal(string.Empty, CvUrlNormalizer.NormalizeForComparison("   "));
	}

	[Fact]
	public void NormalizeForComparison_WwwPrefix_IsStripped()
	{
		var withWww = CvUrlNormalizer.NormalizeForComparison("https://www.linkedin.com/in/jane");
		var withoutWww = CvUrlNormalizer.NormalizeForComparison("https://linkedin.com/in/jane");
		Assert.Equal(withWww, withoutWww);
	}

	[Fact]
	public void NormalizeForComparison_TrailingSlash_IsStripped()
	{
		var withSlash = CvUrlNormalizer.NormalizeForComparison("https://github.com/jane/");
		var withoutSlash = CvUrlNormalizer.NormalizeForComparison("https://github.com/jane");
		Assert.Equal(withSlash, withoutSlash);
	}

	[Fact]
	public void NormalizeForComparison_UppercaseHost_Lowercased()
	{
		var upper = CvUrlNormalizer.NormalizeForComparison("https://GITHUB.COM/jane");
		var lower = CvUrlNormalizer.NormalizeForComparison("https://github.com/jane");
		Assert.Equal(upper, lower);
	}

	[Fact]
	public void NormalizeForComparison_NoScheme_HttpsPrepended()
	{
		var withScheme = CvUrlNormalizer.NormalizeForComparison("https://linkedin.com/in/jane");
		var withoutScheme = CvUrlNormalizer.NormalizeForComparison("linkedin.com/in/jane");
		Assert.Equal(withScheme, withoutScheme);
	}

	[Fact]
	public void NormalizeForComparison_HttpScheme_ReturnedAsIs()
	{
		var result = CvUrlNormalizer.NormalizeForComparison("http://example.com/path");
		Assert.StartsWith("http://", result, StringComparison.Ordinal);
	}

	[Fact]
	public void NormalizeForComparison_QueryString_Preserved()
	{
		var normalized = CvUrlNormalizer.NormalizeForComparison("https://example.com/search?q=test");
		Assert.Contains("?q=test", normalized, StringComparison.Ordinal);
	}

	[Fact]
	public void NormalizeForComparison_UppercaseScheme_Lowercased()
	{
		var upper = CvUrlNormalizer.NormalizeForComparison("HTTPS://github.com/jane");
		var lower = CvUrlNormalizer.NormalizeForComparison("https://github.com/jane");
		Assert.Equal(upper, lower);
	}

	[Fact]
	public void NormalizeForComparison_MalformedUrl_DoesNotThrow()
	{
		var result = CvUrlNormalizer.NormalizeForComparison("not-a-url-at-all!");
		Assert.NotNull(result);
	}

	// ── AreEquivalent ───────────────────────────────────────────────────────

	[Fact]
	public void AreEquivalent_BothNull_ReturnsFalse()
	{
		Assert.False(CvUrlNormalizer.AreEquivalent(null, null));
	}

	[Fact]
	public void AreEquivalent_LeftNull_ReturnsFalse()
	{
		Assert.False(CvUrlNormalizer.AreEquivalent(null, "https://example.com"));
	}

	[Fact]
	public void AreEquivalent_RightNull_ReturnsFalse()
	{
		Assert.False(CvUrlNormalizer.AreEquivalent("https://example.com", null));
	}

	[Fact]
	public void AreEquivalent_BothEmpty_ReturnsFalse()
	{
		Assert.False(CvUrlNormalizer.AreEquivalent(string.Empty, string.Empty));
	}

	[Fact]
	public void AreEquivalent_IdenticalUrls_ReturnsTrue()
	{
		Assert.True(CvUrlNormalizer.AreEquivalent(
			"https://github.com/jane",
			"https://github.com/jane"));
	}

	[Theory]
	[InlineData("https://www.linkedin.com/in/jane/", "https://linkedin.com/in/jane")]
	[InlineData("https://www.github.com/jane/", "https://github.com/jane")]
	[InlineData("https://LINKEDIN.com/in/jane", "https://linkedin.com/in/jane")]
	public void AreEquivalent_NormalizableVariants_ReturnsTrue(string left, string right)
	{
		Assert.True(CvUrlNormalizer.AreEquivalent(left, right));
	}

	[Fact]
	public void AreEquivalent_HttpVsHttps_ReturnsFalse()
	{
		Assert.False(CvUrlNormalizer.AreEquivalent(
			"http://github.com/jane",
			"https://github.com/jane"));
	}

	[Fact]
	public void AreEquivalent_PathIsCaseSensitive()
	{
		// "/Jane" vs "/jane" are different paths (path is NOT lowercased).
		Assert.False(CvUrlNormalizer.AreEquivalent(
			"https://github.com/Jane",
			"https://github.com/jane"));
	}

	[Fact]
	public void AreEquivalent_DifferentHosts_ReturnsFalse()
	{
		Assert.False(CvUrlNormalizer.AreEquivalent(
			"https://github.com/jane",
			"https://gitlab.com/jane"));
	}

	[Fact]
	public void AreEquivalent_DifferentPaths_ReturnsFalse()
	{
		Assert.False(CvUrlNormalizer.AreEquivalent(
			"https://linkedin.com/in/jane",
			"https://linkedin.com/in/john"));
	}
}
