using ReVitae.Core.Cv.AdditionalInformation;
using ReVitae.Core.Localization;
using ReVitae.Core.Validation;

namespace ReVitae.Tests;

public sealed class AdditionalInformationTests
{
    private static readonly AdditionalInformationValidator Validator = new();

    [Fact]
    public void Validate_AcceptsEmptyContent()
    {
        var result = Validator.Validate(new AdditionalInformationContent());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_AcceptsContentExactlyAtMaximumLength()
    {
        var content = new AdditionalInformationContent
        {
            Content = new string('a', AdditionalInformationSchema.ContentMaxLength)
        };

        var result = Validator.Validate(content);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsContentOverMaximumLength()
    {
        var content = new AdditionalInformationContent
        {
            Content = new string('a', AdditionalInformationSchema.ContentMaxLength + 1)
        };

        var result = Validator.Validate(content);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.FieldKey == AdditionalInformationFieldKeys.Content
            && error.Message == TranslationKeys.ValidationAdditionalInformationContentMax);
    }

    [Fact]
    public void HasUserInput_ReturnsFalseForWhitespaceOnlyContent()
    {
        var content = new AdditionalInformationContent { Content = "   \n  " };

        Assert.False(content.HasUserInput());
    }

    [Fact]
    public void HasUserInput_ReturnsTrueWhenContentHasText()
    {
        var content = new AdditionalInformationContent { Content = "Volunteering at local shelter." };

        Assert.True(content.HasUserInput());
    }

    [Fact]
    public void SchemaFields_UseTranslationKeysForValidationMessages()
    {
        foreach (var field in AdditionalInformationSchema.Fields)
        {
            if (!string.IsNullOrEmpty(field.MaximumLengthMessage))
            {
                Assert.StartsWith("validation.", field.MaximumLengthMessage, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Validate_IgnoresWhitespaceOnlyContent()
    {
        var content = new AdditionalInformationContent { Content = "   " };

        var result = Validator.Validate(content);

        Assert.True(result.IsValid);
    }
}
