using ReVitae.Core.Cv;
using ReVitae.Core.Cv.Certificates;
using ReVitae.Core.Cv.Education;
using ReVitae.Core.Cv.Links;
using ReVitae.Core.Cv.Projects;
using ReVitae.Core.Cv.Skills;
using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Localization;
using ReVitae.Core.Validation;
using ReVitae.Core.Validation.Presentation;

namespace ReVitae.Tests.Ui.Validation;

public sealed class ValidationPresenterEdgeCaseTests
{
    private static FieldValidationError Error(string fieldKey, string message) =>
        new(fieldKey, message);

    [Fact]
    public void BuildMessageMap_EmptyErrors_ReturnsEmptyMap()
    {
        var map = ValidationFieldPresenter.BuildMessageMap([], ValidationTestHelpers.Localize);

        Assert.Empty(map);
    }

    [Fact]
    public void BuildMessageMap_SkipsErrorsWithBlankTargetKey()
    {
        var errors = new[]
        {
            Error("firstName", TranslationKeys.ValidationFirstNameRequired),
            Error("   ", TranslationKeys.ValidationLastNameRequired)
        };

        var map = ValidationFieldPresenter.BuildMessageMap(
            errors,
            ValidationTestHelpers.Localize,
            error => error.FieldKey.Trim());

        Assert.Single(map);
        Assert.True(map.ContainsKey("firstName"));
    }

    [Fact]
    public void BuildMessageMap_SkipsErrorsWhenLocalizedMessageIsBlank()
    {
        var errors = new[]
        {
            Error("email", TranslationKeys.ValidationEmailRequired),
            Error("phone", "   ")
        };

        var map = ValidationFieldPresenter.BuildMessageMap(errors, _ => string.Empty);

        Assert.Empty(map);
    }

    [Fact]
    public void BuildMessageMap_DeduplicatesIdenticalMessagesForSameField()
    {
        var errors = new[]
        {
            Error("email", TranslationKeys.ValidationEmailRequired),
            Error("email", TranslationKeys.ValidationEmailRequired)
        };

        var map = ValidationFieldPresenter.BuildMessageMap(errors, ValidationTestHelpers.Localize);

        Assert.Single(map["email"]);
    }

    [Fact]
    public void BuildMessageMap_PreservesStableOrderForMultipleDistinctMessagesOnSameField()
    {
        var errors = new[]
        {
            Error("email", TranslationKeys.ValidationEmailMax),
            Error("email", TranslationKeys.ValidationEmailFormat),
            Error("email", TranslationKeys.ValidationEmailRequired)
        };

        var map = ValidationFieldPresenter.BuildMessageMap(errors, ValidationTestHelpers.Localize);

        Assert.Equal(3, map["email"].Count);
        Assert.Equal(
            [
                ValidationTestHelpers.Localize(TranslationKeys.ValidationEmailMax),
                ValidationTestHelpers.Localize(TranslationKeys.ValidationEmailFormat),
                ValidationTestHelpers.Localize(TranslationKeys.ValidationEmailRequired)
            ],
            map["email"]);
    }

