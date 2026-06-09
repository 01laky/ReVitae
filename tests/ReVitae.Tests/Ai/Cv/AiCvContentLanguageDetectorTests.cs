using ReVitae.Core.Ai.Cv;

namespace ReVitae.Tests.Ai.Cv;

public sealed class AiCvContentLanguageDetectorTests
{
	[Fact]
	public void Detect_SlovakText_ReturnsSk_EvenWithEnglishFallback()
	{
		var text = "Viedol som vývojový tím a zodpovedal za škálovanie služieb a kvalitu kódu.";
		Assert.Equal("sk", AiCvContentLanguageDetector.Detect(text, "en"));
	}

	[Fact]
	public void Detect_EnglishText_ReturnsFallback()
	{
		var text = "Led the development team and owned scaling and code quality.";
		Assert.Equal("en", AiCvContentLanguageDetector.Detect(text, "en"));
	}

	[Fact]
	public void Detect_CzechSpecificText_ReturnsCs()
	{
		var text = "Řídil jsem vývojový tým a měl jsem na starosti kvalitu.";
		Assert.Equal("cs", AiCvContentLanguageDetector.Detect(text, "en"));
	}

	[Fact]
	public void Detect_EmptyText_ReturnsFallback()
	{
		Assert.Equal("sk", AiCvContentLanguageDetector.Detect(string.Empty, "sk"));
		Assert.Equal("en", AiCvContentLanguageDetector.Detect(null, "en"));
	}

	[Fact]
	public void Detect_SingleDiacritic_BelowThreshold_ReturnsFallback()
	{
		// Only one signal char — ambiguous, fall back.
		Assert.Equal("en", AiCvContentLanguageDetector.Detect("Café manager role", "en"));
	}
}
