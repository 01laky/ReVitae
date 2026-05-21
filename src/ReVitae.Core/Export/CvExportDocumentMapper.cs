using ReVitae.Core.Cv.Certificates;
using ReVitae.Core.Cv.Education;
using ReVitae.Core.Cv.Languages;
using ReVitae.Core.Cv.Links;
using ReVitae.Core.Cv.Projects;
using ReVitae.Core.Cv.Skills;
using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Localization;
using CvCertificateEntry = ReVitae.Core.Cv.Certificates.CertificateEntry;
using CvEducationEntry = ReVitae.Core.Cv.Education.EducationEntry;
using CvLanguageEntry = ReVitae.Core.Cv.Languages.LanguageEntry;
using CvProjectEntry = ReVitae.Core.Cv.Projects.ProjectEntry;
using CvWorkExperienceEntry = ReVitae.Core.Cv.WorkExperience.WorkExperienceEntry;

namespace ReVitae.Core.Export;

public static class CvExportDocumentMapper
{
    public static WorkExperienceEntry MapWorkExperience(CvWorkExperienceEntry entry, AppLocalizer localizer)
    {
        return new WorkExperienceEntry(
            NormalizeRequired(entry.JobTitle),
            NormalizeRequired(entry.Company),
            NormalizeOptional(entry.Location),
            localizer.Get(entry.EmploymentType.ToTranslationKey()),
            entry.BuildDateRangeLabel(localizer.Get(TranslationKeys.WorkExperiencePresent)),
            entry.Description,
            entry.Achievements,
            entry.Technologies,
            entry.CompanyUrl);
    }

    public static EducationEntry MapEducation(CvEducationEntry entry, AppLocalizer localizer)
    {
        return new EducationEntry(
            NormalizeRequired(entry.Degree),
            NormalizeRequired(entry.Institution),
            NormalizeOptional(entry.FieldOfStudy),
            NormalizeOptional(entry.Location),
            localizer.Get(entry.DegreeType.ToTranslationKey()),
            entry.BuildDateRangeLabel(localizer.Get(TranslationKeys.EducationPresent)),
            entry.Grade,
            entry.Description,
            entry.InstitutionUrl);
    }

    public static SkillsGroup MapSkillsGroup(SkillsGroupEntry group, AppLocalizer localizer)
    {
        var skills = group.Skills
            .Where(skill => skill.HasUserInput())
            .Select(skill => new SkillItem(
                skill.Name.Trim(),
                localizer.Get(skill.Proficiency.ToTranslationKey()),
                skill.YearsOfExperience))
            .ToArray();

        return new SkillsGroup(NormalizeRequired(group.Category), skills);
    }

    public static LanguageEntry MapLanguage(CvLanguageEntry entry, AppLocalizer localizer)
    {
        return new LanguageEntry(
            LanguagePreviewFormatter.FormatMainLine(entry, localizer),
            LanguagePreviewFormatter.FormatSubSkillLines(entry, localizer));
    }

    public static CertificateEntry MapCertificate(CvCertificateEntry entry, AppLocalizer localizer)
    {
        return new CertificateEntry(
            CertificatePreviewFormatter.FormatMainLine(entry, localizer),
            CertificatePreviewFormatter.FormatDetailLines(entry, localizer));
    }

    public static ProjectEntry MapProject(CvProjectEntry entry, AppLocalizer localizer)
    {
        return new ProjectEntry(
            ProjectPreviewFormatter.FormatMainLine(entry, localizer),
            ProjectPreviewFormatter.FormatDetailLines(entry, localizer));
    }

    public static string MapLinkLine(LinkEntry entry) => LinkPreviewFormatter.FormatLine(entry);

    public static string NormalizeRequired(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();

    public static string NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}
