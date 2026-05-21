using ReVitae.Core.Cv.Education;
using ReVitae.Core.Localization;
using ReVitae.Core.Validation;

namespace ReVitae.Tests;

public sealed class EducationTests
{
    private static readonly EducationCollectionValidator Validator = new();

    private static EducationEntry CreateValidEntry()
    {
        return new EducationEntry
        {
            Institution = "Comenius University",
            Degree = "Bachelor of Science",
            FieldOfStudy = "Computer Science",
            Location = "Bratislava",
            DegreeType = DegreeType.Bachelor,
            StartMonth = 9,
            StartYear = 2018,
            EndMonth = 6,
            EndYear = 2021,
            Grade = "A",
            Description = "Focused on software engineering.",
            InstitutionUrl = "https://uniba.sk"
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
        var result = Validator.Validate(Array.Empty<EducationEntry>());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void HasUserInput_TreatsBlankEntryAsDraft()
    {
        var entry = new EducationEntry();

        Assert.False(entry.HasUserInput());
    }

    [Fact]
    public void Validate_IgnoresDraftEntryWithoutUserInput()
    {
        var result = Validator.Validate([new EducationEntry()]);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ActivatesEntryAfterFirstFieldInput()
    {
        var entry = new EducationEntry { Degree = "Bachelor" };

        var result = Validator.Validate([entry]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.FieldKey.EndsWith(".institution", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ReportsRequiredFieldFailuresForActiveEntry()
    {
        var entry = new EducationEntry { Institution = "University" };

        var result = Validator.Validate([entry]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.FieldKey.EndsWith(".degree", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("institution", 161, TranslationKeys.ValidationEducationInstitutionMax)]
    [InlineData("degree", 161, TranslationKeys.ValidationEducationDegreeMax)]
    [InlineData("fieldOfStudy", 161, TranslationKeys.ValidationEducationFieldOfStudyMax)]
    [InlineData("location", 121, TranslationKeys.ValidationEducationLocationMax)]
    [InlineData("description", 2001, TranslationKeys.ValidationEducationDescriptionMax)]
    [InlineData("grade", 81, TranslationKeys.ValidationEducationGradeMax)]
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
    public void Validate_AcceptsValidInstitutionUrl()
    {
        var entry = CreateValidEntry();
        entry.InstitutionUrl = "https://example.com";

        var result = Validator.Validate([entry]);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsInvalidInstitutionUrl()
    {
        var entry = CreateValidEntry();
        entry.InstitutionUrl = "example.com";

        var result = Validator.Validate([entry]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationEducationInstitutionUrlFormat);
    }

    [Fact]
    public void Validate_RequiresEndDateWhenNotCurrentlyStudying()
    {
        var entry = CreateValidEntry();
        entry.EndMonth = null;
        entry.EndYear = null;

        var result = Validator.Validate([entry]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationEducationEndMonthRequired);
        Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationEducationEndYearRequired);
    }

    [Fact]
    public void Validate_AllowsMissingEndDateWhenCurrentlyStudying()
    {
        var entry = CreateValidEntry();
        entry.IsCurrentlyStudying = true;
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
        Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationEducationStartAfterEnd);
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
        Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationEducationStartMonthInvalid);
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
        Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationEducationStartYearInvalid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Validate_RejectsInvalidEndMonth(int month)
    {
        var entry = CreateValidEntry();
        entry.EndMonth = month;

        var result = Validator.Validate([entry]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationEducationEndMonthInvalid);
    }

    [Theory]
    [InlineData(1949)]
    [InlineData(2101)]
    public void Validate_RejectsInvalidEndYear(int year)
    {
        var entry = CreateValidEntry();
        entry.EndYear = year;

        var result = Validator.Validate([entry]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationEducationEndYearInvalid);
    }

    [Fact]
    public void Validate_ValidatesMultipleActiveEntriesTogether()
    {
        var first = CreateValidEntry();
        var second = CreateValidEntry();
        second.Degree = string.Empty;

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
        Assert.Equal(source.Institution, duplicate.Institution);
        Assert.Equal(source.Degree, duplicate.Degree);
        Assert.Equal(source.FieldOfStudy, duplicate.FieldOfStudy);
        Assert.Equal(source.Location, duplicate.Location);
        Assert.Equal(source.DegreeType, duplicate.DegreeType);
        Assert.Equal(source.StartMonth, duplicate.StartMonth);
        Assert.Equal(source.StartYear, duplicate.StartYear);
        Assert.Equal(source.EndMonth, duplicate.EndMonth);
        Assert.Equal(source.EndYear, duplicate.EndYear);
        Assert.Equal(source.IsCurrentlyStudying, duplicate.IsCurrentlyStudying);
        Assert.Equal(source.Grade, duplicate.Grade);
        Assert.Equal(source.Description, duplicate.Description);
        Assert.Equal(source.InstitutionUrl, duplicate.InstitutionUrl);
        Assert.True(duplicate.HasUserInput());
    }

    [Fact]
    public void SortByDateNewestFirst_PlacesCurrentStudyFirst()
    {
        var older = CreateValidEntry();
        older.StartMonth = 1;
        older.StartYear = 2015;
        older.EndMonth = 12;
        older.EndYear = 2018;

        var current = CreateValidEntry();
        current.Degree = "Master of Science";
        current.IsCurrentlyStudying = true;
        current.EndMonth = null;
        current.EndYear = null;

        var sorted = EducationSorter.SortByDateNewestFirst([older, current]);

        Assert.Equal(current.Id, sorted[0].Id);
    }

    [Fact]
    public void SortByDateNewestFirst_KeepsDraftEntriesAtBottom()
    {
        var active = CreateValidEntry();
        var draft = new EducationEntry();

        var sorted = EducationSorter.SortByDateNewestFirst([draft, active]);

        Assert.Equal(active.Id, sorted[0].Id);
        Assert.Equal(draft.Id, sorted[1].Id);
    }

    [Fact]
    public void SchemaFields_UseTranslationKeysForValidationMessages()
    {
        foreach (var field in EducationSchema.EntryFields)
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
        entry.Degree = "   ";

        var result = Validator.Validate([entry]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationEducationDegreeRequired);
    }

    [Fact]
    public void HasUserInput_DetectsNonDefaultDegreeType()
    {
        var entry = new EducationEntry { DegreeType = DegreeType.Master };

        Assert.True(entry.HasUserInput());
    }

    private static void SetField(EducationEntry entry, string fieldName, string value)
    {
        switch (fieldName)
        {
            case "institution": entry.Institution = value; break;
            case "degree": entry.Degree = value; break;
            case "fieldOfStudy": entry.FieldOfStudy = value; break;
            case "location": entry.Location = value; break;
            case "description": entry.Description = value; break;
            case "grade": entry.Grade = value; break;
            default: throw new ArgumentOutOfRangeException(nameof(fieldName));
        }
    }
}
