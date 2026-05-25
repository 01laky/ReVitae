using ReVitae.Core.Validation;

namespace ReVitae.Tests;

public sealed class FieldValidatorTests
{
	private const string TextKey = "textField";
	private const string RequiredTextKey = "requiredTextField";
	private const string EmailKey = "emailField";
	private const string UrlKey = "urlField";

	private static FieldValidator CreateValidator()
	{
		return new FieldValidator(
		[
			new FieldSchema(TextKey, "Text", IsRequired: false, 10, FieldFormat.Text, string.Empty, "Text is too long."),
			new FieldSchema(RequiredTextKey, "Required text", IsRequired: true, 10, FieldFormat.Text, "Required.", "Too long."),
			new FieldSchema(EmailKey, "Email", IsRequired: true, 30, FieldFormat.Email, "Email required.", "Email too long.", "Invalid email."),
			new FieldSchema(UrlKey, "URL", IsRequired: false, 100, FieldFormat.Url, string.Empty, "URL too long.", "Invalid URL.")
		]);
	}

	[Fact]
	public void Constructor_ThrowsWhenSchemaKeysAreDuplicated()
	{
		var duplicateSchemas = new[]
		{
			new FieldSchema("duplicate", "One", IsRequired: false, 10, FieldFormat.Text, string.Empty, "Too long."),
			new FieldSchema("duplicate", "Two", IsRequired: false, 10, FieldFormat.Text, string.Empty, "Too long.")
		};

		Assert.Throws<ArgumentException>(() => new FieldValidator(duplicateSchemas));
	}

