using ReVitae.Core.Cv;
using ReVitae.Core.Validation;

namespace ReVitae.Tests;

public sealed class MainPersonalInformationSchemaTests
{
    private static readonly FieldValidator Validator = MainPersonalInformationSchema.CreateValidator();

    public static TheoryData<string> RequiredFieldKeys()
    {
        return new TheoryData<string>
        {
            MainPersonalInformationFieldKeys.FirstName,
            MainPersonalInformationFieldKeys.LastName,
            MainPersonalInformationFieldKeys.Email
        };
    }

    public static TheoryData<string> OptionalFieldKeys()
    {
        return new TheoryData<string>
        {
            MainPersonalInformationFieldKeys.ProfessionalTitle,
            MainPersonalInformationFieldKeys.Phone,
            MainPersonalInformationFieldKeys.Location,
            MainPersonalInformationFieldKeys.LinkedInUrl,
            MainPersonalInformationFieldKeys.PortfolioUrl,
            MainPersonalInformationFieldKeys.GitHubUrl,
            MainPersonalInformationFieldKeys.ShortSummary
        };
    }

    public static TheoryData<string, int> MaximumLengthCases()
    {
        return new TheoryData<string, int>
        {
            { MainPersonalInformationFieldKeys.FirstName, 80 },
            { MainPersonalInformationFieldKeys.LastName, 80 },
            { MainPersonalInformationFieldKeys.ProfessionalTitle, 120 },
            { MainPersonalInformationFieldKeys.Email, 160 },
            { MainPersonalInformationFieldKeys.Phone, 40 },
            { MainPersonalInformationFieldKeys.Location, 120 },
            { MainPersonalInformationFieldKeys.LinkedInUrl, 240 },
            { MainPersonalInformationFieldKeys.PortfolioUrl, 240 },
            { MainPersonalInformationFieldKeys.GitHubUrl, 240 },
            { MainPersonalInformationFieldKeys.ShortSummary, 800 }
        };
    }

    public static TheoryData<string> UrlFieldKeys()
    {
        return new TheoryData<string>
        {
            MainPersonalInformationFieldKeys.LinkedInUrl,
            MainPersonalInformationFieldKeys.PortfolioUrl,
            MainPersonalInformationFieldKeys.GitHubUrl
        };
    }

