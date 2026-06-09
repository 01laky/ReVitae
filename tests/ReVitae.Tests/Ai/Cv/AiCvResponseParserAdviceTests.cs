using ReVitae.Core.Ai.Cv;

namespace ReVitae.Tests.Ai.Cv;

public sealed class AiCvResponseParserAdviceTests
{
	[Fact]
	public void ParseAdviceList_BulletsWithRationale_SplitsAdviceAndWhy()
	{
		var raw = "- Group skills into categories — recruiters scan faster\n- Drop generic entries";
		var items = AiCvResponseParser.ParseAdviceList(raw);

		Assert.Equal(2, items.Count);
		Assert.Equal("Group skills into categories", items[0].Advice);
		Assert.Equal("recruiters scan faster", items[0].Rationale);
		Assert.Null(items[1].Rationale);
	}

	[Fact]
	public void ParseAdviceList_NumberedList_StripsPrefix()
	{
		var raw = "1. Lead with relevant skills\n2) Remove duplicates";
		var items = AiCvResponseParser.ParseAdviceList(raw);

		Assert.Equal(2, items.Count);
		Assert.Equal("Lead with relevant skills", items[0].Advice);
		Assert.Equal("Remove duplicates", items[1].Advice);
	}

	[Fact]
	public void ParseAdviceList_MarkdownFences_AreStripped()
	{
		var raw = "```\n- Add measurable results\n```";
		var items = AiCvResponseParser.ParseAdviceList(raw);

		Assert.Single(items);
		Assert.Equal("Add measurable results", items[0].Advice);
	}

	[Fact]
	public void ParseAdviceList_CapsAtFourItems()
	{
		var raw = "- a\n- b\n- c\n- d\n- e\n- f";
		var items = AiCvResponseParser.ParseAdviceList(raw);
		Assert.Equal(4, items.Count);
	}

	[Fact]
	public void ParseAdviceList_HyphenRationale_Splits()
	{
		var items = AiCvResponseParser.ParseAdviceList("- Quantify impact - it shows results");
		Assert.Equal("Quantify impact", items[0].Advice);
		Assert.Equal("it shows results", items[0].Rationale);
	}

	[Fact]
	public void ParseAdviceList_Empty_Throws()
	{
		Assert.Throws<AiCvResponseParseException>(() => AiCvResponseParser.ParseAdviceList("   "));
	}

	[Fact]
	public void ParseAdviceList_PlainLinesWithoutBullets_StillParsed()
	{
		var items = AiCvResponseParser.ParseAdviceList("Add a summary line\nList your top skills");
		Assert.Equal(2, items.Count);
	}
}
