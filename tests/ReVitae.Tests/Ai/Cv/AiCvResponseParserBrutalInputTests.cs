using ReVitae.Core.Ai.Cv;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Ai.Cv;

/// <summary>
/// Prompt 049 B3 — adversarial robustness of the LLM response parser. The contract is that
/// for any non-null input, <see cref="AiCvResponseParser.Parse"/> either returns a string or
/// throws <see cref="AiCvResponseParseException"/> — never any other exception type. The same
/// "typed-or-value" invariant holds for <see cref="AiCvResponseParser.ParseAdviceList"/>,
/// which is additionally null-safe. Includes a deterministic (seeded) fuzz sweep.
/// </summary>
[Trait("Category", "Property")]
public sealed class AiCvResponseParserBrutalInputTests
{
	public static IEnumerable<object[]> MalformedInputs =>
	[
		["{\"summary\": \"truncated"],
		["```json\n{\"x\":1}\n```"],
		["﻿Hello world"],
		["Text with  control chars"],
		["```"],
		["``````"],
		["```\n```"],
		["1.\n2)\n* \n- "],
		["Great summary 🚀🎯"],
		["   leading and trailing   "],
		["```\n```\ninner text\n```\n```"],
		["\n\n\n\t\t  \n"],
		["}{][\"':;,.<>/\\|"],
		["normal advisor output, nothing special"],
		["\uD800 lone high surrogate"],
		["a\rb\nc\r\nd"]
	];

	[Theory]
	[MemberData(nameof(MalformedInputs))]
	public void Parse_ThrowsOnlyTypedExceptionForAnyNonNullInput(string input)
	{
		var exception = Record.Exception(() => AiCvResponseParser.Parse(input, AiCvTaskKind.ImproveProfessionalSummary));

		Assert.True(
			exception is null or AiCvResponseParseException,
			$"Parse threw an unexpected {exception?.GetType().Name} for input <{input}>.");
	}

	[Theory]
	[MemberData(nameof(MalformedInputs))]
	public void ParseAdviceList_ThrowsOnlyTypedExceptionForAnyNonNullInput(string input)
	{
		var exception = Record.Exception(() => AiCvResponseParser.ParseAdviceList(input));

		Assert.True(
			exception is null or AiCvResponseParseException,
			$"ParseAdviceList threw an unexpected {exception?.GetType().Name} for input <{input}>.");
	}

	[Fact]
	public void ParseAdviceList_Null_ThrowsTypedException()
	{
		Assert.Throws<AiCvResponseParseException>(() => AiCvResponseParser.ParseAdviceList(null!));
	}

	[Fact]
	public void Parse_WhitespaceOnly_ThrowsEmptyResponse()
	{
		var exception = Assert.Throws<AiCvResponseParseException>(
			() => AiCvResponseParser.Parse("   \t\n  ", AiCvTaskKind.ImproveProfessionalSummary));
		Assert.Equal(TranslationKeys.AiCvEmptyResponse, exception.ErrorMessageKey);
	}

	[Fact]
	public void Parse_EmptyFence_ThrowsEmptyResponse()
	{
		var exception = Assert.Throws<AiCvResponseParseException>(
			() => AiCvResponseParser.Parse("```\n```", AiCvTaskKind.ImproveProfessionalSummary));
		Assert.Equal(TranslationKeys.AiCvEmptyResponse, exception.ErrorMessageKey);
	}

	[Fact]
	public void Parse_OverMaxLength_ThrowsResponseTooLong()
	{
		var huge = new string('x', 5000);
		var exception = Assert.Throws<AiCvResponseParseException>(
			() => AiCvResponseParser.Parse(huge, AiCvTaskKind.ImproveProfessionalSummary));
		Assert.Equal(TranslationKeys.AiCvResponseTooLong, exception.ErrorMessageKey);
	}

	[Fact]
	public void Parse_StripsLanguageTaggedFence_ReturnsInnerContent()
	{
		var result = AiCvResponseParser.Parse("```text\nClean summary content.\n```", AiCvTaskKind.DraftProfessionalSummary);
		Assert.Equal("Clean summary content.", result);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	[InlineData(4)]
	[InlineData(5)]
	[InlineData(6)]
	[InlineData(7)]
	[InlineData(8)]
	[InlineData(9)]
	[InlineData(10)]
	[InlineData(11)]
	public void Parse_DeterministicFuzz_NeverThrowsUntypedException(int seed)
	{
		var input = BuildFuzzString(seed);
		var task = (AiCvTaskKind)(seed % 6);

		var exception = Record.Exception(() => AiCvResponseParser.Parse(input, task));

		Assert.True(
			exception is null or AiCvResponseParseException,
			$"Seed {seed} produced an unexpected {exception?.GetType().Name}.");
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	[InlineData(4)]
	[InlineData(5)]
	[InlineData(6)]
	[InlineData(7)]
	public void ParseAdviceList_DeterministicFuzz_NeverThrowsUntypedException(int seed)
	{
		var input = BuildFuzzString(seed);

		var exception = Record.Exception(() => AiCvResponseParser.ParseAdviceList(input));

		Assert.True(
			exception is null or AiCvResponseParseException,
			$"Seed {seed} produced an unexpected {exception?.GetType().Name}.");
	}

	// Deterministic generator (no Random/Date — reproducible). Mixes fences, bullets,
	// punctuation, control chars, and unicode based purely on the seed.
	private static string BuildFuzzString(int seed)
	{
		const string pool = "```\n- * • 1.2) — - {}[]\":,abc  é日🚀";
		var length = 3 + (seed * 7 % 40);
		var chars = new char[length];
		var state = unchecked(((uint)seed * 2654435761u) + 1u);
		for (var i = 0; i < length; i++)
		{
			state = unchecked((state * 1664525u) + 1013904223u);
			chars[i] = pool[(int)(state % (uint)pool.Length)];
		}

		return new string(chars);
	}
}