    [Fact]
    public void Schema_CoversEveryCurrentMainField()
    {
        var expectedKeys = new[]
        {
            MainPersonalInformationFieldKeys.FirstName,
            MainPersonalInformationFieldKeys.LastName,
            MainPersonalInformationFieldKeys.ProfessionalTitle,
            MainPersonalInformationFieldKeys.Email,
            MainPersonalInformationFieldKeys.Phone,
            MainPersonalInformationFieldKeys.Location,
            MainPersonalInformationFieldKeys.LinkedInUrl,
            MainPersonalInformationFieldKeys.PortfolioUrl,
            MainPersonalInformationFieldKeys.GitHubUrl,
            MainPersonalInformationFieldKeys.ShortSummary
        };

        var actualKeys = MainPersonalInformationSchema.Fields
            .Select(field => field.Key)
            .ToArray();

        Assert.Equal(expectedKeys.Order(), actualKeys.Order());
        Assert.Equal(expectedKeys.Length, actualKeys.Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(RequiredFieldKeys))]
    public void RequiredFields_RejectEmptyValues(string fieldKey)
    {
        var result = Validator.ValidateField(fieldKey, string.Empty);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.FieldKey == fieldKey);
    }

    [Theory]
    [MemberData(nameof(RequiredFieldKeys))]
    public void RequiredFields_RejectWhitespaceOnlyValues(string fieldKey)
    {
        var result = Validator.ValidateField(fieldKey, "   ");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.FieldKey == fieldKey);
    }

    [Theory]
    [MemberData(nameof(OptionalFieldKeys))]
    public void OptionalFields_AcceptEmptyValues(string fieldKey)
    {
        var result = Validator.ValidateField(fieldKey, string.Empty);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [MemberData(nameof(OptionalFieldKeys))]
    public void OptionalFields_AcceptWhitespaceOnlyValues(string fieldKey)
    {
        var result = Validator.ValidateField(fieldKey, "   ");

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [MemberData(nameof(MaximumLengthCases))]
    public void Fields_AcceptValuesExactlyAtMaximumLength(string fieldKey, int maximumLength)
    {
        var value = BuildValidValueAtLength(fieldKey, maximumLength);

        var result = Validator.ValidateField(fieldKey, value);

        Assert.True(result.IsValid);
    }

    [Theory]
    [MemberData(nameof(MaximumLengthCases))]
    public void Fields_RejectValuesOverMaximumLength(string fieldKey, int maximumLength)
    {
        var value = BuildValidValueAtLength(fieldKey, maximumLength + 1);

        var result = Validator.ValidateField(fieldKey, value);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.FieldKey == fieldKey);
    }

    [Theory]
    [InlineData("john.doe@example.com")]
    [InlineData("john+cv@example.co.uk")]
    public void Email_AcceptsValidValues(string email)
    {
        var result = Validator.ValidateField(MainPersonalInformationFieldKeys.Email, email);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("john")]
    [InlineData("john@example")]
    [InlineData("john@@example.com")]
    [InlineData("john example@example.com")]
    public void Email_RejectsInvalidValues(string email)
    {
        var result = Validator.ValidateField(MainPersonalInformationFieldKeys.Email, email);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.FieldKey == MainPersonalInformationFieldKeys.Email);
    }

    [Theory]
    [MemberData(nameof(UrlFieldKeys))]
    public void UrlFields_AcceptValidHttpAndHttpsValues(string fieldKey)
    {
        var httpResult = Validator.ValidateField(fieldKey, "http://example.com/profile");
        var httpsResult = Validator.ValidateField(fieldKey, "https://example.com/profile");

        Assert.True(httpResult.IsValid);
        Assert.True(httpsResult.IsValid);
    }

    [Theory]
    [MemberData(nameof(UrlFieldKeys))]
    public void UrlFields_RejectInvalidValues(string fieldKey)
    {
        var missingSchemeResult = Validator.ValidateField(fieldKey, "example.com/profile");
        var ftpResult = Validator.ValidateField(fieldKey, "ftp://example.com/profile");
        var missingDotResult = Validator.ValidateField(fieldKey, "https://localhost/profile");

        Assert.False(missingSchemeResult.IsValid);
        Assert.False(ftpResult.IsValid);
        Assert.False(missingDotResult.IsValid);
    }

    [Fact]
    public void Validate_ReportsAllInvalidFields()
    {
        var result = Validator.Validate(
            new Dictionary<string, string?>
            {
                [MainPersonalInformationFieldKeys.FirstName] = string.Empty,
                [MainPersonalInformationFieldKeys.LastName] = "Doe",
                [MainPersonalInformationFieldKeys.Email] = "invalid-email",
                [MainPersonalInformationFieldKeys.LinkedInUrl] = "linkedin.com/in/johndoe",
                [MainPersonalInformationFieldKeys.ShortSummary] = new string('a', 801)
            });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.FieldKey == MainPersonalInformationFieldKeys.FirstName);
        Assert.Contains(result.Errors, error => error.FieldKey == MainPersonalInformationFieldKeys.Email);
        Assert.Contains(result.Errors, error => error.FieldKey == MainPersonalInformationFieldKeys.LinkedInUrl);
        Assert.Contains(result.Errors, error => error.FieldKey == MainPersonalInformationFieldKeys.ShortSummary);
    }

    [Fact]
    public void Validate_AcceptsFullyValidPersonalInformation()
    {
        var result = Validator.Validate(CreateValidValues());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_TreatsNullDictionaryValuesAsEmpty()
    {
        var values = CreateValidValues();
        values[MainPersonalInformationFieldKeys.FirstName] = null;

        var result = Validator.Validate(values);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.FieldKey == MainPersonalInformationFieldKeys.FirstName);
    }

    [Theory]
    [MemberData(nameof(RequiredFieldKeys))]
    public void RequiredFields_AcceptValuesWithSurroundingWhitespace(string fieldKey)
    {
        var value = fieldKey switch
        {
            MainPersonalInformationFieldKeys.Email => "  john.doe@example.com  ",
            _ => "  valid-value  "
        };

        var result = Validator.ValidateField(fieldKey, value);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(MainPersonalInformationFieldKeys.FirstName, true, FieldFormat.Text)]
    [InlineData(MainPersonalInformationFieldKeys.LastName, true, FieldFormat.Text)]
    [InlineData(MainPersonalInformationFieldKeys.ProfessionalTitle, false, FieldFormat.Text)]
    [InlineData(MainPersonalInformationFieldKeys.Email, true, FieldFormat.Email)]
    [InlineData(MainPersonalInformationFieldKeys.Phone, false, FieldFormat.Text)]
    [InlineData(MainPersonalInformationFieldKeys.Location, false, FieldFormat.Text)]
    [InlineData(MainPersonalInformationFieldKeys.LinkedInUrl, false, FieldFormat.Url)]
    [InlineData(MainPersonalInformationFieldKeys.PortfolioUrl, false, FieldFormat.Url)]
    [InlineData(MainPersonalInformationFieldKeys.GitHubUrl, false, FieldFormat.Url)]
    [InlineData(MainPersonalInformationFieldKeys.ShortSummary, false, FieldFormat.Text)]
    public void SchemaFields_HaveExpectedRequiredAndFormatMetadata(string fieldKey, bool isRequired, FieldFormat format)
    {
        var field = MainPersonalInformationSchema.Fields.Single(schema => schema.Key == fieldKey);

        Assert.Equal(isRequired, field.IsRequired);
        Assert.Equal(format, field.Format);
    }

    [Fact]
    public void SchemaFields_UseTranslationKeysForValidationMessages()
    {
        foreach (var field in MainPersonalInformationSchema.Fields)
        {
            if (field.IsRequired)
            {
                Assert.StartsWith("validation.", field.RequiredMessage);
            }

            Assert.StartsWith("validation.", field.MaximumLengthMessage);

            if (field.Format is FieldFormat.Email or FieldFormat.Url)
            {
                Assert.NotNull(field.FormatMessage);
                Assert.StartsWith("validation.", field.FormatMessage);
            }
        }
    }

    [Theory]
    [InlineData("john.doe@EXAMPLE.COM")]
    [InlineData("john.doe+jobs@mail.example.org")]
    public void Email_AcceptsAdditionalValidFormats(string email)
    {
        var result = Validator.ValidateField(MainPersonalInformationFieldKeys.Email, email);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("@example.com")]
    [InlineData("john@")]
    [InlineData("john@example")]
    public void Email_RejectsAdditionalInvalidFormats(string email)
    {
        var result = Validator.ValidateField(MainPersonalInformationFieldKeys.Email, email);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.FieldKey == MainPersonalInformationFieldKeys.Email);
    }

    [Fact]
    public void Email_ReportsBothMaxLengthAndFormatErrorsWhenBothFail()
    {
        var invalidLongEmail = new string('a', 157) + "@bad";

        var result = Validator.ValidateField(MainPersonalInformationFieldKeys.Email, invalidLongEmail);

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
        Assert.All(result.Errors, error => Assert.Equal(MainPersonalInformationFieldKeys.Email, error.FieldKey));
    }

    [Theory]
    [MemberData(nameof(UrlFieldKeys))]
    public void UrlFields_AcceptSubdomainsPathsQueryStringsAndPorts(string fieldKey)
    {
        var result = Validator.ValidateField(fieldKey, "https://profile.example.com:8443/cv?id=1");

        Assert.True(result.IsValid);
    }

    [Theory]
    [MemberData(nameof(UrlFieldKeys))]
    public void UrlFields_RejectRelativeAndUnsafeSchemes(string fieldKey)
    {
        var relativeResult = Validator.ValidateField(fieldKey, "/profile/john");
        var javascriptResult = Validator.ValidateField(fieldKey, "javascript:alert(1)");
        var fileResult = Validator.ValidateField(fieldKey, "file:///tmp/cv.pdf");

        Assert.False(relativeResult.IsValid);
        Assert.False(javascriptResult.IsValid);
        Assert.False(fileResult.IsValid);
    }

    [Fact]
    public void ValidateField_ThrowsForUnknownFieldKey()
    {
        var exception = Assert.Throws<ArgumentException>(() => Validator.ValidateField("unknown.field", "value"));

        Assert.Equal("fieldKey", exception.ParamName);
    }

    private static Dictionary<string, string?> CreateValidValues()
    {
        return new Dictionary<string, string?>
        {
            [MainPersonalInformationFieldKeys.FirstName] = "John",
            [MainPersonalInformationFieldKeys.LastName] = "Doe",
            [MainPersonalInformationFieldKeys.ProfessionalTitle] = "Software Engineer",
            [MainPersonalInformationFieldKeys.Email] = "john.doe@example.com",
            [MainPersonalInformationFieldKeys.Phone] = "+421 900 000 000",
            [MainPersonalInformationFieldKeys.Location] = "Bratislava, Slovakia",
            [MainPersonalInformationFieldKeys.LinkedInUrl] = "https://linkedin.com/in/johndoe",
            [MainPersonalInformationFieldKeys.PortfolioUrl] = "https://example.com",
            [MainPersonalInformationFieldKeys.GitHubUrl] = "https://github.com/johndoe",
            [MainPersonalInformationFieldKeys.ShortSummary] = "Experienced engineer building cross-platform desktop apps."
        };
    }

    private static string BuildValidValueAtLength(string fieldKey, int length)
    {
        return fieldKey switch
        {
            MainPersonalInformationFieldKeys.Email => BuildEmailAtLength(length),
            MainPersonalInformationFieldKeys.LinkedInUrl
                or MainPersonalInformationFieldKeys.PortfolioUrl
                or MainPersonalInformationFieldKeys.GitHubUrl => BuildUrlAtLength(length),
            _ => new string('a', length)
        };
    }

    private static string BuildEmailAtLength(int length)
    {
        const string suffix = "@example.com";
        return new string('a', length - suffix.Length) + suffix;
    }

    private static string BuildUrlAtLength(int length)
    {
        const string prefix = "https://example.com/";
        return prefix + new string('a', length - prefix.Length);
    }
}
