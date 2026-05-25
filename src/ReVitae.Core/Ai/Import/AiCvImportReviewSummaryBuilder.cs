using ReVitae.Core.Import;

namespace ReVitae.Core.Ai.Import;

public sealed record AiCvImportSectionSummaryRow(
	CvImportSectionId SectionId,
	string BeforeLabel,
	string AfterLabel,
	bool IsImproved);

public sealed record AiCvImportReviewSummary(
	IReadOnlyList<AiCvImportSectionSummaryRow> Rows,
	IReadOnlyList<CvImportSectionId> ImprovedSections);

public static class AiCvImportReviewSummaryBuilder
{
	public static AiCvImportReviewSummary Build(CvImportResult? before, CvImportResult after)
	{
		before ??= EmptyResult();
		var rows = new List<AiCvImportSectionSummaryRow>();
		var improved = new List<CvImportSectionId>();

		AddPersonalRow(rows, improved, before, after);
		AddRepeatableRow(rows, improved, CvImportSectionId.WorkExperience, before.WorkExperienceEntries.Count, after.WorkExperienceEntries.Count);
		AddRepeatableRow(rows, improved, CvImportSectionId.Education, before.EducationEntries.Count, after.EducationEntries.Count);
		AddRepeatableRow(rows, improved, CvImportSectionId.Skills, before.SkillsGroups.Count, after.SkillsGroups.Count);
		AddRepeatableRow(rows, improved, CvImportSectionId.Languages, before.LanguageEntries.Count, after.LanguageEntries.Count);
		AddRepeatableRow(rows, improved, CvImportSectionId.Certificates, before.CertificateEntries.Count, after.CertificateEntries.Count);
		AddRepeatableRow(rows, improved, CvImportSectionId.Projects, before.ProjectEntries.Count, after.ProjectEntries.Count);
		AddRepeatableRow(rows, improved, CvImportSectionId.Links, before.LinkEntries.Count, after.LinkEntries.Count);
		AddAdditionalRow(rows, improved, before, after);

		return new AiCvImportReviewSummary(rows, improved);
	}

	private static void AddPersonalRow(
		ICollection<AiCvImportSectionSummaryRow> rows,
		ICollection<CvImportSectionId> improved,
		CvImportResult before,
		CvImportResult after)
	{
		var beforeLabel = DescribePersonal(before.Personal);
		var afterLabel = DescribePersonal(after.Personal);
		var isImproved = !string.Equals(beforeLabel, afterLabel, StringComparison.Ordinal) &&
						 afterLabel != "partial" &&
						 (beforeLabel == "partial" || afterLabel == "complete");
		if (isImproved)
		{
			improved.Add(CvImportSectionId.PersonalInformation);
		}

		rows.Add(new AiCvImportSectionSummaryRow(CvImportSectionId.PersonalInformation, beforeLabel, afterLabel, isImproved));
	}

	private static void AddRepeatableRow(
		ICollection<AiCvImportSectionSummaryRow> rows,
		ICollection<CvImportSectionId> improved,
		CvImportSectionId sectionId,
		int beforeCount,
		int afterCount)
	{
		var beforeLabel = beforeCount == 0 ? "—" : $"{beforeCount} entries";
		var afterLabel = afterCount == 0 ? "—" : $"{afterCount} entries";
		var isImproved = afterCount > beforeCount;
		if (isImproved)
		{
			improved.Add(sectionId);
		}

		rows.Add(new AiCvImportSectionSummaryRow(sectionId, beforeLabel, afterLabel, isImproved));
	}

	private static void AddAdditionalRow(
		ICollection<AiCvImportSectionSummaryRow> rows,
		ICollection<CvImportSectionId> improved,
		CvImportResult before,
		CvImportResult after)
	{
		var beforeHas = !string.IsNullOrWhiteSpace(before.AdditionalInformationContent);
		var afterHas = !string.IsNullOrWhiteSpace(after.AdditionalInformationContent);
		var beforeLabel = beforeHas ? "present" : "—";
		var afterLabel = afterHas ? "present" : "—";
		var isImproved = !beforeHas && afterHas;
		if (isImproved)
		{
			improved.Add(CvImportSectionId.AdditionalInformation);
		}

		rows.Add(new AiCvImportSectionSummaryRow(CvImportSectionId.AdditionalInformation, beforeLabel, afterLabel, isImproved));
	}

	private static string DescribePersonal(PersonalInformationImport personal)
	{
		var hasName = !string.IsNullOrWhiteSpace(personal.FirstName) || !string.IsNullOrWhiteSpace(personal.LastName);
		var hasContact = !string.IsNullOrWhiteSpace(personal.Email) || !string.IsNullOrWhiteSpace(personal.Phone);
		if (hasName && hasContact)
		{
			return "complete";
		}

		if (hasName || hasContact || !string.IsNullOrWhiteSpace(personal.ShortSummary))
		{
			return "partial";
		}

		return "—";
	}

	private static CvImportResult EmptyResult() =>
		new()
		{
			Success = false,
			SectionHasData = new Dictionary<CvImportSectionId, bool>(),
		};
}
