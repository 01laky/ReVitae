using ReVitae.Core.Ai.Cv;

namespace ReVitae.Tests.Ai.Cv;

public sealed class AiCvResponseParserAdviceEdgeCaseTests
{
	[Fact]
	public void ParseAdviceList_AsteriskBullets_Parsed()
	{
		var items = AiCvResponseParser.ParseAdviceList("* first\n* second");
		Assert.Equal(2, items.Count);
		Assert.Equal("first", items[0].Advice);
	}

	[Fact]
	public void ParseAdviceList_MixedBulletStyles_AllParsed()
	{
		var items = AiCvResponseParser.ParseAdviceList("- dash\n* star\n1. number");
		Assert.Equal(3, items.Count);
		Assert.Equal("dash", items[0].Advice);
		Assert.Equal("star", items[1].Advice);
		Assert.Equal("number", items[2].Advice);
	}

	[Fact]
	public void ParseAdviceList_MultipleEmDashes_FirstSplitsRationale()
	{
		var items = AiCvResponseParser.ParseAdviceList("- advice — reason — extra");
		Assert.Equal("advice", items[0].Advice);
		Assert.Equal("reason — extra", items[0].Rationale);
	}

	[Fact]
	public void ParseAdviceList_BlankLinesBetween_Skipped()
	{
		var items = AiCvResponseParser.ParseAdviceList("- a\n\n\n- b");
		Assert.Equal(2, items.Count);
	}

	[Fact]
	public void ParseAdviceList_BulletWithNoText_Skipped()
	{
		var items = AiCvResponseParser.ParseAdviceList("- \n- real advice");
		Assert.Single(items);
		Assert.Equal("real advice", items[0].Advice);
	}

	[Fact]
	public void ParseAdviceList_MoreThanFourWithBlanks_CapsAtFour()
	{
		var items = AiCvResponseParser.ParseAdviceList("- a\n\n- b\n- c\n\n- d\n- e\n- f");
		Assert.Equal(4, items.Count);
	}

	[Fact]
	public void ParseAdviceList_LeadingTrailingWhitespace_Trimmed()
	{
		var items = AiCvResponseParser.ParseAdviceList("   -   spaced advice   ");
		Assert.Equal("spaced advice", items[0].Advice);
	}

	[Fact]
	public void ParseAdviceList_EmDashTakesPrecedenceOverHyphen()
	{
		var items = AiCvResponseParser.ParseAdviceList("- advice — em wins - not hyphen");
		Assert.Equal("advice", items[0].Advice);
		Assert.Equal("em wins - not hyphen", items[0].Rationale);
	}

	[Fact]
	public void ParseAdviceList_OnlyBlankAndBulletStubs_Throws()
	{
		Assert.Throws<AiCvResponseParseException>(
			() => AiCvResponseParser.ParseAdviceList("- \n\n* \n   "));
	}

	[Fact]
	public void ParseAdviceList_FencedWithLanguageTag_Stripped()
	{
		var items = AiCvResponseParser.ParseAdviceList("```text\n- inside fence\n```");
		Assert.Single(items);
		Assert.Equal("inside fence", items[0].Advice);
	}

	[Fact]
	public void Parse_ShortenSummary_RespectsMaxLength()
	{
		// 600-char cap for ShortenProfessionalSummary; a longer reply fails.
		var tooLong = new string('a', 700);
		Assert.Throws<AiCvResponseParseException>(
			() => AiCvResponseParser.Parse(tooLong, AiCvTaskKind.ShortenProfessionalSummary));
	}

	[Fact]
	public void Parse_ShortenSummary_WithinLimit_Succeeds()
	{
		var ok = new string('a', 400);
		Assert.Equal(ok, AiCvResponseParser.Parse(ok, AiCvTaskKind.ShortenProfessionalSummary));
	}
}
