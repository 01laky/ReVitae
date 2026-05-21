using ReVitae.Core.Cv.Links;
using ReVitae.Core.Import;
using CvWorkExperienceEntry = ReVitae.Core.Cv.WorkExperience.WorkExperienceEntry;
using CvEducationEntry = ReVitae.Core.Cv.Education.EducationEntry;
using CvSkillsGroupEntry = ReVitae.Core.Cv.Skills.SkillsGroupEntry;
using CvLanguageEntry = ReVitae.Core.Cv.Languages.LanguageEntry;
using CvCertificateEntry = ReVitae.Core.Cv.Certificates.CertificateEntry;
using CvProjectEntry = ReVitae.Core.Cv.Projects.ProjectEntry;

namespace ReVitae.Core.Export;

public static class CvExportSourceDataFactory
{
    public static CvExportSourceData Create(
        PersonalInformationImport personal,
        IEnumerable<CvWorkExperienceEntry> workExperience,
        IEnumerable<CvEducationEntry> education,
        IEnumerable<CvSkillsGroupEntry> skills,
        IEnumerable<CvLanguageEntry> languages,
        IEnumerable<CvCertificateEntry> certificates,
        IEnumerable<CvProjectEntry> projects,
        IEnumerable<LinkEntry> links,
        string? additionalInformation)
    {
        return new CvExportSourceData(
            personal,
            workExperience.Where(entry => entry.HasUserInput()).ToArray(),
            education.Where(entry => entry.HasUserInput()).ToArray(),
            skills.Where(entry => entry.HasUserInput()).ToArray(),
            languages.Where(entry => entry.HasUserInput()).ToArray(),
            certificates.Where(entry => entry.HasUserInput()).ToArray(),
            projects.Where(entry => entry.HasUserInput()).ToArray(),
            links.Where(entry => entry.HasUserInput()).ToArray(),
            string.IsNullOrWhiteSpace(additionalInformation) ? null : additionalInformation.Trim());
    }
}
