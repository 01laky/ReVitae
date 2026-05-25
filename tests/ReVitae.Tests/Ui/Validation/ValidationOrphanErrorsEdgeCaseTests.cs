using ReVitae.Core.Cv;
using ReVitae.Core.Cv.AdditionalInformation;
using ReVitae.Core.Cv.Certificates;
using ReVitae.Core.Cv.Education;
using ReVitae.Core.Cv.Languages;
using ReVitae.Core.Cv.Links;
using ReVitae.Core.Cv.Projects;
using ReVitae.Core.Cv.Skills;
using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Localization;
using ReVitae.Core.Validation;
using ReVitae.Core.Validation.Presentation;

namespace ReVitae.Tests.Ui.Validation;

public sealed class ValidationOrphanErrorsEdgeCaseTests
{
	[Fact]
	public void NoOrphanValidatorErrors_ForInvalidPersonalInformationFixture()
	{
		var values = ValidationTestHelpers.BuildInvalidPersonalValues();
		var result = MainPersonalInformationSchema.CreateValidator().Validate(values);
		var registered = ValidationTestHelpers.BuildPersonalInfoRegistryKeys();

		ValidationTestHelpers.AssertEveryErrorKeyIsRegisteredAndMapped(result.Errors, registered);
	}

	[Fact]
	public void NoOrphanValidatorErrors_ForInvalidWorkExperienceFixture()
	{
		var entry = new WorkExperienceEntry("work-1")
		{
			JobTitle = "Engineer",
			Company = string.Empty,
			StartMonth = null,
			StartYear = null,
			EndMonth = 1,
			EndYear = 2020,
			CompanyUrl = "not-a-url"
		};

		var result = new WorkExperienceCollectionValidator().Validate([entry]);
		var registered = ValidationTestHelpers.BuildWorkExperienceRegistryKeys(entry);

		ValidationTestHelpers.AssertEveryErrorKeyIsRegisteredAndMapped(result.Errors, registered);
	}

	[Fact]
	public void NoOrphanValidatorErrors_ForInvalidEducationFixture()
	{
		var entry = new EducationEntry("edu-1")
		{
			Institution = "STU",
			Degree = string.Empty,
			StartMonth = 9,
			StartYear = 2020,
			EndMonth = 1,
			EndYear = 2018
		};

		var result = new EducationCollectionValidator().Validate([entry]);
		var registered = ValidationTestHelpers.BuildEducationRegistryKeys(entry);

		ValidationTestHelpers.AssertEveryErrorKeyIsRegisteredAndMapped(result.Errors, registered);
	}

	[Fact]
	public void NoOrphanValidatorErrors_ForInvalidSkillsFixture_WithChipOnlySkillData()
	{
		var group = new SkillsGroupEntry("group-1");
		group.Skills.Add(new SkillItem("skill-1")
		{
			Name = "Git",
			YearsOfExperience = 99
		});
		group.Skills.Add(new SkillItem("skill-2") { Name = "C#" });
		group.Skills.Add(new SkillItem("skill-3") { Name = "c#" });

		var result = new SkillsCollectionValidator().Validate([group]);
		var registered = ValidationTestHelpers.BuildSkillsRegistryKeys(group);

		ValidationTestHelpers.AssertEveryErrorKeyIsRegisteredAndMapped(result.Errors, registered);
	}

	[Fact]
	public void NoOrphanValidatorErrors_ForInvalidLanguagesFixture()
	{
		var first = new LanguageEntry("lang-1")
		{
			Language = "English",
			Proficiency = LanguageProficiency.Fluent,
			CefrLevel = CefrLevel.C1
		};
		var duplicate = new LanguageEntry("lang-2")
		{
			Language = "english",
			Proficiency = LanguageProficiency.Advanced,
			CefrLevel = CefrLevel.B2
		};
		var invalidCefr = new LanguageEntry("lang-3")
		{
			Language = "Slovak",
			Proficiency = LanguageProficiency.Native,
			Certificate = new string('c', 121)
		};

		var result = new LanguagesCollectionValidator().Validate([first, duplicate, invalidCefr]);
		var registered = ValidationTestHelpers.BuildLanguagesRegistryKeys(first);
		registered.UnionWith(ValidationTestHelpers.BuildLanguagesRegistryKeys(duplicate));
		registered.UnionWith(ValidationTestHelpers.BuildLanguagesRegistryKeys(invalidCefr));

		ValidationTestHelpers.AssertEveryErrorKeyIsRegisteredAndMapped(result.Errors, registered);
	}

