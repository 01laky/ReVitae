using System.Text;
using ReVitae.Core.Export;
using ReVitae.Core.Import;

namespace ReVitae.Core.Ai.Cv;

/// <summary>
/// Extracts a compact plain-text view and basic metrics for a single CV section from a
/// <see cref="CvExportSourceData"/> snapshot. Shared by the advisor gate (045 C.7) and the
/// advisor prompt builder (045 A.2) so both reason about the same content.
/// </summary>
public static class AiCvSectionContent
{
	/// <summary>Sections the per-section advisor supports in v1 (045 A.2).</summary>
	public static readonly IReadOnlyList<CvImportSectionId> AdvisorSections =
	[
		CvImportSectionId.Summary,
		CvImportSectionId.WorkExperience,
		CvImportSectionId.Skills,
		CvImportSectionId.Education,
		CvImportSectionId.Languages,
		CvImportSectionId.Projects,
	];

	public static bool IsAdvisorSection(CvImportSectionId section) =>
		AdvisorSections.Contains(section);

	public sealed record SectionMetrics(int EntryCount, int NonWhitespaceCharCount)
	{
		public bool IsEmpty => EntryCount == 0 && NonWhitespaceCharCount == 0;
	}

	public static SectionMetrics Measure(CvImportSectionId section, CvExportSourceData snapshot)
	{
		var text = Describe(section, snapshot);
		var chars = text.Count(c => !char.IsWhiteSpace(c));
		return new SectionMetrics(CountEntries(section, snapshot), chars);
	}

	public static int CountEntries(CvImportSectionId section, CvExportSourceData snapshot) =>
		section switch
		{
			CvImportSectionId.WorkExperience => snapshot.WorkExperience.Count,
			CvImportSectionId.Education => snapshot.Education.Count,
			CvImportSectionId.Skills => snapshot.Skills.Count,
			CvImportSectionId.Languages => snapshot.Languages.Count,
			CvImportSectionId.Projects => snapshot.Projects.Count,
			CvImportSectionId.Summary =>
				string.IsNullOrWhiteSpace(snapshot.Personal.ShortSummary) ? 0 : 1,
			_ => 0,
		};

	/// <summary>Compact, label-free plain text for the section, suitable for a prompt.</summary>
	public static string Describe(CvImportSectionId section, CvExportSourceData snapshot)
	{
		var sb = new StringBuilder();
		switch (section)
		{
			case CvImportSectionId.Summary:
				sb.Append(snapshot.Personal.ShortSummary.Trim());
				break;
			case CvImportSectionId.WorkExperience:
				foreach (var w in snapshot.WorkExperience)
				{
					AppendLine(sb, JoinNonEmpty(" — ", w.JobTitle, w.Company));
					AppendLine(sb, w.Description);
					if (!string.IsNullOrWhiteSpace(w.Achievements))
					{
						AppendLine(sb, w.Achievements);
					}
				}

				break;
			case CvImportSectionId.Education:
				foreach (var e in snapshot.Education)
				{
					AppendLine(sb, JoinNonEmpty(" — ", e.Degree, e.FieldOfStudy, e.Institution));
				}

				break;
			case CvImportSectionId.Skills:
				foreach (var g in snapshot.Skills)
				{
					var skills = string.Join(", ", g.Skills.Select(s => s.Name).Where(n => !string.IsNullOrWhiteSpace(n)));
					AppendLine(sb, JoinNonEmpty(": ", g.Category, skills));
				}

				break;
			case CvImportSectionId.Languages:
				foreach (var l in snapshot.Languages)
				{
					AppendLine(sb, JoinNonEmpty(" — ", l.Language, l.Proficiency.ToString()));
				}

				break;
			case CvImportSectionId.Projects:
				foreach (var p in snapshot.Projects)
				{
					AppendLine(sb, JoinNonEmpty(" — ", p.Name, p.Role));
					AppendLine(sb, p.Description);
				}

				break;
		}

		return sb.ToString().Trim();
	}

	private static void AppendLine(StringBuilder sb, string? value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			sb.AppendLine(value.Trim());
		}
	}

	private static string JoinNonEmpty(string separator, params string?[] parts) =>
		string.Join(separator, parts.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p!.Trim()));
}
