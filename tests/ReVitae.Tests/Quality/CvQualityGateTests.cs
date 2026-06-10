using ReVitae.Core.Cv.Certificates;
using ReVitae.Core.Cv.Education;
using ReVitae.Core.Cv.Languages;
using ReVitae.Core.Cv.Links;
using ReVitae.Core.Cv.Projects;
using ReVitae.Core.Cv.Skills;
using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Export;
using ReVitae.Core.Import;
using ReVitae.Core.Quality;
using CvCertificateEntry = ReVitae.Core.Cv.Certificates.CertificateEntry;
using CvEducationEntry = ReVitae.Core.Cv.Education.EducationEntry;
using CvLanguageEntry = ReVitae.Core.Cv.Languages.LanguageEntry;
using CvLinkEntry = ReVitae.Core.Cv.Links.LinkEntry;
using CvProjectEntry = ReVitae.Core.Cv.Projects.ProjectEntry;
using CvSkillsGroupEntry = ReVitae.Core.Cv.Skills.SkillsGroupEntry;
using CvWorkExperienceEntry = ReVitae.Core.Cv.WorkExperience.WorkExperienceEntry;
using CvSkillItem = ReVitae.Core.Cv.Skills.SkillItem;

namespace ReVitae.Tests.Quality;

public sealed class CvQualityGateTests
{
	private static CvExportSourceData Make(
		PersonalInformationImport? personal = null,
		IEnumerable<CvWorkExperienceEntry>? work = null,
		IEnumerable<CvEducationEntry>? education = null,
		IEnumerable<CvSkillsGroupEntry>? skills = null,
		IEnumerable<CvLanguageEntry>? languages = null,
		IEnumerable<CvCertificateEntry>? certificates = null,
		IEnumerable<CvProjectEntry>? projects = null,
		IEnumerable<CvLinkEntry>? links = null,
		string? additional = null) =>
		CvExportSourceDataFactory.Create(
			personal ?? new PersonalInformationImport(),
			work ?? [],
			education ?? [],
			skills ?? [],
			languages ?? [],
			certificates ?? [],
			projects ?? [],
			links ?? [],
			additional);

	private static CvWorkExperienceEntry Work() =>
		new() { JobTitle = "Dev", Company = "Corp", Description = "Did things." };

	private static CvEducationEntry Edu() =>
		new() { Institution = "MIT", Degree = "BSc" };

	private static CvSkillsGroupEntry Skills()
	{
		var g = new CvSkillsGroupEntry { Category = "Tech" };
		g.Skills.Add(new CvSkillItem { Name = "C#" });
		return g;
	}

	// ── HasStartedCv ────────────────────────────────────────────────────────

	[Fact]
	public void HasStartedCv_AllEmpty_ReturnsFalse()
	{
		Assert.False(CvQualityGate.HasStartedCv(Make()));
	}

	[Fact]
	public void HasStartedCv_FirstNameOnly_ReturnsTrue()
	{
		Assert.True(CvQualityGate.HasStartedCv(Make(personal: new PersonalInformationImport { FirstName = "Jane" })));
	}

	[Fact]
	public void HasStartedCv_LastNameOnly_ReturnsTrue()
	{
		Assert.True(CvQualityGate.HasStartedCv(Make(personal: new PersonalInformationImport { LastName = "Doe" })));
	}

	[Fact]
	public void HasStartedCv_EmailOnly_ReturnsTrue()
	{
		Assert.True(CvQualityGate.HasStartedCv(Make(personal: new PersonalInformationImport { Email = "a@b.com" })));
	}

	[Fact]
	public void HasStartedCv_ProfessionalTitleOnly_ReturnsTrue()
	{
		Assert.True(CvQualityGate.HasStartedCv(Make(personal: new PersonalInformationImport { ProfessionalTitle = "Dev" })));
	}

	[Fact]
	public void HasStartedCv_ShortSummaryOnly_ReturnsTrue()
	{
		Assert.True(CvQualityGate.HasStartedCv(Make(personal: new PersonalInformationImport { ShortSummary = "Hello." })));
	}

	[Fact]
	public void HasStartedCv_WorkEntryOnly_ReturnsTrue()
	{
		Assert.True(CvQualityGate.HasStartedCv(Make(work: [Work()])));
	}

	[Fact]
	public void HasStartedCv_EducationOnly_ReturnsTrue()
	{
		Assert.True(CvQualityGate.HasStartedCv(Make(education: [Edu()])));
	}

	[Fact]
	public void HasStartedCv_SkillsOnly_ReturnsTrue()
	{
		Assert.True(CvQualityGate.HasStartedCv(Make(skills: [Skills()])));
	}

	[Fact]
	public void HasStartedCv_LanguageOnly_ReturnsTrue()
	{
		Assert.True(CvQualityGate.HasStartedCv(Make(languages: [new CvLanguageEntry { Language = "English" }])));
	}

	[Fact]
	public void HasStartedCv_CertificateOnly_ReturnsTrue()
	{
		Assert.True(CvQualityGate.HasStartedCv(Make(certificates: [new CvCertificateEntry { Name = "AWS SAA" }])));
	}

