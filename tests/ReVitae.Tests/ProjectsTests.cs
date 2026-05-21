using ReVitae.Core.Cv.Projects;
using ReVitae.Core.Localization;
using ReVitae.Core.Validation;

namespace ReVitae.Tests;

public sealed class ProjectsTests
{
    private static readonly ProjectsCollectionValidator Validator = new();

    private static ProjectEntry CreateValidEntry()
    {
        var entry = new ProjectEntry
        {
            Name = "ReVitae",
            Role = "Lead Developer",
            Organization = "Personal project",
            StartMonth = 1,
            StartYear = 2024,
            EndMonth = 6,
            EndYear = 2025,
            ProjectUrl = "https://github.com/example/revitae",
            Highlights = "Shipped cross-platform desktop MVP.",
            Description = "Built a structured CV editor with live preview and PDF export."
        };

        entry.Technologies.Add(new ProjectTechnologyItem { Name = "C#" });
        entry.Technologies.Add(new ProjectTechnologyItem { Name = "Avalonia" });
        return entry;
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
        var result = Validator.Validate(Array.Empty<ProjectEntry>());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void HasUserInput_TreatsBlankEntryAsDraft()
    {
        Assert.False(new ProjectEntry().HasUserInput());
    }

    [Fact]
    public void Validate_IgnoresDraftEntryWithoutUserInput()
    {
        var result = Validator.Validate([new ProjectEntry()]);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ActivatesEntryAfterFirstFieldInput()
    {
        var entry = new ProjectEntry { Role = "Developer" };

        var result = Validator.Validate([entry]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationProjectsNameRequired);
    }

    [Fact]
    public void Validate_ActivatesEntryAfterAddingTechnologyChip()
    {
        var entry = new ProjectEntry();
        entry.Technologies.Add(new ProjectTechnologyItem { Name = "C#" });

        var result = Validator.Validate([entry]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationProjectsNameRequired);
    }

    [Fact]
    public void Validate_ReportsMissingProjectNameForActiveEntry()
    {
        var entry = new ProjectEntry { Role = "Developer" };

        var result = Validator.Validate([entry]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationProjectsNameRequired);
    }

    [Theory]
    [InlineData("name", 161, TranslationKeys.ValidationProjectsNameMax)]
    [InlineData("role", 121, TranslationKeys.ValidationProjectsRoleMax)]
    [InlineData("highlights", 2001, TranslationKeys.ValidationProjectsHighlightsMax)]
    [InlineData("description", 2001, TranslationKeys.ValidationProjectsDescriptionMax)]
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
    public void Validate_AcceptsValidProjectUrl()
    {
        var entry = CreateValidEntry();
        entry.ProjectUrl = "https://example.com";

        var result = Validator.Validate([entry]);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsInvalidProjectUrl()
    {
        var entry = CreateValidEntry();
        entry.ProjectUrl = "example.com";

        var result = Validator.Validate([entry]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationProjectsProjectUrlFormat);
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
        Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationProjectsStartAfterEnd);
    }

    [Fact]
    public void Validate_AllowsMissingEndDateWhenCurrentlyActive()
    {
        var entry = CreateValidEntry();
        entry.IsCurrentlyActive = true;
        entry.EndMonth = null;
        entry.EndYear = null;

        var result = Validator.Validate([entry]);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsDuplicateTechnologyNamesWithinEntry()
    {
        var entry = CreateValidEntry();
        entry.Technologies.Add(new ProjectTechnologyItem { Name = "c#" });

        var result = Validator.Validate([entry]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationProjectsDuplicateTechnology);
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
        Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationProjectsStartMonthInvalid);
    }

    [Fact]
    public void ParseTechnologyNames_SplitsCommaAndNewlineValues()
    {
        var names = ProjectTechnologiesParser.ParseTechnologyNames("C#, Avalonia\nPostgreSQL");

        Assert.Equal(["C#", "Avalonia", "PostgreSQL"], names);
    }

    [Fact]
    public void Duplicate_CopiesAllFieldValuesWithNewIdentity()
    {
        var source = CreateValidEntry();
        var duplicate = source.Duplicate();

        Assert.NotEqual(source.Id, duplicate.Id);
        Assert.Equal(source.Name, duplicate.Name);
        Assert.Equal(source.Role, duplicate.Role);
        Assert.Equal(source.Organization, duplicate.Organization);
        Assert.Equal(source.StartMonth, duplicate.StartMonth);
        Assert.Equal(source.StartYear, duplicate.StartYear);
        Assert.Equal(source.EndMonth, duplicate.EndMonth);
        Assert.Equal(source.EndYear, duplicate.EndYear);
        Assert.Equal(source.IsCurrentlyActive, duplicate.IsCurrentlyActive);
        Assert.Equal(source.ProjectUrl, duplicate.ProjectUrl);
        Assert.Equal(source.Highlights, duplicate.Highlights);
        Assert.Equal(source.Description, duplicate.Description);
        Assert.Equal(source.Technologies.Count, duplicate.Technologies.Count);
        foreach (var technology in duplicate.Technologies)
        {
            Assert.DoesNotContain(technology.Id, source.Technologies.Select(item => item.Id));
        }
        Assert.True(duplicate.HasUserInput());
    }

    [Fact]
    public void SortByDateNewestFirst_OrdersByStartDateDescending()
    {
        var older = CreateValidEntry();
        older.StartMonth = 1;
        older.StartYear = 2020;

        var newer = CreateValidEntry();
        newer.Name = "Newer Project";
        newer.StartMonth = 6;
        newer.StartYear = 2024;

        var sorted = ProjectSorter.SortByDateNewestFirst([older, newer]);

        Assert.Equal(newer.Id, sorted[0].Id);
    }

    [Fact]
    public void SortByDateNewestFirst_KeepsUndatedEntriesAfterDatedOnes()
    {
        var dated = CreateValidEntry();
        var undated = CreateValidEntry();
        undated.Name = "Undated Project";
        undated.StartMonth = null;
        undated.StartYear = null;

        var sorted = ProjectSorter.SortByDateNewestFirst([undated, dated]);

        Assert.Equal(dated.Id, sorted[0].Id);
        Assert.Equal(undated.Id, sorted[1].Id);
    }

    [Fact]
    public void SortByDateNewestFirst_KeepsDraftEntriesAtBottom()
    {
        var active = CreateValidEntry();
        var draft = new ProjectEntry();

        var sorted = ProjectSorter.SortByDateNewestFirst([draft, active]);

        Assert.Equal(active.Id, sorted[0].Id);
        Assert.Equal(draft.Id, sorted[1].Id);
    }

    [Fact]
    public void PreviewFormatter_IncludesTechnologiesAndHighlights()
    {
        var entry = CreateValidEntry();
        entry.IsCurrentlyActive = true;
        entry.EndMonth = null;
        entry.EndYear = null;
        var localizer = new AppLocalizer(AppLocalizer.FallbackLanguageCode);

        var mainLine = ProjectPreviewFormatter.FormatMainLine(entry, localizer);
        var detailLines = ProjectPreviewFormatter.FormatDetailLines(entry, localizer);

        Assert.Contains("ReVitae", mainLine);
        Assert.Contains("Present", mainLine);
        Assert.Contains(detailLines, line => line.StartsWith("Technologies:", StringComparison.Ordinal));
        Assert.Contains(detailLines, line => line.StartsWith("Highlights:", StringComparison.Ordinal));
    }

    [Fact]
    public void PreviewFormatter_OmitsDateWhenNotProvided()
    {
        var entry = CreateValidEntry();
        entry.StartMonth = null;
        entry.StartYear = null;
        entry.EndMonth = null;
        entry.EndYear = null;
        entry.IsCurrentlyActive = false;
        var localizer = new AppLocalizer(AppLocalizer.FallbackLanguageCode);

        var mainLine = ProjectPreviewFormatter.FormatMainLine(entry, localizer);

        Assert.DoesNotContain("Present", mainLine);
        Assert.DoesNotContain("2024", mainLine);
    }

    [Fact]
    public void SchemaFields_UseTranslationKeysForValidationMessages()
    {
        foreach (var field in ProjectsSchema.EntryFields)
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

        Assert.StartsWith("validation.", ProjectsSchema.TechnologyNameField.RequiredMessage);
        Assert.StartsWith("validation.", ProjectsSchema.TechnologyNameField.MaximumLengthMessage);
    }

    [Fact]
    public void Validate_RejectsWhitespaceOnlyRequiredProjectName()
    {
        var entry = CreateValidEntry();
        entry.Name = "   ";

        var result = Validator.Validate([entry]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationProjectsNameRequired);
    }

    [Fact]
    public void BuildHeaderSummary_IncludesProjectNameAndDateRange()
    {
        var entry = CreateValidEntry();

        var summary = entry.BuildHeaderSummary("Present");

        Assert.Contains(entry.Name, summary);
        Assert.Contains("01 / 2024", summary);
    }

    private static void SetField(ProjectEntry entry, string fieldName, string value)
    {
        switch (fieldName)
        {
            case "name": entry.Name = value; break;
            case "role": entry.Role = value; break;
            case "highlights": entry.Highlights = value; break;
            case "description": entry.Description = value; break;
            default: throw new ArgumentOutOfRangeException(nameof(fieldName));
        }
    }
}
