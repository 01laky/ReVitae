using ReVitae.Core.Localization;

namespace ReVitae.Tests;

public sealed class TranslationKeysTests
{
	[Fact]
	public void RequiredKeys_ContainNoDuplicates()
	{
		var duplicates = TranslationKeys.RequiredKeys
			.GroupBy(key => key, StringComparer.Ordinal)
			.Where(group => group.Count() > 1)
			.Select(group => group.Key)
			.ToArray();

		Assert.Empty(duplicates);
	}

	[Fact]
	public void RequiredKeys_ContainNoEmptyValues()
	{
		Assert.All(TranslationKeys.RequiredKeys, key => Assert.False(string.IsNullOrWhiteSpace(key)));
	}

	[Fact]
	public void EnglishTranslations_ContainEveryRequiredKey()
	{
		var englishTranslations = AppLocalizer.GetTranslations(AppLocalizer.FallbackLanguageCode);

		foreach (var key in TranslationKeys.RequiredKeys)
		{
			Assert.True(
				englishTranslations.ContainsKey(key),
				$"English translations are missing key '{key}'.");
		}
	}

	[Fact]
	public void ValidationMessageKeys_AreUsedByPersonalInformationSchema()
	{
		var schemaMessageKeys = ReVitae.Core.Cv.MainPersonalInformationSchema.Fields
			.SelectMany(field => new[] { field.RequiredMessage, field.MaximumLengthMessage, field.FormatMessage })
			.Where(message => !string.IsNullOrWhiteSpace(message))
			.Distinct()
			.ToArray();

		foreach (var messageKey in schemaMessageKeys)
		{
			Assert.Contains(messageKey, TranslationKeys.RequiredKeys);
		}
	}
}
