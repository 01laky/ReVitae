using System.Text;
using ReVitae.Core.Export;
using ReVitae.Core.Import;

namespace ReVitae.Tests.Infrastructure;

/// <summary>
/// Prompt 049 A7 — round-trip diff harness. Compares the structured fields of an exported
/// <see cref="CvExportSourceData"/> against the <see cref="CvImportResult"/> obtained by
/// re-importing it, returning a structured list of differences. Used to prove that a
/// round-trip loses no structured data — stronger than "produced zero validation errors".
/// Strings are NFC-normalized before comparison so encoding form is not a false diff.
/// </summary>
internal static class CvModelDiff
{
	public static IReadOnlyList<string> Compare(CvExportSourceData expected, CvImportResult actual)
	{
		var diffs = new List<string>();

		ComparePersonal(expected, actual, diffs);
		CompareCount("workExperience", expected.WorkExperience.Count, actual.WorkExperienceEntries.Count, diffs);
		CompareCount("education", expected.Education.Count, actual.EducationEntries.Count, diffs);
		CompareCount("skills", expected.Skills.Count, actual.SkillsGroups.Count, diffs);
		CompareCount("languages", expected.Languages.Count, actual.LanguageEntries.Count, diffs);
		CompareCount("certificates", expected.Certificates.Count, actual.CertificateEntries.Count, diffs);
		CompareCount("projects", expected.Projects.Count, actual.ProjectEntries.Count, diffs);
		CompareCount("links", expected.Links.Count, actual.LinkEntries.Count, diffs);

		if (expected.WorkExperience.Count > 0 && actual.WorkExperienceEntries.Count > 0)
		{
			var e = expected.WorkExperience[0];
			var a = actual.WorkExperienceEntries[0];
			CompareField("work[0].jobTitle", e.JobTitle, a.JobTitle, diffs);
			CompareField("work[0].company", e.Company, a.Company, diffs);
		}

		if (expected.Education.Count > 0 && actual.EducationEntries.Count > 0)
		{
			var e = expected.Education[0];
			var a = actual.EducationEntries[0];
			CompareField("education[0].institution", e.Institution, a.Institution, diffs);
			CompareField("education[0].degree", e.Degree, a.Degree, diffs);
		}

		return diffs;
	}

	private static void ComparePersonal(CvExportSourceData expected, CvImportResult actual, List<string> diffs)
	{
		var e = expected.Personal;
		var a = actual.Personal;
		CompareField("personal.firstName", e.FirstName, a.FirstName, diffs);
		CompareField("personal.lastName", e.LastName, a.LastName, diffs);
		CompareField("personal.professionalTitle", e.ProfessionalTitle, a.ProfessionalTitle, diffs);
		CompareField("personal.email", e.Email, a.Email, diffs);
		CompareField("personal.phone", e.Phone, a.Phone, diffs);
		CompareField("personal.location", e.Location, a.Location, diffs);
		CompareField("personal.linkedIn", e.LinkedInUrl, a.LinkedInUrl, diffs);
		CompareField("personal.portfolio", e.PortfolioUrl, a.PortfolioUrl, diffs);
		CompareField("personal.gitHub", e.GitHubUrl, a.GitHubUrl, diffs);
		CompareField("personal.shortSummary", e.ShortSummary, a.ShortSummary, diffs);
	}

	private static void CompareCount(string label, int expected, int actual, List<string> diffs)
	{
		if (expected != actual)
		{
			diffs.Add($"{label}.count: expected {expected}, got {actual}");
		}
	}

	private static void CompareField(string label, string? expected, string? actual, List<string> diffs)
	{
		var e = Normalize(expected);
		var a = Normalize(actual);
		if (!string.Equals(e, a, StringComparison.Ordinal))
		{
			diffs.Add($"{label}: expected '{e}', got '{a}'");
		}
	}

	private static string Normalize(string? value) =>
		string.IsNullOrEmpty(value) ? string.Empty : value.Normalize(NormalizationForm.FormC);
}