    [Fact]
    public void BuildMessageMap_UsesCustomResolveTargetKey()
    {
        var errors = new[] { Error("workExperience.entry.jobTitle", TranslationKeys.ValidationWorkExperienceJobTitleRequired) };

        var map = ValidationFieldPresenter.BuildMessageMap(
            errors,
            ValidationTestHelpers.Localize,
            _ => "jobTitle");

        Assert.True(map.ContainsKey("jobTitle"));
        Assert.DoesNotContain(map.Keys, key => key.Contains("workExperience", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(MainPersonalInformationFieldKeys.FirstName)]
    [InlineData(MainPersonalInformationFieldKeys.Email)]
    [InlineData(MainPersonalInformationFieldKeys.ShortSummary)]
    [InlineData(MainPersonalInformationFieldKeys.LinkedInUrl)]
    public void BuildMessageMap_MapsScalarPersonalInfoKeys(string fieldKey)
    {
        var errors = new[] { Error(fieldKey, TranslationKeys.ValidationFirstNameRequired) };

        var map = ValidationFieldPresenter.BuildMessageMap(errors, ValidationTestHelpers.Localize);

        Assert.True(map.ContainsKey(fieldKey));
        Assert.NotEmpty(map[fieldKey]);
    }

    [Theory]
    [InlineData("workExperience", "entry-a", "jobTitle")]
    [InlineData("education", "entry-b", "institution")]
    [InlineData("skills", "group-1", "category")]
    [InlineData("languages", "entry-c", "language")]
    [InlineData("certificates", "entry-d", "name")]
    [InlineData("projects", "entry-e", "name")]
    [InlineData("links", "entry-f", "url")]
    public void BuildMessageMap_MapsEntryPrefixedKeys(string prefix, string entryId, string fieldSuffix)
    {
        var fieldKey = $"{prefix}.{entryId}.{fieldSuffix}";
        var errors = new[] { Error(fieldKey, TranslationKeys.ValidationFirstNameRequired) };

        var map = ValidationFieldPresenter.BuildMessageMap(errors, ValidationTestHelpers.Localize);

        Assert.True(map.ContainsKey(fieldKey));
    }

    [Fact]
    public void BuildMessageMap_LocalizesThroughTranslationKeys_NotRawKeyStrings()
    {
        var errors = new[] { Error("firstName", TranslationKeys.ValidationFirstNameRequired) };

        var map = ValidationFieldPresenter.BuildMessageMap(errors, ValidationTestHelpers.Localize);

        Assert.Equal(ValidationTestHelpers.Localize(TranslationKeys.ValidationFirstNameRequired), map["firstName"][0]);
        Assert.DoesNotContain("validation.", map["firstName"][0], StringComparison.Ordinal);
    }

    [Fact]
    public void BuildMessageMap_UnsupportedLanguageOverlay_StillResolvesEnglishFallbackText()
    {
        var unsupportedLocalizer = new AppLocalizer("ja-JP");
        var errors = new[] { Error("email", TranslationKeys.ValidationEmailRequired) };

        var map = ValidationFieldPresenter.BuildMessageMap(errors, unsupportedLocalizer.Get);

        Assert.Equal(
            ValidationTestHelpers.Localize(TranslationKeys.ValidationEmailRequired),
            map["email"][0]);
    }

    [Fact]
    public void BuildMessageMap_MalformedUnknownKeys_DoNotCrash_AndRemainAddressable()
    {
        var errors = new[]
        {
            Error("???", TranslationKeys.ValidationEmailRequired),
            Error("workExperience..jobTitle", TranslationKeys.ValidationWorkExperienceJobTitleRequired)
        };

        var map = ValidationFieldPresenter.BuildMessageMap(errors, ValidationTestHelpers.Localize);

        Assert.Equal(2, map.Count);
    }

    [Theory]
    [InlineData(WorkExperienceFieldKeys.StartMonth)]
    [InlineData(WorkExperienceFieldKeys.StartYear)]
    [InlineData(WorkExperienceFieldKeys.EndMonth)]
    [InlineData(WorkExperienceFieldKeys.EndYear)]
    [InlineData(WorkExperienceFieldKeys.DateRange)]
    public void GetMessagesForSuffix_WorkExperienceDateFields_MatchSuffixOrExactKey(string fieldSuffix)
    {
        const string entryId = "we-1";
        var fieldKey = WorkExperienceFieldKeys.Build(entryId, fieldSuffix);
        var errors = new[] { Error(fieldKey, TranslationKeys.ValidationWorkExperienceStartMonthRequired) };

        var suffixMessages = ValidationFieldPresenter.GetMessagesForSuffix(
            errors,
            fieldSuffix,
            ValidationTestHelpers.Localize);
        var exactMessages = ValidationFieldPresenter.GetMessagesForExactKey(
            errors,
            fieldKey,
            ValidationTestHelpers.Localize);

        Assert.Single(suffixMessages);
        Assert.Equal(exactMessages, suffixMessages);
    }

    [Theory]
    [InlineData(EducationFieldKeys.StartMonth)]
    [InlineData(EducationFieldKeys.EndYear)]
    [InlineData(EducationFieldKeys.DateRange)]
    public void GetMessagesForSuffix_EducationDateFields_MatchSuffixOrExactKey(string fieldSuffix)
    {
        const string entryId = "edu-1";
        var fieldKey = EducationFieldKeys.Build(entryId, fieldSuffix);
        var errors = new[] { Error(fieldKey, TranslationKeys.ValidationEducationStartYearRequired) };

        var suffixMessages = ValidationFieldPresenter.GetMessagesForSuffix(errors, fieldSuffix, ValidationTestHelpers.Localize);
        var exactMessages = ValidationFieldPresenter.GetMessagesForExactKey(errors, fieldKey, ValidationTestHelpers.Localize);

        Assert.Single(suffixMessages);
        Assert.Equal(exactMessages, suffixMessages);
    }

    [Theory]
    [InlineData(CertificatesFieldKeys.IssueMonth)]
    [InlineData(CertificatesFieldKeys.IssueYear)]
    [InlineData(CertificatesFieldKeys.ExpirationMonth)]
    [InlineData(CertificatesFieldKeys.ExpirationYear)]
    [InlineData(CertificatesFieldKeys.DateRange)]
    public void GetMessagesForSuffix_CertificateDateFields_MatchSuffixOrExactKey(string fieldSuffix)
    {
        const string entryId = "cert-1";
        var fieldKey = CertificatesFieldKeys.Build(entryId, fieldSuffix);
        var errors = new[] { Error(fieldKey, TranslationKeys.ValidationCertificatesIssueMonthRequired) };

        var suffixMessages = ValidationFieldPresenter.GetMessagesForSuffix(errors, fieldSuffix, ValidationTestHelpers.Localize);
        var exactMessages = ValidationFieldPresenter.GetMessagesForExactKey(errors, fieldKey, ValidationTestHelpers.Localize);

        Assert.Single(suffixMessages);
        Assert.Equal(exactMessages, suffixMessages);
    }

    [Fact]
    public void GetMessagesForSuffix_SkillsGroupCategory_MatchesDottedSuffix()
    {
        var fieldKey = SkillsFieldKeys.BuildGroup("grp-1", SkillsFieldKeys.Category);
        var errors = new[] { Error(fieldKey, TranslationKeys.ValidationSkillsCategoryRequired) };

        var messages = ValidationFieldPresenter.GetMessagesForSuffix(
            errors,
            SkillsFieldKeys.Category,
            ValidationTestHelpers.Localize);

        Assert.Single(messages);
    }

    [Fact]
    public void GetMessagesForSuffix_SkillChipProficiency_MatchesNestedSuffix()
    {
        var fieldKey = SkillsFieldKeys.BuildSkill("grp-1", "skill-1", SkillsFieldKeys.SkillProficiency);
        var errors = new[] { Error(fieldKey, TranslationKeys.ValidationSkillsSkillNameRequired) };

        var messages = ValidationFieldPresenter.GetMessagesForSuffix(
            errors,
            SkillsFieldKeys.SkillProficiency,
            ValidationTestHelpers.Localize);

        Assert.Single(messages);
    }

    [Fact]
    public void GetMessagesForSuffix_ProjectTechnologyChip_MatchesTechnologySuffix()
    {
        var fieldKey = ProjectsFieldKeys.BuildTechnology("proj-1", "tech-1", ProjectsFieldKeys.TechnologyName);
        var errors = new[] { Error(fieldKey, TranslationKeys.ValidationProjectsTechnologyNameRequired) };

        var messages = ValidationFieldPresenter.GetMessagesForSuffix(
            errors,
            ProjectsFieldKeys.TechnologyName,
            ValidationTestHelpers.Localize);

        Assert.Single(messages);
    }

    [Fact]
    public void GetMessagesForSuffix_DuplicateLinkUrl_KeyedToDuplicateEntrySuffix()
    {
        var firstKey = LinksFieldKeys.Build("link-1", LinksFieldKeys.Url);
        var secondKey = LinksFieldKeys.Build("link-2", LinksFieldKeys.Url);
        var errors = new[]
        {
            Error(firstKey, TranslationKeys.ValidationLinksUrlFormat),
            Error(secondKey, TranslationKeys.ValidationLinksDuplicateUrl)
        };

        var secondMessages = ValidationFieldPresenter.GetMessagesForSuffix(
            errors,
            LinksFieldKeys.Url,
            ValidationTestHelpers.Localize);

        Assert.Equal(2, secondMessages.Count);
        Assert.Contains(ValidationTestHelpers.Localize(TranslationKeys.ValidationLinksDuplicateUrl), secondMessages);
    }

    [Fact]
    public void GetMessagesForExactKey_DoesNotMatchPrefixOnlyKey()
    {
        var errors = new[]
        {
            Error("email", TranslationKeys.ValidationEmailRequired),
            Error("professionalEmail", TranslationKeys.ValidationEmailFormat)
        };

        var messages = ValidationFieldPresenter.GetMessagesForExactKey(
            errors,
            "email",
            ValidationTestHelpers.Localize);

        Assert.Single(messages);
    }

    [Fact]
    public void GetMessagesForSuffix_MatchesExactKeyWithoutDotPrefix()
    {
        var errors = new[] { Error("email", TranslationKeys.ValidationEmailRequired) };

        var messages = ValidationFieldPresenter.GetMessagesForSuffix(
            errors,
            "email",
            ValidationTestHelpers.Localize);

        Assert.Single(messages);
    }

    [Fact]
    public void GetMessagesForSuffix_DeduplicatesLocalizedMessages()
    {
        var fieldKey = MainPersonalInformationFieldKeys.Email;
        var errors = new[]
        {
            Error(fieldKey, TranslationKeys.ValidationEmailRequired),
            Error(fieldKey, TranslationKeys.ValidationEmailRequired)
        };

        var messages = ValidationFieldPresenter.GetMessagesForSuffix(
            errors,
            MainPersonalInformationFieldKeys.Email,
            ValidationTestHelpers.Localize);

        Assert.Single(messages);
    }

    [Fact]
    public void BuildMessageMap_CaseSensitiveKeys_DoNotMergeDistinctCasing()
    {
        var errors = new[]
        {
            Error("Email", TranslationKeys.ValidationEmailRequired),
            Error("email", TranslationKeys.ValidationEmailFormat)
        };

        var map = ValidationFieldPresenter.BuildMessageMap(errors, ValidationTestHelpers.Localize);

        Assert.Equal(2, map.Count);
        Assert.True(map.ContainsKey("Email"));
        Assert.True(map.ContainsKey("email"));
    }

    [Fact]
    public void BuildMessageMap_ClearingErrorsForField_RemovesKeyFromSubsequentMap()
    {
        var invalidErrors = new[] { Error("firstName", TranslationKeys.ValidationFirstNameRequired) };
        var validErrors = Array.Empty<FieldValidationError>();

        var invalidMap = ValidationFieldPresenter.BuildMessageMap(invalidErrors, ValidationTestHelpers.Localize);
        var validMap = ValidationFieldPresenter.BuildMessageMap(validErrors, ValidationTestHelpers.Localize);

        Assert.True(invalidMap.ContainsKey("firstName"));
        Assert.Empty(validMap);
    }
}
