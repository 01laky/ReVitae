using ReVitae.Core.Import.Extraction;
using ReVitae.Core.Import.Pdf;

namespace ReVitae.Core.Import;

public static class CvImportFieldExtractor
{
	public static CvImportResult Extract(
		CvSegmentationResult segmentation,
		IReadOnlyList<string>? hyperlinkUrls = null,
		ReVitaePdfExportHints? reVitaeHints = null)
	{
		var context = new ImportSectionExtractionContext();
		var personal = ImportFieldExtractionCore.ExtractPersonalInformation(segmentation, context, hyperlinkUrls, reVitaeHints);
		var sidebarSkillTokens = ImportFieldExtractionCore.CollectSidebarSkillTokens(
			ImportFieldExtractionCore.GetBody(segmentation, CvImportSectionId.Skills));
		var orphanWorkDateFragments = ImportFieldExtractionCore.CollectOrphanWorkDateFragments(segmentation.HeaderBlock);
		var workExperience = ImportFieldExtractionCore.ExtractWorkExperience(
			ImportFieldExtractionCore.GetBody(segmentation, CvImportSectionId.WorkExperience),
			sidebarSkillTokens,
			orphanWorkDateFragments);
		var education = ImportFieldExtractionCore.ExtractEducation(
			ImportFieldExtractionCore.GetBody(segmentation, CvImportSectionId.Education),
			context);
		var skills = ImportFieldExtractionCore.ExtractSkills(
			ImportFieldExtractionCore.GetBody(segmentation, CvImportSectionId.Skills),
			context);
		var languages = ImportFieldExtractionCore.ExtractLanguages(
			ImportFieldExtractionCore.GetBody(segmentation, CvImportSectionId.Languages),
			context);
		var certificates = ImportFieldExtractionCore.ExtractCertificates(
			ImportFieldExtractionCore.GetBody(segmentation, CvImportSectionId.Certificates));
		var projects = ImportFieldExtractionCore.ExtractProjects(
			ImportFieldExtractionCore.GetBody(segmentation, CvImportSectionId.Projects));
		var links = ImportFieldExtractionCore.ExtractLinks(
			ImportFieldExtractionCore.GetBody(segmentation, CvImportSectionId.Links),
			personal,
			context);
		var additional = ImportFieldExtractionCore.BuildAdditionalInformation(segmentation, context);

		ImportFieldExtractionCore.AddEntryConfidences(context, workExperience, education, languages, certificates, projects, links);

		var sectionHasData = ImportFieldExtractionCore.BuildSectionHasData(
			personal,
			workExperience,
			education,
			skills,
			languages,
			certificates,
			projects,
			links,
			additional);

		return new CvImportResult
		{
			Success = ImportFieldExtractionCore.HasStructuredData(
				personal, workExperience, education, skills, languages, certificates, projects, links, additional),
			ErrorMessageKey = null,
			Personal = personal,
			WorkExperienceEntries = workExperience,
			EducationEntries = education,
			SkillsGroups = skills,
			LanguageEntries = languages,
			CertificateEntries = certificates,
			ProjectEntries = projects,
			LinkEntries = links,
			AdditionalInformationContent = additional,
			SectionHasData = sectionHasData,
			Warnings = context.Warnings,
			FieldConfidences = context.FieldConfidences
		};
	}
}
