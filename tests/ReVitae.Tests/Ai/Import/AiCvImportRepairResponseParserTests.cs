using ReVitae.Core.Ai.Import;

namespace ReVitae.Tests.Ai.Import;

public sealed class AiCvImportRepairResponseParserTests
{
	[Fact]
	public void Parse_ColonSeparated_MapsIndices()
	{
		var map = AiCvImportRepairResponseParser.Parse("1: John Doe\n2: Acme Corp");
		Assert.Equal("John Doe", map[1]);
		Assert.Equal("Acme Corp", map[2]);
	}

	[Theory]
	[InlineData("1) John")]
	[InlineData("1. John")]
	[InlineData("1 - John")]
	public void Parse_AcceptsCommonSeparators(string line)
	{
		var map = AiCvImportRepairResponseParser.Parse(line);
		Assert.Equal("John", map[1]);
	}

	[Fact]
	public void Parse_IgnoresNonMatchingLines()
	{
		var map = AiCvImportRepairResponseParser.Parse("Here are the fixes:\n1: Fixed\nThanks!");
		Assert.Single(map);
		Assert.Equal("Fixed", map[1]);
	}

	[Fact]
	public void Parse_Empty_ReturnsEmptyMap()
	{
		Assert.Empty(AiCvImportRepairResponseParser.Parse(string.Empty));
	}
}