	[Fact]
	public void ValidateField_ThrowsForUnknownFieldKey()
	{
		var validator = CreateValidator();

		var exception = Assert.Throws<ArgumentException>(() => validator.ValidateField("missing", "value"));

		Assert.Equal("fieldKey", exception.ParamName);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("\t\n")]
	public void ValidateField_RequiredField_RejectsNullEmptyOrWhitespaceOnly(string? value)
	{
		var validator = CreateValidator();

		var result = validator.ValidateField(RequiredTextKey, value);

		Assert.False(result.IsValid);
		Assert.Single(result.Errors);
		Assert.Equal(RequiredTextKey, result.Errors[0].FieldKey);
		Assert.Equal("Required.", result.Errors[0].Message);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void ValidateField_OptionalField_AcceptsNullEmptyOrWhitespaceOnly(string? value)
	{
		var validator = CreateValidator();

		var result = validator.ValidateField(TextKey, value);

		Assert.True(result.IsValid);
		Assert.Empty(result.Errors);
	}

	[Fact]
	public void ValidateField_TrimsLeadingAndTrailingWhitespaceBeforeValidation()
	{
		var validator = CreateValidator();

		var result = validator.ValidateField(RequiredTextKey, "  hello  ");

		Assert.True(result.IsValid);
	}

	[Fact]
	public void ValidateField_TextField_RejectsValueOverMaximumLength()
	{
		var validator = CreateValidator();

		var result = validator.ValidateField(TextKey, "12345678901");

		Assert.False(result.IsValid);
		Assert.Single(result.Errors);
		Assert.Equal("Text is too long.", result.Errors[0].Message);
	}

	[Fact]
	public void ValidateField_TextField_AcceptsValueExactlyAtMaximumLength()
	{
		var validator = CreateValidator();

		var result = validator.ValidateField(TextKey, "1234567890");

		Assert.True(result.IsValid);
	}

	[Fact]
	public void ValidateField_Email_ReportsBothMaxLengthAndFormatErrors()
	{
		var validator = CreateValidator();
		var tooLongInvalidEmail = new string('a', 27) + "@bad";

		var result = validator.ValidateField(EmailKey, tooLongInvalidEmail);

		Assert.False(result.IsValid);
		Assert.Equal(2, result.Errors.Count);
		Assert.All(result.Errors, error => Assert.Equal(EmailKey, error.FieldKey));
		Assert.Contains(result.Errors, error => error.Message == "Email too long.");
		Assert.Contains(result.Errors, error => error.Message == "Invalid email.");
	}

	[Theory]
	[InlineData("user@example.com")]
	[InlineData("USER@EXAMPLE.COM")]
	[InlineData("user.name+tag@example.co.uk")]
	public void ValidateField_Email_AcceptsValidAddresses(string email)
	{
		var validator = CreateValidator();

		var result = validator.ValidateField(EmailKey, email);

		Assert.True(result.IsValid);
	}

	[Theory]
	[InlineData("user")]
	[InlineData("user@")]
	[InlineData("@example.com")]
	[InlineData("user@@example.com")]
	[InlineData("user @example.com")]
	[InlineData("user@example")]
	public void ValidateField_Email_RejectsInvalidAddresses(string email)
	{
		var validator = CreateValidator();

		var result = validator.ValidateField(EmailKey, email);

		Assert.False(result.IsValid);
		Assert.Contains(result.Errors, error => error.Message == "Invalid email.");
	}

	[Fact]
	public void ValidateField_Url_AcceptsIpAddressWithDots()
	{
		var validator = CreateValidator();

		var result = validator.ValidateField(UrlKey, "http://127.0.0.1");

		Assert.True(result.IsValid);
	}

	[Theory]
	[InlineData("https://example.com")]
	[InlineData("http://sub.example.com/path")]
	[InlineData("https://example.com:8080/profile?id=1")]
	public void ValidateField_Url_AcceptsValidHttpAndHttpsUrls(string url)
	{
		var validator = CreateValidator();

		var result = validator.ValidateField(UrlKey, url);

		Assert.True(result.IsValid);
	}

	[Theory]
	[InlineData("example.com")]
	[InlineData("/relative/path")]
	[InlineData("ftp://example.com")]
	[InlineData("file:///tmp/example")]
	[InlineData("javascript:alert(1)")]
	[InlineData("https://localhost")]
	public void ValidateField_Url_RejectsInvalidUrls(string url)
	{
		var validator = CreateValidator();

		var result = validator.ValidateField(UrlKey, url);

		Assert.False(result.IsValid);
		Assert.Contains(result.Errors, error => error.Message == "Invalid URL.");
	}

	[Fact]
	public void Validate_ReturnsValidWhenAllFieldsPass()
	{
		var validator = CreateValidator();

		var result = validator.Validate(new Dictionary<string, string?>
		{
			[RequiredTextKey] = "Jane",
			[EmailKey] = "jane@example.com",
			[TextKey] = "note",
			[UrlKey] = "https://example.com/jane"
		});

		Assert.True(result.IsValid);
		Assert.Empty(result.Errors);
	}

	[Fact]
	public void Validate_ValidatesEverySchemaFieldWhenDictionaryIsEmpty()
	{
		var validator = CreateValidator();

		var result = validator.Validate(new Dictionary<string, string?>());

		Assert.False(result.IsValid);
		Assert.Contains(result.Errors, error => error.FieldKey == RequiredTextKey);
		Assert.Contains(result.Errors, error => error.FieldKey == EmailKey);
		Assert.DoesNotContain(result.Errors, error => error.FieldKey == TextKey);
		Assert.DoesNotContain(result.Errors, error => error.FieldKey == UrlKey);
	}

	[Fact]
	public void Validate_IgnoresExtraDictionaryKeys()
	{
		var validator = CreateValidator();

		var result = validator.Validate(new Dictionary<string, string?>
		{
			[RequiredTextKey] = "Jane",
			[EmailKey] = "jane@example.com",
			["unexpected"] = "ignored"
		});

		Assert.True(result.IsValid);
	}

	[Fact]
	public void Validate_TreatsMissingDictionaryValuesAsEmpty()
	{
		var validator = CreateValidator();

		var result = validator.Validate(new Dictionary<string, string?>
		{
			[RequiredTextKey] = "Jane"
		});

		Assert.False(result.IsValid);
		Assert.Contains(result.Errors, error => error.FieldKey == EmailKey);
	}

	[Fact]
	public void FieldValidationResult_IsValidReflectsErrorCount()
	{
		var validResult = new FieldValidationResult(Array.Empty<FieldValidationError>());
		var invalidResult = new FieldValidationResult(
		[
			new FieldValidationError("field", "message")
		]);

		Assert.True(validResult.IsValid);
		Assert.False(invalidResult.IsValid);
		Assert.Single(invalidResult.Errors);
	}
}
