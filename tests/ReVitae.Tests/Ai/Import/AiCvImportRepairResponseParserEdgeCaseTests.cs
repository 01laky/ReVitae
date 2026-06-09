using ReVitae.Core.Ai.Import;

namespace ReVitae.Tests.Ai.Import;

public sealed class AiCvImportRepairResponseParserEdgeCaseTests
{
	[Fact]
	public void Parse_DuplicateIndex_LastWins()
	{
		var map = AiCvImportRepairResponseParser.Parse("1: First\n1: Second");
		Assert.Equal("Second", map[1]);
	}

	[Fact]
	public void Parse_IndexZero_Ignored()
	{
		var map = AiCvImportRepairResponseParser.Parse("0: nope\n1: yes");
		Assert.False(map.ContainsKey(0));
		Assert.Equal("yes", map[1]);
	}

	[Fact]
	public void Parse_ValueContainingColon_Preserved()
	{
		var map = AiCvImportRepairResponseParser.Parse("1: https://example.com/path");
		Assert.Equal("https://example.com/path", map[1]);
	}

	[Fact]
	public void Parse_WhitespaceAroundValue_Trimmed()
	{
		var map = AiCvImportRepairResponseParser.Parse("1:    spaced value   ");
		Assert.Equal("spaced value", map[1]);
	}

	[Fact]
	public void Parse_MultilineMixedNoise_OnlyValidLines()
	{
		var map = AiCvImportRepairResponseParser.Parse("Here you go:\n1: Alpha\nrandom text\n2: Beta\n");
		Assert.Equal(2, map.Count);
		Assert.Equal("Alpha", map[1]);
		Assert.Equal("Beta", map[2]);
	}

	[Fact]
	public void Parse_HighIndices_Parsed()
	{
		var map = AiCvImportRepairResponseParser.Parse("10: Ten\n25: TwentyFive");
		Assert.Equal("Ten", map[10]);
		Assert.Equal("TwentyFive", map[25]);
	}

	[Fact]
	public void Parse_NullOrWhitespace_ReturnsEmpty()
	{
		Assert.Empty(AiCvImportRepairResponseParser.Parse(null!));
		Assert.Empty(AiCvImportRepairResponseParser.Parse("   \n  "));
	}
}
