using ReVitae.Core.Ai.Cv;

namespace ReVitae.Tests.Ai.Cv;

public sealed class AiCvContentLanguageDetectorEdgeCaseTests
{
	[Fact]
	public void Detect_Null_ReturnsFallback()
	{
		Assert.Equal("fr", AiCvContentLanguageDetector.Detect(null, "fr"));
	}

	[Fact]
	public void Detect_WhitespaceOnly_ReturnsFallback()
	{
		Assert.Equal("en", AiCvContentLanguageDetector.Detect("   \n\t", "en"));
	}

	[Fact]
	public void Detect_ExactlyTwoSlovakChars_ReturnsSk()
	{
		// "áä" — exactly the minimum signal.
		Assert.Equal("sk", AiCvContentLanguageDetector.Detect("manažér áä", "en"));
	}

	[Fact]
	public void Detect_OneSlovakChar_ReturnsFallback()
	{
		Assert.Equal("en", AiCvContentLanguageDetector.Detect("manažer", "en"));
	}

	[Fact]
	public void Detect_UppercaseSlovakDiacritics_ReturnsSk()
	{
		Assert.Equal("sk", AiCvContentLanguageDetector.Detect("ŠČ NÁZOV", "en"));
	}

	[Fact]
	public void Detect_OneCzechCharBelowTotalThreshold_ReturnsFallback()
	{
		// Single Czech-specific char (ř) but total signal < 2 → fallback.
		Assert.Equal("en", AiCvContentLanguageDetector.Detect("reřok", "en"));
	}

	[Fact]
	public void Detect_CzechCharPlusSlovakChar_ReturnsCs()
	{
		// One Czech-specific (ř) + one shared (á) → Czech wins.
		Assert.Equal("cs", AiCvContentLanguageDetector.Detect("práře", "en"));
	}

	[Fact]
	public void Detect_ManySlovakNoCzech_ReturnsSk()
	{
		Assert.Equal("sk", AiCvContentLanguageDetector.Detect("vývojár so skúsenosťami a kvalitou", "en"));
	}

	[Fact]
	public void Detect_PlainAsciiBusinessEnglish_ReturnsFallback()
	{
		Assert.Equal("de", AiCvContentLanguageDetector.Detect("Senior software engineer and team lead", "de"));
	}
}
