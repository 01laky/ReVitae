using ReVitae.Core.Cv;
using ReVitae.Core.Cv.Education;
using ReVitae.Core.Cv.Skills;
using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Import.Pdf;

namespace ReVitae.Core.Import.Extraction;

public sealed class ImportSectionExtractionContext
{
	public List<CvImportWarning> Warnings { get; } = [];

	public List<ImportedFieldConfidence> FieldConfidences { get; } = [];

	public void AddConfidence(string fieldKey, CvImportConfidence confidence) =>
		FieldConfidences.Add(new ImportedFieldConfidence(fieldKey, confidence));
}

public static class PersonalInformationImportExtractor
{
	public static PersonalInformationImport Extract(
		CvSegmentationResult segmentation,
		ImportSectionExtractionContext context,
		IReadOnlyList<string>? hyperlinkUrls = null,
		ReVitaePdfExportHints? reVitaeHints = null) =>
		ImportFieldExtractionCore.ExtractPersonalInformation(segmentation, context, hyperlinkUrls, reVitaeHints);
}

public static class WorkExperienceImportExtractor
{
	public static IReadOnlyList<WorkExperienceEntry> Extract(
		string body,
		IReadOnlySet<string> sidebarSkillTokens,
		Queue<string> orphanWorkDateFragments) =>
		ImportFieldExtractionCore.ExtractWorkExperience(body, sidebarSkillTokens, orphanWorkDateFragments);
}

public static class EducationImportExtractor
{
	public static IReadOnlyList<EducationEntry> Extract(string body, ImportSectionExtractionContext context) =>
		ImportFieldExtractionCore.ExtractEducation(body, context);
}

public static class SkillsImportExtractor
{
	public static IReadOnlyList<SkillsGroupEntry> Extract(string body, ImportSectionExtractionContext context) =>
		ImportFieldExtractionCore.ExtractSkills(body, context);
}
