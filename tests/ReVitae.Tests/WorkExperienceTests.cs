using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Localization;
using ReVitae.Core.Validation;

namespace ReVitae.Tests;

public sealed class WorkExperienceTests
{
    private static readonly WorkExperienceCollectionValidator Validator = new();

    private static WorkExperienceEntry CreateValidEntry()
    {
        return new WorkExperienceEntry
        {
            JobTitle = "Software Engineer",
            Company = "Acme Corp",
            Location = "Bratislava",
            EmploymentType = EmploymentType.FullTime,
            StartMonth = 3,
            StartYear = 2021,
            EndMonth = 8,
            EndYear = 2024,
            Description = "Built desktop apps.",
            Achievements = "Reduced build time by 40%.",
            Technologies = "C#, Avalonia",
            CompanyUrl = "https://acme.example.com"
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
        var result = Validator.Validate(Array.Empty<WorkExperienceEntry>());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void HasUserInput_TreatsBlankEntryAsDraft()
    {
        var entry = new WorkExperienceEntry();

        Assert.False(entry.HasUserInput());
    }

    [Fact]
    public void Validate_IgnoresDraftEntryWithoutUserInput()
    {
        var result = Validator.Validate([new WorkExperienceEntry()]);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ActivatesEntryAfterFirstFieldInput()
    {
        var entry = new WorkExperienceEntry { JobTitle = "Engineer" };

        var result = Validator.Validate([entry]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.FieldKey.EndsWith(".company", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ReportsRequiredFieldFailuresForActiveEntry()
    {
        var entry = new WorkExperienceEntry { Company = "Acme Corp" };

        var result = Validator.Validate([entry]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.FieldKey.EndsWith(".jobTitle", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("jobTitle", 121, TranslationKeys.ValidationWorkExperienceJobTitleMax)]
    [InlineData("company", 161, TranslationKeys.ValidationWorkExperienceCompanyMax)]
    [InlineData("description", 2001, TranslationKeys.ValidationWorkExperienceDescriptionMax)]
    [InlineData("achievements", 2001, TranslationKeys.ValidationWorkExperienceAchievementsMax)]
    [InlineData("technologies", 501, TranslationKeys.ValidationWorkExperienceTechnologiesMax)]
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
    public void Validate_AcceptsValidCompanyUrl()
    {
        var entry = CreateValidEntry();
        entry.CompanyUrl = "https://example.com";

        var result = Validator.Validate([entry]);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsInvalidCompanyUrl()
    {
        var entry = CreateValidEntry();
        entry.CompanyUrl = "example.com";

        var result = Validator.Validate([entry]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationWorkExperienceCompanyUrlFormat);
    }

    [Fact]
    public void Validate_RequiresEndDateWhenNotCurrentlyWorking()
    {
        var entry = CreateValidEntry();
        entry.EndMonth = null;
        entry.EndYear = null;

        var result = Validator.Validate([entry]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationWorkExperienceEndMonthRequired);
        Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationWorkExperienceEndYearRequired);
    }

    [Fact]
    public void Validate_AllowsMissingEndDateWhenCurrentlyWorking()
    {
        var entry = CreateValidEntry();
        entry.IsCurrentlyWorking = true;
        entry.EndMonth = null;
        entry.EndYear = null;

        var result = Validator.Validate([entry]);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsStartDateAfterEndDate()
    {
        var entry = CreateValidEntry();
        entry.StartMonth = 12;
        entry.StartYear = 2024;
        entry.EndMonth = 1;
        entry.EndYear = 2024;

        var result = Validator.Validate([entry]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationWorkExperienceStartAfterEnd);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Validate_RejectsInvalidStartMonth(int month)
    {
        var entry = CreateValidEntry();
        entry.StartMonth = month;

        var result = Validator.Validate([entry]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationWorkExperienceStartMonthInvalid);
    }

    [Theory]
    [InlineData(1949)]
    [InlineData(2101)]
    public void Validate_RejectsInvalidStartYear(int year)
    {
        var entry = CreateValidEntry();
        entry.StartYear = year;

        var result = Validator.Validate([entry]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationWorkExperienceStartYearInvalid);
    }

    [Fact]
    public void Validate_ValidatesMultipleActiveEntriesTogether()
    {
        var first = CreateValidEntry();
        var second = CreateValidEntry();
        second.JobTitle = string.Empty;

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
        Assert.Equal(source.JobTitle, duplicate.JobTitle);
        Assert.Equal(source.Company, duplicate.Company);
        Assert.Equal(source.Location, duplicate.Location);
        Assert.Equal(source.EmploymentType, duplicate.EmploymentType);
        Assert.Equal(source.StartMonth, duplicate.StartMonth);
        Assert.Equal(source.StartYear, duplicate.StartYear);
        Assert.Equal(source.EndMonth, duplicate.EndMonth);
        Assert.Equal(source.EndYear, duplicate.EndYear);
        Assert.Equal(source.IsCurrentlyWorking, duplicate.IsCurrentlyWorking);
        Assert.Equal(source.Description, duplicate.Description);
        Assert.Equal(source.Achievements, duplicate.Achievements);
        Assert.Equal(source.Technologies, duplicate.Technologies);
        Assert.Equal(source.CompanyUrl, duplicate.CompanyUrl);
        Assert.True(duplicate.HasUserInput());
    }

    [Fact]
    public void SortByDateNewestFirst_PlacesCurrentRoleFirst()
    {
        var older = CreateValidEntry();
        older.StartMonth = 1;
        older.StartYear = 2020;
        older.EndMonth = 12;
        older.EndYear = 2022;

        var current = CreateValidEntry();
        current.JobTitle = "Current Role";
        current.IsCurrentlyWorking = true;
        current.EndMonth = null;
        current.EndYear = null;

        var sorted = WorkExperienceSorter.SortByDateNewestFirst([older, current]);

        Assert.Equal(current.Id, sorted[0].Id);
    }

    [Fact]
    public void SortByDateNewestFirst_KeepsDraftEntriesAtBottom()
    {
        var active = CreateValidEntry();
        var draft = new WorkExperienceEntry();

        var sorted = WorkExperienceSorter.SortByDateNewestFirst([draft, active]);

        Assert.Equal(active.Id, sorted[0].Id);
        Assert.Equal(draft.Id, sorted[1].Id);
    }

    [Fact]
    public void SchemaFields_UseTranslationKeysForValidationMessages()
    {
        foreach (var field in WorkExperienceSchema.EntryFields)
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
        entry.JobTitle = "   ";

        var result = Validator.Validate([entry]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationWorkExperienceJobTitleRequired);
    }

    private static void SetField(WorkExperienceEntry entry, string fieldName, string value)
    {
        switch (fieldName)
        {
            case "jobTitle": entry.JobTitle = value; break;
            case "company": entry.Company = value; break;
            case "description": entry.Description = value; break;
            case "achievements": entry.Achievements = value; break;
            case "technologies": entry.Technologies = value; break;
            default: throw new ArgumentOutOfRangeException(nameof(fieldName));
        }
    }
}