	[Fact]
	public void NoOrphanValidatorErrors_ForInvalidCertificatesFixture()
	{
		var entry = new CertificateEntry("cert-1")
		{
			Name = "AWS Certified",
			Issuer = string.Empty,
			IssueMonth = null,
			IssueYear = null,
			ExpirationMonth = 6,
			ExpirationYear = 2020,
			CredentialUrl = "bad-url"
		};

		var result = new CertificatesCollectionValidator().Validate([entry]);
		var registered = ValidationTestHelpers.BuildCertificatesRegistryKeys(entry);

		ValidationTestHelpers.AssertEveryErrorKeyIsRegisteredAndMapped(result.Errors, registered);
	}

	[Fact]
	public void NoOrphanValidatorErrors_ForInvalidProjectsFixture_WithTechnologyChips()
	{
		var entry = new ProjectEntry("proj-1") { Role = "Developer" };
		entry.Technologies.Add(new ProjectTechnologyItem("tech-1") { Name = string.Empty });
		entry.Technologies.Add(new ProjectTechnologyItem("tech-2") { Name = "C#" });
		entry.Technologies.Add(new ProjectTechnologyItem("tech-3") { Name = "c#" });

		var result = new ProjectsCollectionValidator().Validate([entry]);
		var registered = ValidationTestHelpers.BuildProjectsRegistryKeys(entry);

		ValidationTestHelpers.AssertEveryErrorKeyIsRegisteredAndMapped(result.Errors, registered);
	}

	[Fact]
	public void NoOrphanValidatorErrors_ForInvalidLinksFixture_DuplicateUrlOnSecondEntry()
	{
		var first = new LinkEntry("link-1")
		{
			Label = "Portfolio",
			Url = "https://example.com/profile"
		};
		var duplicate = new LinkEntry("link-2")
		{
			Label = "Mirror",
			Url = "HTTPS://EXAMPLE.COM/profile"
		};

		var result = new LinksCollectionValidator().Validate([first, duplicate]);
		var registered = ValidationTestHelpers.BuildLinksRegistryKeys(first);
		registered.UnionWith(ValidationTestHelpers.BuildLinksRegistryKeys(duplicate));

		ValidationTestHelpers.AssertEveryErrorKeyIsRegisteredAndMapped(result.Errors, registered);
		Assert.Contains(
			result.Errors,
			error => error.FieldKey == LinksFieldKeys.Build(duplicate.Id, LinksFieldKeys.Url)
				&& error.Message == TranslationKeys.ValidationLinksDuplicateUrl);
	}

	[Fact]
	public void NoOrphanValidatorErrors_ForInvalidAdditionalInformationFixture()
	{
		var content = new AdditionalInformationContent
		{
			Content = new string('a', AdditionalInformationSchema.ContentMaxLength + 1)
		};

		var result = new AdditionalInformationValidator().Validate(content);
		var registered = ValidationTestHelpers.BuildAdditionalInformationRegistryKeys();

		ValidationTestHelpers.AssertEveryErrorKeyIsRegisteredAndMapped(result.Errors, registered);
	}

