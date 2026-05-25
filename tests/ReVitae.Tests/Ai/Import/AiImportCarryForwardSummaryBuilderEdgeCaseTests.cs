using System.Text.Json.Nodes;
using ReVitae.Core.Ai.Import;

namespace ReVitae.Tests.Ai.Import;

public sealed class AiImportCarryForwardSummaryBuilderEdgeCaseTests
{
	[Fact]
	public void Build_EmptyObject_ReturnsNone()
	{
		var summary = AiImportCarryForwardSummaryBuilder.Build(new JsonObject(), maxChars: 500);

		Assert.Equal("none", summary);
	}

	[Fact]
	public void Build_IncludesPersonalAndCounts()
	{
		var root = new JsonObject
		{
			["personalInformation"] = new JsonObject
			{
				["firstName"] = "Jane",
				["lastName"] = "Doe",
				["email"] = "jane@example.com",
			},
			["workExperience"] = new JsonArray(new JsonObject(), new JsonObject()),
			["skills"] = new JsonArray(new JsonObject()),
		};

		var summary = AiImportCarryForwardSummaryBuilder.Build(root, maxChars: 500);

		Assert.Contains("Jane Doe", summary, StringComparison.Ordinal);
		Assert.Contains("Email: jane@example.com", summary, StringComparison.Ordinal);
		Assert.Contains("Work: 2 entries", summary, StringComparison.Ordinal);
		Assert.Contains("Skills: 1 entries", summary, StringComparison.Ordinal);
	}

	[Fact]
	public void Build_TruncatesToMaxChars()
	{
		var root = new JsonObject
		{
			["personalInformation"] = new JsonObject
			{
				["firstName"] = new string('A', 200),
				["lastName"] = new string('B', 200),
			},
		};

		var summary = AiImportCarryForwardSummaryBuilder.Build(root, maxChars: 50);

		Assert.True(summary.Length <= 50);
	}

	[Fact]
	public void Build_ZeroMaxChars_ReturnsEmpty()
	{
		var root = new JsonObject
		{
			["personalInformation"] = new JsonObject { ["firstName"] = "Jane" },
		};

		Assert.Equal(string.Empty, AiImportCarryForwardSummaryBuilder.Build(root, maxChars: 0));
	}

	[Fact]
	public void Build_NegativeMaxChars_ReturnsEmpty()
	{
		Assert.Equal(string.Empty, AiImportCarryForwardSummaryBuilder.Build(new JsonObject(), maxChars: -1));
	}

	[Fact]
	public void Build_SkipsEmptyPersonalName()
	{
		var root = new JsonObject
		{
			["personalInformation"] = new JsonObject
			{
				["firstName"] = "   ",
				["lastName"] = "",
			},
			["languages"] = new JsonArray(new JsonObject()),
		};

		var summary = AiImportCarryForwardSummaryBuilder.Build(root, maxChars: 200);

		Assert.Contains("Languages: 1 entries", summary, StringComparison.Ordinal);
		Assert.DoesNotContain("Name:", summary, StringComparison.Ordinal);
	}
}
