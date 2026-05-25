using ReVitae.Core.Cv.Languages;
using ReVitae.Core.Localization;
using ReVitae.Core.Validation;

namespace ReVitae.Tests;

public sealed class LanguagesTests
{
	private static readonly LanguagesCollectionValidator Validator = new();

	private static LanguageEntry CreateValidEntry()
	{
		return new LanguageEntry
		{
			Language = "English",
			Proficiency = LanguageProficiency.Fluent,
			CefrLevel = CefrLevel.C1,
			Certificate = "IELTS 8.0",
			ReadingProficiency = LanguageProficiency.Fluent,
			WritingProficiency = LanguageProficiency.Advanced,
			SpeakingProficiency = LanguageProficiency.Fluent,
			ListeningProficiency = LanguageProficiency.Fluent
		};
	}

	[Fact]
	public void Validate_AcceptsValidCompleteEntry()
	{
		var result = Validator.Validate([CreateValidEntry()]);

		Assert.True(result.IsValid);
	}

	[Fact]
	public void Validate_AcceptsEmptyList()
	{
		var result = Validator.Validate(Array.Empty<LanguageEntry>());

		Assert.True(result.IsValid);
	}

	[Fact]
	public void HasUserInput_TreatsBlankEntryAsDraft()
	{
		Assert.False(new LanguageEntry().HasUserInput());
	}

	[Fact]
	public void Validate_IgnoresDraftEntryWithoutUserInput()
	{
		var result = Validator.Validate([new LanguageEntry()]);

		Assert.True(result.IsValid);
	}

	[Fact]
	public void Validate_ReportsMissingLanguageForActiveEntry()
	{
		var entry = new LanguageEntry { Proficiency = LanguageProficiency.Native };

		var result = Validator.Validate([entry]);

		Assert.False(result.IsValid);
		Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationLanguagesLanguageRequired);
	}

	[Fact]
	public void Validate_RejectsDuplicateLanguageNames()
	{
		var first = CreateValidEntry();
		var second = CreateValidEntry();
		second.Certificate = "Cambridge";

		var result = Validator.Validate([first, second]);

		Assert.False(result.IsValid);
		Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationLanguagesDuplicateLanguage);
	}

	[Fact]
	public void ValidateField_RejectsInvalidCefrValue()
	{
		var validator = LanguagesSchema.CreateEntryValidator();
		var result = validator.ValidateField(LanguagesFieldKeys.CefrLevel, "Z9");

		Assert.False(result.IsValid);
		Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationLanguagesCefrInvalid);
	}

	[Theory]
	[InlineData("language", 81, TranslationKeys.ValidationLanguagesLanguageMax)]
	[InlineData("certificate", 121, TranslationKeys.ValidationLanguagesCertificateMax)]
	public void Validate_RejectsValuesOverMaximumLength(string fieldName, int length, string expectedMessageKey)
	{
		var entry = CreateValidEntry();
		if (fieldName == "language")
		{
			entry.Language = new string('a', length);
		}
		else
		{
			entry.Certificate = new string('a', length);
		}

		var result = Validator.Validate([entry]);

		Assert.False(result.IsValid);
		Assert.Contains(result.Errors, error => error.Message == expectedMessageKey);
	}

	[Fact]
	public void Duplicate_CopiesAllFieldValuesWithNewIdentity()
	{
		var source = CreateValidEntry();
		var duplicate = source.Duplicate();

		Assert.NotEqual(source.Id, duplicate.Id);
		Assert.Equal(source.Language, duplicate.Language);
		Assert.Equal(source.Proficiency, duplicate.Proficiency);
		Assert.Equal(source.CefrLevel, duplicate.CefrLevel);
		Assert.Equal(source.Certificate, duplicate.Certificate);
		Assert.Equal(source.ReadingProficiency, duplicate.ReadingProficiency);
		Assert.Equal(source.WritingProficiency, duplicate.WritingProficiency);
		Assert.Equal(source.SpeakingProficiency, duplicate.SpeakingProficiency);
		Assert.Equal(source.ListeningProficiency, duplicate.ListeningProficiency);
		Assert.True(duplicate.HasUserInput());
	}

	[Fact]
	public void LanguageFlagResolver_ReturnsKnownFlagForSupportedLanguage()
	{
		Assert.Equal("🇸🇰", LanguageFlagResolver.ResolveFlagEmoji("Slovak"));
	}

	[Fact]
	public void LanguageFlagResolver_ReturnsFallbackForUnknownLanguage()
	{
		Assert.Equal("🌐", LanguageFlagResolver.ResolveFlagEmoji("Klingon"));
	}

	[Fact]
	public void Suggestions_FilterMatchesCaseInsensitively()
	{
		var matches = LanguageSuggestions.Filter("ger");

		Assert.Contains("German", matches);
	}

	[Fact]
	public void PreviewFormatter_IncludesSubSkillLines()
	{
		var entry = CreateValidEntry();
		var localizer = new AppLocalizer(AppLocalizer.FallbackLanguageCode);

		var subSkillLines = LanguagePreviewFormatter.FormatSubSkillLines(entry, localizer);

		Assert.Equal(4, subSkillLines.Count);
	}

	[Fact]
	public void SchemaFields_UseTranslationKeysForValidationMessages()
	{
		foreach (var field in LanguagesSchema.EntryFields)
		{
			if (field.IsRequired)
			{
				Assert.StartsWith("validation.", field.RequiredMessage);
			}

			Assert.StartsWith("validation.", field.MaximumLengthMessage);
		}
	}
}
