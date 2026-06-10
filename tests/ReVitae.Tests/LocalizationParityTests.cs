using System.Reflection;
using System.Text.RegularExpressions;
using ReVitae.Core.Localization;

namespace ReVitae.Tests;

/// <summary>
/// Prompt 049 B12 — localization correctness. Every declared translation key must resolve,
/// no translation may be empty, and the placeholder set (<c>{0}</c>, <c>{1}</c>, …) must be
/// identical across every language for a given key — a mismatched placeholder set throws at
/// runtime when <c>string.Format</c> runs.
/// </summary>
public sealed class LocalizationParityTests
{
	private static readonly IReadOnlyList<string> AllKeys = typeof(TranslationKeys)
		.GetFields(BindingFlags.Public | BindingFlags.Static)
		.Where(field => field is { IsLiteral: true, FieldType: var type } && type == typeof(string))
		.Select(field => (string)field.GetRawConstantValue()!)
		.Distinct(StringComparer.Ordinal)
		.ToArray();

	private static readonly IReadOnlyList<string> LanguageCodes =
		AppLocalizer.SupportedLanguages.Select(language => language.Code).ToArray();

	public static IEnumerable<object[]> Languages => LanguageCodes.Select(code => new object[] { code });

	private static ISet<int> Placeholders(string value) =>
		Regex.Matches(value, @"\{(\d+)\}")
			.Select(match => int.Parse(match.Groups[1].Value))
			.ToHashSet();

	[Fact]
	public void TranslationKeys_AreDiscovered()
	{
		Assert.True(AllKeys.Count > 200, $"Expected many translation keys, found {AllKeys.Count}.");
	}

	[Fact]
	public void EveryKey_ResolvesInEnglish()
	{
		var english = AppLocalizer.GetTranslations("en");
		var unresolved = AllKeys.Where(key => !english.ContainsKey(key)).ToArray();

		Assert.True(
			unresolved.Length == 0,
			$"Translation keys with no English entry: {string.Join(", ", unresolved)}");
	}

	[Theory]
	[MemberData(nameof(Languages))]
	public void EveryKey_ResolvesNonEmptyInEveryLanguage(string languageCode)
	{
		var translations = AppLocalizer.GetTranslations(languageCode);
		var localizer = new AppLocalizer(languageCode);

		var empty = AllKeys
			.Where(key => translations.ContainsKey(key))
			.Where(key => string.IsNullOrWhiteSpace(localizer.Get(key)))
			.ToArray();

		Assert.True(empty.Length == 0, $"{languageCode} has empty translations: {string.Join(", ", empty)}");
	}

	[Theory]
	[MemberData(nameof(Languages))]
	public void PlaceholderArity_MatchesEnglishForEveryKey(string languageCode)
	{
		var english = AppLocalizer.GetTranslations("en");
		var translations = AppLocalizer.GetTranslations(languageCode);

		var mismatches = new List<string>();
		foreach (var key in AllKeys)
		{
			if (!english.TryGetValue(key, out var englishValue) || !translations.TryGetValue(key, out var localValue))
			{
				continue;
			}

			if (!Placeholders(englishValue).SetEquals(Placeholders(localValue)))
			{
				mismatches.Add($"{key} (en:{englishValue} / {languageCode}:{localValue})");
			}
		}

		Assert.True(
			mismatches.Count == 0,
			$"Placeholder arity mismatches in {languageCode}: {string.Join(" | ", mismatches)}");
	}

	[Theory]
	[MemberData(nameof(Languages))]
	public void Format_DoesNotThrowForAnyPlaceholderKey(string languageCode)
	{
		var localizer = new AppLocalizer(languageCode);
		var translations = AppLocalizer.GetTranslations(languageCode);
		var args = new object[] { "A", "B", "C", "D" };

		foreach (var key in AllKeys.Where(k => translations.ContainsKey(k)))
		{
			var value = localizer.Get(key);
			if (Placeholders(value).Count == 0)
			{
				continue;
			}

			var exception = Record.Exception(() => string.Format(value, args));
			Assert.True(exception is null, $"{languageCode}/{key} threw on format: {exception?.Message}");
		}
	}
}
