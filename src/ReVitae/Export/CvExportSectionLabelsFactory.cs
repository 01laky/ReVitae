using ReVitae.Core.Export;
using ReVitae.Core.Localization;

namespace ReVitae.Export;

public static class CvExportSectionLabelsFactory
{
	public static CvExportSectionLabels Create(AppLocalizer localizer)
	{
		return new CvExportSectionLabels(
			Summary: localizer.Get(TranslationKeys.Summary),
			Contact: localizer.Get(TranslationKeys.Contact),
			Profile: localizer.Get(TranslationKeys.Profile),
			Objective: localizer.Get(TranslationKeys.Objective),
			PreviewWorkExperience: localizer.Get(TranslationKeys.PreviewWorkExperience),
			PreviewAchievements: localizer.Get(TranslationKeys.PreviewAchievements),
			PreviewTechnologies: localizer.Get(TranslationKeys.PreviewTechnologies),
			WorkExperienceCompanyUrl: localizer.Get(TranslationKeys.WorkExperienceCompanyUrl),
			PreviewEducation: localizer.Get(TranslationKeys.PreviewEducation),
			PreviewFieldOfStudy: localizer.Get(TranslationKeys.PreviewFieldOfStudy),
			PreviewGrade: localizer.Get(TranslationKeys.PreviewGrade),
			EducationInstitutionUrl: localizer.Get(TranslationKeys.EducationInstitutionUrl),
			PreviewSkills: localizer.Get(TranslationKeys.PreviewSkills),
			PreviewYearsSuffix: localizer.Get(TranslationKeys.PreviewYearsSuffix),
			PreviewLanguages: localizer.Get(TranslationKeys.PreviewLanguages),
			PreviewCertificates: localizer.Get(TranslationKeys.PreviewCertificates),
			PreviewProjects: localizer.Get(TranslationKeys.PreviewProjects),
			PreviewCustomLinks: localizer.Get(TranslationKeys.PreviewCustomLinks),
			PreviewAdditionalInformation: localizer.Get(TranslationKeys.PreviewAdditionalInformation),
			ContactLinks: localizer.Get(TranslationKeys.ContactLinks),
			Digital: localizer.Get(TranslationKeys.Digital),
			Links: localizer.Get(TranslationKeys.Links),
			Online: localizer.Get(TranslationKeys.Online),
			Email: localizer.Get(TranslationKeys.Email),
			Phone: localizer.Get(TranslationKeys.Phone),
			Location: localizer.Get(TranslationKeys.Location),
			ProfessionalTitle: localizer.Get(TranslationKeys.ProfessionalTitle),
			LinkedInUrl: localizer.Get(TranslationKeys.LinkedInUrl),
			PortfolioUrl: localizer.Get(TranslationKeys.PortfolioUrl),
			GitHubUrl: localizer.Get(TranslationKeys.GitHubUrl));
	}
}