	[Fact]
	public void NoOrphanValidatorErrors_ForCombinedMultiSectionFixture()
	{
		var personalValues = ValidationTestHelpers.BuildInvalidPersonalValues();

		var workEntry = new WorkExperienceEntry("work-1") { JobTitle = "Engineer" };
		var educationEntry = new EducationEntry("edu-1") { Institution = "University" };

		var skillsGroup = new SkillsGroupEntry("group-1") { Category = "Tools" };

		var languageEntry = new LanguageEntry("lang-1") { Proficiency = LanguageProficiency.Fluent };
		var certificateEntry = new CertificateEntry("cert-1") { Name = "Certified" };
		var projectEntry = new ProjectEntry("proj-1") { Role = "Lead" };
		projectEntry.Technologies.Add(new ProjectTechnologyItem("tech-1") { Name = "C#" });

		var firstLink = new LinkEntry("link-1")
		{
			Label = "Site",
			Url = "https://example.com/a"
		};
		var duplicateLink = new LinkEntry("link-2")
		{
			Label = "Mirror",
			Url = "https://example.com/a"
		};

		var additional = new AdditionalInformationContent
		{
			Content = new string('x', AdditionalInformationSchema.ContentMaxLength + 5)
		};

		var result = ValidationTestHelpers.ValidateFormLike(
			personalValues,
			[workEntry],
			[educationEntry],
			[skillsGroup],
			[languageEntry],
			[certificateEntry],
			[projectEntry],
			[firstLink, duplicateLink],
			additional);

		Assert.False(result.IsValid);
		Assert.NotEmpty(result.Errors);

		var registered = ValidationTestHelpers.BuildCombinedRegistryKeys(
			[workEntry],
			[educationEntry],
			[skillsGroup],
			[languageEntry],
			[certificateEntry],
			[projectEntry],
			[firstLink, duplicateLink]);

		ValidationTestHelpers.AssertEveryErrorKeyIsRegisteredAndMapped(result.Errors, registered);

		var invalidKeys = ValidationNavigationPlanner.CollectInvalidKeys(result.Errors);
		var firstInvalid = ValidationNavigationPlanner.GetFirstInvalidFieldKey(
			[
				MainPersonalInformationFieldKeys.FirstName,
				MainPersonalInformationFieldKeys.LastName,
				MainPersonalInformationFieldKeys.Email,
				WorkExperienceFieldKeys.Build(workEntry.Id, WorkExperienceFieldKeys.Company),
				EducationFieldKeys.Build(educationEntry.Id, EducationFieldKeys.Degree),
				SkillsFieldKeys.BuildGroup(skillsGroup.Id, SkillsFieldKeys.SkillsCollection),
				LanguagesFieldKeys.Build(languageEntry.Id, LanguagesFieldKeys.Language),
				CertificatesFieldKeys.Build(certificateEntry.Id, CertificatesFieldKeys.Issuer),
				ProjectsFieldKeys.Build(projectEntry.Id, ProjectsFieldKeys.Name),
				LinksFieldKeys.Build(duplicateLink.Id, LinksFieldKeys.Url),
				AdditionalInformationFieldKeys.Content
			],
			invalidKeys);

		Assert.Equal(MainPersonalInformationFieldKeys.FirstName, firstInvalid);
	}

	[Fact]
	public void FindOrphanErrors_ReportsUnregisteredValidatorKeys()
	{
		var errors = new[]
		{
			new FieldValidationError("firstName", TranslationKeys.ValidationFirstNameRequired),
			new FieldValidationError("unmapped.validator.key", TranslationKeys.ValidationEmailRequired)
		};

		var registered = ValidationTestHelpers.BuildPersonalInfoRegistryKeys();
		var orphans = ValidationOrphanChecker.FindOrphanErrors(errors, registered);

		Assert.Single(orphans);
		Assert.Equal("unmapped.validator.key", orphans[0]);
	}

	[Fact]
	public void FindOrphanErrors_BlankResolvedTarget_ReportsOriginalFieldKeyAsOrphan()
	{
		var errors = new[] { new FieldValidationError("workExperience.entry.jobTitle", "msg") };
		var registered = ValidationTestHelpers.BuildPersonalInfoRegistryKeys();

		var orphans = ValidationOrphanChecker.FindOrphanErrors(
			errors,
			registered,
			_ => "   ");

		Assert.Single(orphans);
		Assert.Equal("workExperience.entry.jobTitle", orphans[0]);
	}

	[Fact]
	public void FindOrphanErrors_CustomResolveTargetKey_MapsToRegisteredAlias()
	{
		const string aliasKey = "email";
		var errors = new[]
		{
			new FieldValidationError("legacy.email.key", TranslationKeys.ValidationEmailRequired)
		};

		var registered = new HashSet<string>(StringComparer.Ordinal) { aliasKey };
		var orphans = ValidationOrphanChecker.FindOrphanErrors(
			errors,
			registered,
			_ => aliasKey);

		Assert.Empty(orphans);
	}

	[Theory]
	[InlineData("workExperience.entry.startMonth")]
	[InlineData("education.entry.endYear")]
	[InlineData("certificates.entry.issueMonth")]
	[InlineData("projects.entry.startYear")]
	public void FindOrphanErrors_DateSubfieldKeys_RequireExplicitRegistryEntry(string fieldKey)
	{
		var errors = new[] { new FieldValidationError(fieldKey, TranslationKeys.ValidationWorkExperienceStartMonthRequired) };
		var registered = new HashSet<string>(StringComparer.Ordinal) { "workExperience.entry.jobTitle" };

		var orphans = ValidationOrphanChecker.FindOrphanErrors(errors, registered);

		Assert.Single(orphans);
		Assert.Equal(fieldKey, orphans[0]);
	}
}
