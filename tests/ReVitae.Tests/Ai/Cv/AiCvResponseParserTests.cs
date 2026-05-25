using ReVitae.Core.Ai.Cv;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Ai.Cv;

public sealed class AiCvResponseParserTests
{
	[Fact]
	public void Parse_TrimsWhitespace()
	{
		var result = AiCvResponseParser.Parse("  Improved text.  ", AiCvTaskKind.ImproveWorkDescription);

		Assert.Equal("Improved text.", result);
	}

	[Fact]
	public void Parse_StripsMarkdownFences()
	{
		var result = AiCvResponseParser.Parse(
			"""
            ```text
            Improved text.
            ```
            """,
			AiCvTaskKind.ImproveWorkDescription);

		Assert.Equal("Improved text.", result);
	}

	[Fact]
	public void Parse_Empty_ThrowsEmptyResponse()
	{
		var ex = Assert.Throws<AiCvResponseParseException>(() =>
			AiCvResponseParser.Parse("   ", AiCvTaskKind.ImproveWorkDescription));

		Assert.Equal(TranslationKeys.AiCvEmptyResponse, ex.ErrorMessageKey);
	}

	[Fact]
	public void Parse_TooLong_ThrowsResponseTooLong()
	{
		var tooLong = new string('a', AiCvResponseParser.ResolveMaxLength(AiCvTaskKind.ImproveProfessionalSummary) + 1);

		var ex = Assert.Throws<AiCvResponseParseException>(() =>
			AiCvResponseParser.Parse(tooLong, AiCvTaskKind.ImproveProfessionalSummary));

		Assert.Equal(TranslationKeys.AiCvResponseTooLong, ex.ErrorMessageKey);
	}

	[Theory]
	[InlineData("plain", "plain")]
	public void StripMarkdownFences_HandlesPlainText(string input, string expectedStart)
	{
		var result = AiCvResponseParser.StripMarkdownFences(input);

		Assert.StartsWith(expectedStart, result, StringComparison.Ordinal);
	}
}
