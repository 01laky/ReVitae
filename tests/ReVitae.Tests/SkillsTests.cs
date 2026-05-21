using ReVitae.Core.Cv.Skills;
using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Localization;
using ReVitae.Core.Validation;

namespace ReVitae.Tests;

public sealed class SkillsTests
{
    private static readonly SkillsCollectionValidator Validator = new();

    private static SkillsGroupEntry CreateValidGroup()
    {
        var group = new SkillsGroupEntry
        {
            Category = "Programming Languages"
        };
        group.Skills.Add(new SkillItem
        {
            Name = "C#",
            Proficiency = ProficiencyLevel.Advanced,
            YearsOfExperience = 5
        });
        return group;
    }

    [Fact]
    public void Validate_AcceptsValidCompleteGroup()
    {
        var result = Validator.Validate([CreateValidGroup()]);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_AcceptsEmptyList()
    {
        var result = Validator.Validate(Array.Empty<SkillsGroupEntry>());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void HasUserInput_TreatsBlankGroupAsDraft()
    {
        var group = new SkillsGroupEntry();

        Assert.False(group.HasUserInput());
    }

    [Fact]
    public void Validate_IgnoresDraftGroupWithoutUserInput()
    {
        var result = Validator.Validate([new SkillsGroupEntry()]);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ActivatesGroupAfterCategoryInput()
    {
        var group = new SkillsGroupEntry { Category = "Tools" };

        var result = Validator.Validate([group]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationSkillsAtLeastOneRequired);
    }

    [Fact]
    public void Validate_ReportsMissingCategoryForActiveGroup()
    {
        var group = new SkillsGroupEntry();
        group.Skills.Add(new SkillItem { Name = "Git" });

        var result = Validator.Validate([group]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationSkillsCategoryRequired);
    }

    [Fact]
    public void Validate_RejectsDuplicateSkillNamesWithinGroup()
    {
        var group = CreateValidGroup();
        group.Skills.Add(new SkillItem { Name = "c#" });

        var result = Validator.Validate([group]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationSkillsDuplicateInGroup);
    }

    [Fact]
    public void Validate_RejectsInvalidYearsOfExperience()
    {
        var group = CreateValidGroup();
        group.Skills[0].YearsOfExperience = 61;

        var result = Validator.Validate([group]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message == TranslationKeys.ValidationSkillsYearsOfExperienceInvalid);
    }

    [Theory]
    [InlineData("category", 121, TranslationKeys.ValidationSkillsCategoryMax)]
    [InlineData("skillName", 81, TranslationKeys.ValidationSkillsSkillNameMax)]
    public void Validate_RejectsValuesOverMaximumLength(string fieldName, int length, string expectedMessageKey)
    {
        var group = CreateValidGroup();
        if (fieldName == "category")
        {
            group.Category = new string('a', length);
        }
        else
        {
            group.Skills[0].Name = new string('a', length);
        }

        var result = Validator.Validate([group]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message == expectedMessageKey);
    }

    [Fact]
    public void ParseSkillNames_SplitsCommaSeparatedValues()
    {
        var names = SkillsTextParser.ParseSkillNames("C#, TypeScript, Git");

        Assert.Equal(["C#", "TypeScript", "Git"], names);
    }

    [Fact]
    public void ParseSkillNames_SplitsNewlineSeparatedValues()
    {
        var names = SkillsTextParser.ParseSkillNames("C#\nTypeScript\nGit");

        Assert.Equal(["C#", "TypeScript", "Git"], names);
    }

    [Fact]
    public void ParseSkillNames_TrimsAndRemovesEmptyItems()
    {
        var names = SkillsTextParser.ParseSkillNames(" C# , , TypeScript , ");

        Assert.Equal(["C#", "TypeScript"], names);
    }

    [Fact]
    public void DeduplicateAcrossGroups_KeepsFirstOccurrenceOnly()
    {
        var first = CreateValidGroup();
        var second = new SkillsGroupEntry { Category = "Tools" };
        second.Skills.Add(new SkillItem { Name = "C#", Proficiency = ProficiencyLevel.Intermediate });

        var deduplicated = SkillsDeduplication.DeduplicateAcrossGroups([first, second]);

        Assert.Single(deduplicated[0].Skills);
        Assert.Empty(deduplicated[1].Skills);
    }

    [Fact]
    public void ExcludeWorkExperienceTechnologies_RemovesMatchingSkills()
    {
        var group = CreateValidGroup();
        var workEntry = new WorkExperienceEntry
        {
            JobTitle = "Engineer",
            Company = "Acme",
            StartMonth = 1,
            StartYear = 2020,
            EndMonth = 12,
            EndYear = 2024,
            Technologies = "C#, Avalonia"
        };

        var filtered = SkillsDeduplication.ExcludeWorkExperienceTechnologies([group], [workEntry]);

        Assert.Empty(filtered[0].Skills);
    }

    [Fact]
    public void Duplicate_CopiesAllFieldValuesWithNewIdentity()
    {
        var source = CreateValidGroup();
        var duplicate = source.Duplicate();

        Assert.NotEqual(source.Id, duplicate.Id);
        Assert.Equal(source.Category, duplicate.Category);
        Assert.Single(duplicate.Skills);
        Assert.NotEqual(source.Skills[0].Id, duplicate.Skills[0].Id);
        Assert.Equal(source.Skills[0].Name, duplicate.Skills[0].Name);
        Assert.Equal(source.Skills[0].Proficiency, duplicate.Skills[0].Proficiency);
        Assert.Equal(source.Skills[0].YearsOfExperience, duplicate.Skills[0].YearsOfExperience);
        Assert.True(duplicate.HasUserInput());
    }

    [Fact]
    public void Suggestions_FilterMatchesCaseInsensitively()
    {
        var matches = SkillsSuggestions.Filter("type");

        Assert.Contains("TypeScript", matches);
    }

    [Fact]
    public void BuildHeaderSummary_IncludesCategoryAndSkillPreview()
    {
        var group = CreateValidGroup();

        var summary = group.BuildHeaderSummary();

        Assert.Contains("Programming Languages", summary);
        Assert.Contains("C#", summary);
    }

    [Fact]
    public void SchemaFields_UseTranslationKeysForValidationMessages()
    {
        Assert.StartsWith("validation.", SkillsSchema.CategoryField.RequiredMessage);
        Assert.StartsWith("validation.", SkillsSchema.SkillNameField.RequiredMessage);
        Assert.StartsWith("validation.", SkillsSchema.SkillProficiencyField.FormatMessage!);
    }
}
