using ReVitae.Core.Import;
using CvWorkExperienceEntry = ReVitae.Core.Cv.WorkExperience.WorkExperienceEntry;
using CvEducationEntry = ReVitae.Core.Cv.Education.EducationEntry;
using CvSkillsGroupEntry = ReVitae.Core.Cv.Skills.SkillsGroupEntry;
using CvLanguageEntry = ReVitae.Core.Cv.Languages.LanguageEntry;
using CvCertificateEntry = ReVitae.Core.Cv.Certificates.CertificateEntry;
using CvProjectEntry = ReVitae.Core.Cv.Projects.ProjectEntry;
using CvLinkEntry = ReVitae.Core.Cv.Links.LinkEntry;

namespace ReVitae.Core.Export;

public sealed record CvExportSourceData(
	PersonalInformationImport Personal,
	IReadOnlyList<CvWorkExperienceEntry> WorkExperience,
	IReadOnlyList<CvEducationEntry> Education,
	IReadOnlyList<CvSkillsGroupEntry> Skills,
	IReadOnlyList<CvLanguageEntry> Languages,
	IReadOnlyList<CvCertificateEntry> Certificates,
	IReadOnlyList<CvProjectEntry> Projects,
	IReadOnlyList<CvLinkEntry> Links,
	string? AdditionalInformation);