	[Fact]
	public void HasStartedCv_ProjectOnly_ReturnsTrue()
	{
		Assert.True(CvQualityGate.HasStartedCv(Make(projects: [new CvProjectEntry { Name = "MyApp" }])));
	}

	[Fact]
	public void HasStartedCv_LinkOnly_ReturnsTrue()
	{
		Assert.True(CvQualityGate.HasStartedCv(Make(links: [new CvLinkEntry { Label = "Blog", Url = "https://x.com" }])));
	}

	[Fact]
	public void HasStartedCv_AdditionalInfoOnly_ReturnsTrue()
	{
		Assert.True(CvQualityGate.HasStartedCv(Make(additional: "Available immediately.")));
	}

	[Fact]
	public void HasStartedCv_WhitespaceNameOnly_ReturnsFalse()
	{
		Assert.False(CvQualityGate.HasStartedCv(Make(personal: new PersonalInformationImport { FirstName = "   " })));
	}

	// ── HasOtherSectionData ─────────────────────────────────────────────────

	[Fact]
	public void HasOtherSectionData_ExcludeWork_HasOnlyWork_ReturnsFalse()
	{
		var data = Make(personal: new PersonalInformationImport { FirstName = "A" }, work: [Work()]);
		// snapshot contains only work (and personal name triggers HasStartedCv but personal fields
		// are checked separately when excluding PersonalInformation, not WorkExperience)
		// HasOtherSectionData with exclude=WorkExperience: education=0, skills=0, etc → false
		// Note: personal title/email/phone/summary would also count, but none are set here.
		Assert.False(CvQualityGate.HasOtherSectionData(data, CvImportSectionId.WorkExperience));
	}

	[Fact]
	public void HasOtherSectionData_ExcludeWork_HasEducation_ReturnsTrue()
	{
		var data = Make(work: [Work()], education: [Edu()]);
		Assert.True(CvQualityGate.HasOtherSectionData(data, CvImportSectionId.WorkExperience));
	}

	[Fact]
	public void HasOtherSectionData_ExcludeEducation_HasOnlyEducation_ReturnsFalse()
	{
		var data = Make(personal: new PersonalInformationImport { FirstName = "A" }, education: [Edu()]);
		Assert.False(CvQualityGate.HasOtherSectionData(data, CvImportSectionId.Education));
	}

	[Fact]
	public void HasOtherSectionData_ExcludeEducation_HasWork_ReturnsTrue()
	{
		var data = Make(work: [Work()], education: [Edu()]);
		Assert.True(CvQualityGate.HasOtherSectionData(data, CvImportSectionId.Education));
	}

	[Fact]
	public void HasOtherSectionData_ExcludeSkills_HasOnlySkills_ReturnsFalse()
	{
		var data = Make(personal: new PersonalInformationImport { FirstName = "A" }, skills: [Skills()]);
		Assert.False(CvQualityGate.HasOtherSectionData(data, CvImportSectionId.Skills));
	}

	[Fact]
	public void HasOtherSectionData_ExcludeSkills_HasWork_ReturnsTrue()
	{
		var data = Make(work: [Work()], skills: [Skills()]);
		Assert.True(CvQualityGate.HasOtherSectionData(data, CvImportSectionId.Skills));
	}

	[Fact]
	public void HasOtherSectionData_ExcludePersonal_HasOnlyPersonalEmail_ReturnsFalse()
	{
		var data = Make(personal: new PersonalInformationImport { Email = "a@b.com" });
		Assert.False(CvQualityGate.HasOtherSectionData(data, CvImportSectionId.PersonalInformation));
	}

	[Fact]
	public void HasOtherSectionData_ExcludePersonal_HasWork_ReturnsTrue()
	{
		var data = Make(
			personal: new PersonalInformationImport { Email = "a@b.com" },
			work: [Work()]);
		Assert.True(CvQualityGate.HasOtherSectionData(data, CvImportSectionId.PersonalInformation));
	}

	[Fact]
	public void HasOtherSectionData_ExcludeCertificates_HasWork_ReturnsTrue()
	{
		var data = Make(
			work: [Work()],
			certificates: [new CvCertificateEntry { Name = "AWS" }]);
		Assert.True(CvQualityGate.HasOtherSectionData(data, CvImportSectionId.Certificates));
	}

	[Fact]
	public void HasOtherSectionData_ExcludeProjects_HasLinks_ReturnsTrue()
	{
		var data = Make(
			projects: [new CvProjectEntry { Name = "App" }],
			links: [new CvLinkEntry { Label = "Blog", Url = "https://x.com" }]);
		Assert.True(CvQualityGate.HasOtherSectionData(data, CvImportSectionId.Projects));
	}

	[Fact]
	public void HasOtherSectionData_ExcludeLinks_HasAdditionalInfo_ReturnsTrue()
	{
		var data = Make(
			links: [new CvLinkEntry { Label = "Blog", Url = "https://x.com" }],
			additional: "Available for relocation.");
		Assert.True(CvQualityGate.HasOtherSectionData(data, CvImportSectionId.Links));
	}
}
