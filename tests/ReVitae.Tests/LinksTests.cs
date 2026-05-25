using ReVitae.Core.Cv.Links;
using ReVitae.Core.Localization;
using ReVitae.Core.Validation;

namespace ReVitae.Tests;

public sealed class LinksTests
{
	private static readonly LinksCollectionValidator Validator = new();

	private static LinkEntry CreateValidEntry()
	{
		return new LinkEntry
		{
			Label = "Behance",
			Url = "https://behance.net/username",
			Note = "design portfolio"
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
		var result = Validator.Validate(Array.Empty<LinkEntry>());

		Assert.True(result.IsValid);
	}

	[Fact]
	public void HasUserInput_TreatsBlankEntryAsDraft()
	{
		Assert.False(new LinkEntry().HasUserInput());
	}

	[Fact]
	public void Validate_IgnoresDraftEntryWithoutUserInput()
	{
		var result = Validator.Validate([new LinkEntry()]);

		Assert.True(result.IsValid);
	}

	[Fact]
	public void Validate_ActivatesEntryAfterFirstFieldInput()
	{
		var entry = new LinkEntry { Label = "Medium" };

		var result = Validator.Validate([entry]);

		Assert.False(result.IsValid);
		Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationLinksUrlRequired);
	}

	[Fact]
	public void Validate_ReportsRequiredFieldFailuresForActiveEntry()
	{
		var entry = new LinkEntry { Url = "https://example.com" };

		var result = Validator.Validate([entry]);

		Assert.False(result.IsValid);
		Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationLinksLabelRequired);
	}

	[Theory]
	[InlineData("label", 81, TranslationKeys.ValidationLinksLabelMax)]
	[InlineData("url", 241, TranslationKeys.ValidationLinksUrlMax)]
	[InlineData("note", 121, TranslationKeys.ValidationLinksNoteMax)]
	public void Validate_RejectsValuesOverMaximumLength(string fieldName, int length, string expectedMessageKey)
	{
		var entry = CreateValidEntry();
		SetField(entry, fieldName, new string('a', length));

		var result = Validator.Validate([entry]);

		Assert.False(result.IsValid);
		Assert.Contains(result.Errors, error =>
			error.FieldKey.EndsWith("." + fieldName, StringComparison.Ordinal)
			&& error.Message == expectedMessageKey);
	}

	[Fact]
	public void Validate_AcceptsValidUrl()
	{
		var entry = CreateValidEntry();
		entry.Url = "https://example.com/profile";

		var result = Validator.Validate([entry]);

		Assert.True(result.IsValid);
	}

	[Fact]
	public void Validate_RejectsInvalidUrl()
	{
		var entry = CreateValidEntry();
		entry.Url = "example.com";

		var result = Validator.Validate([entry]);

		Assert.False(result.IsValid);
		Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationLinksUrlFormat);
	}

	[Fact]
	public void Validate_RejectsDuplicateUrlsAcrossActiveEntries()
	{
		var first = CreateValidEntry();
		var second = new LinkEntry
		{
			Label = "Portfolio mirror",
			Url = "https://behance.net/username"
		};

		var result = Validator.Validate([first, second]);

		Assert.False(result.IsValid);
		Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationLinksDuplicateUrl);
	}

	[Fact]
	public void Validate_RejectsDuplicateUrlsCaseInsensitively()
	{
		var first = CreateValidEntry();
		var second = new LinkEntry
		{
			Label = "Behance mirror",
			Url = "HTTPS://BEHANCE.NET/USERNAME"
		};

		var result = Validator.Validate([first, second]);

		Assert.False(result.IsValid);
		Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationLinksDuplicateUrl);
	}

	[Fact]
	public void Validate_RejectsWhitespaceOnlyRequiredValuesInsideActiveEntry()
	{
		var entry = new LinkEntry
		{
			Label = "   ",
			Url = "   ",
			Note = "note"
		};

		var result = Validator.Validate([entry]);

		Assert.False(result.IsValid);
		Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationLinksLabelRequired);
		Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationLinksUrlRequired);
	}

	[Fact]
	public void Duplicate_CopiesAllFieldValuesWithNewIdentity()
	{
		var source = CreateValidEntry();
		var duplicate = source.Duplicate();

		Assert.NotEqual(source.Id, duplicate.Id);
		Assert.Equal(source.Label, duplicate.Label);
		Assert.Equal(source.Url, duplicate.Url);
		Assert.Equal(source.Note, duplicate.Note);
	}

	[Fact]
	public void FieldKeys_AreUniqueAcrossEntries()
	{
		var first = CreateValidEntry();
		var second = CreateValidEntry();

		var firstKeys = first.ToFieldValues().Keys.ToArray();
		var secondKeys = second.ToFieldValues().Keys.ToArray();

		Assert.Empty(firstKeys.Intersect(secondKeys, StringComparer.Ordinal));
	}

	[Fact]
	public void SchemaFields_UseTranslationKeysForValidationMessages()
	{
		foreach (var field in LinksSchema.EntryFields)
		{
			if (!string.IsNullOrEmpty(field.RequiredMessage))
			{
				Assert.StartsWith("validation.", field.RequiredMessage, StringComparison.Ordinal);
			}

			if (!string.IsNullOrEmpty(field.MaximumLengthMessage))
			{
				Assert.StartsWith("validation.", field.MaximumLengthMessage, StringComparison.Ordinal);
			}

			if (!string.IsNullOrEmpty(field.FormatMessage))
			{
				Assert.StartsWith("validation.", field.FormatMessage, StringComparison.Ordinal);
			}
		}
	}

	[Fact]
	public void BuildHeaderSummary_UsesLabelAndUrl()
	{
		var entry = new LinkEntry
		{
			Label = "ORCID",
			Url = "https://orcid.org/0000-0002-1825-0097"
		};

		Assert.Equal("ORCID · https://orcid.org/0000-0002-1825-0097", entry.BuildHeaderSummary());
	}

	[Fact]
	public void FormatLine_IncludesNoteWhenPresent()
	{
		var entry = CreateValidEntry();

		var line = LinkPreviewFormatter.FormatLine(entry);

		Assert.Contains("Behance:", line, StringComparison.Ordinal);
		Assert.Contains("https://behance.net/username", line, StringComparison.Ordinal);
		Assert.Contains("design portfolio", line, StringComparison.Ordinal);
	}

	[Fact]
	public void FormatLine_OmitsNoteWhenEmpty()
	{
		var entry = CreateValidEntry();
		entry.Note = string.Empty;

		var line = LinkPreviewFormatter.FormatLine(entry);

		Assert.Equal("Behance: https://behance.net/username", line);
	}

	[Theory]
	[InlineData("Beh", "Behance")]
	[InlineData("scholar", "Google Scholar")]
	public void LinkLabelSuggestions_FiltersByQuery(string query, string expectedMatch)
	{
		var results = LinkLabelSuggestions.Filter(query);

		Assert.Contains(expectedMatch, results);
	}

	private static void SetField(LinkEntry entry, string fieldName, string value)
	{
		switch (fieldName)
		{
			case "label":
				entry.Label = value;
				break;
			case "url":
				entry.Url = value;
				break;
			case "note":
				entry.Note = value;
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(fieldName));
		}
	}
}
