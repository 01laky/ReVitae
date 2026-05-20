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
