using ReVitae.Core.Cv.Certificates;
using ReVitae.Core.Localization;
using ReVitae.Core.Validation;

namespace ReVitae.Tests;

public sealed class CertificatesTests
{
    private static readonly CertificatesCollectionValidator Validator = new();

    private static CertificateEntry CreateValidEntry()
    {
        return new CertificateEntry
        {
            Name = "AWS Certified Solutions Architect – Associate",
            Issuer = "Amazon Web Services",
            IssueMonth = 3,
            IssueYear = 2024,
            ExpirationMonth = 3,
            ExpirationYear = 2027,
            CredentialId = "ABC123456789",
            CredentialUrl = "https://www.credly.com/badges/example",
            Description = "Score: 920 / 1000"
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
        var result = Validator.Validate(Array.Empty<CertificateEntry>());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void HasUserInput_TreatsBlankEntryAsDraft()
    {
        Assert.False(new CertificateEntry().HasUserInput());
    }

    [Fact]
    public void Validate_IgnoresDraftEntryWithoutUserInput()
    {
        var result = Validator.Validate([new CertificateEntry()]);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ActivatesEntryAfterFirstFieldInput()
    {
        var entry = new CertificateEntry { Name = "Certified Scrum Master" };

        var result = Validator.Validate([entry]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationCertificatesIssuerRequired);
    }

    [Fact]
    public void Validate_ReportsRequiredFieldFailuresForActiveEntry()
    {
        var entry = new CertificateEntry { Issuer = "Microsoft" };

        var result = Validator.Validate([entry]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationCertificatesNameRequired);
    }

    [Theory]
    [InlineData("name", 161, TranslationKeys.ValidationCertificatesNameMax)]
    [InlineData("issuer", 161, TranslationKeys.ValidationCertificatesIssuerMax)]
    [InlineData("credentialId", 81, TranslationKeys.ValidationCertificatesCredentialIdMax)]
    [InlineData("description", 501, TranslationKeys.ValidationCertificatesDescriptionMax)]
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
    public void Validate_AcceptsValidCredentialUrl()
    {
        var entry = CreateValidEntry();
        entry.CredentialUrl = "https://example.com/badge";

        var result = Validator.Validate([entry]);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsInvalidCredentialUrl()
    {
        var entry = CreateValidEntry();
        entry.CredentialUrl = "example.com";

        var result = Validator.Validate([entry]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationCertificatesCredentialUrlFormat);
    }

    [Fact]
    public void Validate_RejectsIssueDateAfterExpirationDate()
    {
        var entry = CreateValidEntry();
        entry.IssueMonth = 12;
        entry.IssueYear = 2024;
        entry.ExpirationMonth = 1;
        entry.ExpirationYear = 2024;

        var result = Validator.Validate([entry]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationCertificatesIssueAfterExpiration);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Validate_RejectsInvalidIssueMonth(int month)
    {
        var entry = CreateValidEntry();
        entry.IssueMonth = month;

        var result = Validator.Validate([entry]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationCertificatesIssueMonthInvalid);
    }

    [Theory]
    [InlineData(1949)]
    [InlineData(2101)]
    public void Validate_RejectsInvalidIssueYear(int year)
    {
        var entry = CreateValidEntry();
        entry.IssueYear = year;

        var result = Validator.Validate([entry]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationCertificatesIssueYearInvalid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Validate_RejectsInvalidExpirationMonth(int month)
    {
        var entry = CreateValidEntry();
        entry.ExpirationMonth = month;

        var result = Validator.Validate([entry]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationCertificatesExpirationMonthInvalid);
    }

    [Theory]
    [InlineData(1949)]
    [InlineData(2101)]
    public void Validate_RejectsInvalidExpirationYear(int year)
    {
        var entry = CreateValidEntry();
        entry.ExpirationYear = year;

        var result = Validator.Validate([entry]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationCertificatesExpirationYearInvalid);
    }

    [Fact]
    public void Validate_ValidatesMultipleActiveEntriesTogether()
    {
        var first = CreateValidEntry();
        var second = CreateValidEntry();
        second.Name = string.Empty;

        var result = Validator.Validate([first, second]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.FieldKey.Contains(second.Id, StringComparison.Ordinal));
    }

    [Fact]
    public void FieldKeys_AreUniqueAcrossEntries()
    {
        var first = CreateValidEntry();
        var second = CreateValidEntry();

        var firstKeys = first.ToFieldValues().Keys.ToArray();
        var secondKeys = second.ToFieldValues().Keys.ToArray();

        Assert.Empty(firstKeys.Intersect(secondKeys));
    }

    [Fact]
    public void Duplicate_CopiesAllFieldValuesWithNewIdentity()
    {
        var source = CreateValidEntry();
        var duplicate = source.Duplicate();

        Assert.NotEqual(source.Id, duplicate.Id);
        Assert.Equal(source.Name, duplicate.Name);
        Assert.Equal(source.Issuer, duplicate.Issuer);
        Assert.Equal(source.IssueMonth, duplicate.IssueMonth);
        Assert.Equal(source.IssueYear, duplicate.IssueYear);
        Assert.Equal(source.ExpirationMonth, duplicate.ExpirationMonth);
        Assert.Equal(source.ExpirationYear, duplicate.ExpirationYear);
        Assert.Equal(source.CredentialId, duplicate.CredentialId);
        Assert.Equal(source.CredentialUrl, duplicate.CredentialUrl);
        Assert.Equal(source.Description, duplicate.Description);
        Assert.True(duplicate.HasUserInput());
    }

    [Fact]
    public void SortByDateNewestFirst_OrdersByIssueDateDescending()
    {
        var older = CreateValidEntry();
        older.IssueMonth = 1;
        older.IssueYear = 2020;

        var newer = CreateValidEntry();
        newer.Name = "Azure Administrator Associate";
        newer.IssueMonth = 6;
        newer.IssueYear = 2024;

        var sorted = CertificateSorter.SortByDateNewestFirst([older, newer]);

        Assert.Equal(newer.Id, sorted[0].Id);
    }

    [Fact]
    public void SortByDateNewestFirst_PreservesOrderWhenIssueDatesTie()
    {
        var first = CreateValidEntry();
        first.IssueMonth = 3;
        first.IssueYear = 2024;

        var second = CreateValidEntry();
        second.Name = "Second Certificate";
        second.IssueMonth = 3;
        second.IssueYear = 2024;

        var sorted = CertificateSorter.SortByDateNewestFirst([first, second]);

        Assert.Equal(first.Id, sorted[0].Id);
        Assert.Equal(second.Id, sorted[1].Id);
    }

    [Fact]
    public void SortByDateNewestFirst_KeepsDraftEntriesAtBottom()
    {
        var active = CreateValidEntry();
        var draft = new CertificateEntry();

        var sorted = CertificateSorter.SortByDateNewestFirst([draft, active]);

        Assert.Equal(active.Id, sorted[0].Id);
        Assert.Equal(draft.Id, sorted[1].Id);
    }

    [Fact]
    public void IssuerSuggestions_FilterMatchesCaseInsensitively()
    {
        var matches = IssuerSuggestions.Filter("amazon");

        Assert.Contains("Amazon Web Services", matches);
    }

    [Fact]
    public void PreviewFormatter_IncludesExpirationAndDetailLines()
    {
        var entry = CreateValidEntry();
        var localizer = new AppLocalizer(AppLocalizer.FallbackLanguageCode);

        var mainLine = CertificatePreviewFormatter.FormatMainLine(entry, localizer);
        var detailLines = CertificatePreviewFormatter.FormatDetailLines(entry, localizer);

        Assert.Contains("Valid until", mainLine);
        Assert.Equal(3, detailLines.Count);
    }

    [Fact]
    public void PreviewFormatter_OmitsExpirationWhenNotSet()
    {
        var entry = CreateValidEntry();
        entry.ExpirationMonth = null;
        entry.ExpirationYear = null;
        var localizer = new AppLocalizer(AppLocalizer.FallbackLanguageCode);

        var mainLine = CertificatePreviewFormatter.FormatMainLine(entry, localizer);

        Assert.DoesNotContain("Valid until", mainLine);
    }

    [Fact]
    public void SchemaFields_UseTranslationKeysForValidationMessages()
    {
        foreach (var field in CertificatesSchema.EntryFields)
        {
            if (field.IsRequired)
            {
                Assert.StartsWith("validation.", field.RequiredMessage);
            }

            Assert.StartsWith("validation.", field.MaximumLengthMessage);

            if (field.FormatMessage is not null)
            {
                Assert.StartsWith("validation.", field.FormatMessage);
            }
        }
    }

    [Fact]
    public void Validate_RejectsWhitespaceOnlyRequiredValuesInsideActiveEntry()
    {
        var entry = CreateValidEntry();
        entry.Name = "   ";

        var result = Validator.Validate([entry]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationCertificatesNameRequired);
    }

    [Fact]
    public void BuildHeaderSummary_IncludesNameIssuerAndIssueDate()
    {
        var entry = CreateValidEntry();

        var summary = entry.BuildHeaderSummary();

        Assert.Contains(entry.Name, summary);
        Assert.Contains(entry.Issuer, summary);
        Assert.Contains("03 / 2024", summary);
    }

    private static void SetField(CertificateEntry entry, string fieldName, string value)
    {
        switch (fieldName)
        {
            case "name": entry.Name = value; break;
            case "issuer": entry.Issuer = value; break;
            case "credentialId": entry.CredentialId = value; break;
            case "description": entry.Description = value; break;
            default: throw new ArgumentOutOfRangeException(nameof(fieldName));
        }
    }
}
