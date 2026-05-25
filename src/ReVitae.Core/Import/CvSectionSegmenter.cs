using ReVitae.Core.Localization;

namespace ReVitae.Core.Import;

public sealed class CvSegmentationResult
{
	public string HeaderBlock { get; init; } = string.Empty;

	public IReadOnlyDictionary<CvImportSectionId, string> SectionBodies { get; init; }
		= new Dictionary<CvImportSectionId, string>();

	public IReadOnlyList<CvImportWarning> Warnings { get; init; } = [];
}

public static class CvSectionSegmenter
{
	private sealed record SectionKeyword(CvImportSectionId SectionId, string Keyword);

	private static readonly SectionKeyword[] Keywords =
	[
		new(CvImportSectionId.Contact, "contact information"),
		new(CvImportSectionId.Contact, "contact"),
		new(CvImportSectionId.Contact, "kontakt"),
		new(CvImportSectionId.Summary, "professional summary"),
		new(CvImportSectionId.Summary, "personal summary"),
		new(CvImportSectionId.Summary, "summary"),
		new(CvImportSectionId.Summary, "profile"),
		new(CvImportSectionId.Summary, "about me"),
		new(CvImportSectionId.Summary, "objective"),
		new(CvImportSectionId.Summary, "profil"),
		new(CvImportSectionId.Summary, "zhrnutie"),
		new(CvImportSectionId.Summary, "o mne"),
		new(CvImportSectionId.WorkExperience, "work experience"),
		new(CvImportSectionId.WorkExperience, "professional experience"),
		new(CvImportSectionId.WorkExperience, "employment history"),
		new(CvImportSectionId.WorkExperience, "employment"),
		new(CvImportSectionId.WorkExperience, "experience"),
		new(CvImportSectionId.WorkExperience, "pracovné skúsenosti"),
		new(CvImportSectionId.WorkExperience, "pracovne skusenosti"),
		new(CvImportSectionId.WorkExperience, "kariéra"),
		new(CvImportSectionId.Education, "academic background"),
		new(CvImportSectionId.Education, "education and training"),
		new(CvImportSectionId.Education, "education"),
		new(CvImportSectionId.Education, "vzdelanie"),
		new(CvImportSectionId.Education, "štúdium"),
		new(CvImportSectionId.Education, "studium"),
		new(CvImportSectionId.Skills, "technical skills"),
		new(CvImportSectionId.Skills, "core competencies"),
		new(CvImportSectionId.Skills, "technologies"),
		new(CvImportSectionId.Skills, "skills"),
		new(CvImportSectionId.Skills, "zručnosti"),
		new(CvImportSectionId.Skills, "zrucnosti"),
		new(CvImportSectionId.Skills, "technológie"),
		new(CvImportSectionId.Languages, "language skills"),
		new(CvImportSectionId.Languages, "languages"),
		new(CvImportSectionId.Languages, "jazykové zručnosti"),
		new(CvImportSectionId.Languages, "jazyky"),
		new(CvImportSectionId.Certificates, "certifications"),
		new(CvImportSectionId.Certificates, "certificates"),
		new(CvImportSectionId.Certificates, "licenses"),
		new(CvImportSectionId.Certificates, "certifikáty"),
		new(CvImportSectionId.Certificates, "certifikaty"),
		new(CvImportSectionId.Projects, "selected projects"),
		new(CvImportSectionId.Projects, "personal projects"),
		new(CvImportSectionId.Projects, "projects"),
		new(CvImportSectionId.Projects, "projekty"),
		new(CvImportSectionId.Links, "online profiles"),
		new(CvImportSectionId.Links, "links"),
		new(CvImportSectionId.Links, "odkazy"),
		new(CvImportSectionId.AdditionalInformation, "additional information"),
		new(CvImportSectionId.AdditionalInformation, "volunteering"),
		new(CvImportSectionId.AdditionalInformation, "publications"),
		new(CvImportSectionId.AdditionalInformation, "interests"),
		new(CvImportSectionId.AdditionalInformation, "hobbies"),
		new(CvImportSectionId.AdditionalInformation, "awards"),
		new(CvImportSectionId.AdditionalInformation, "dodatočné informácie"),
		new(CvImportSectionId.AdditionalInformation, "dodatocne informacie"),
		new(CvImportSectionId.AdditionalInformation, "záujmy"),
		new(CvImportSectionId.AdditionalInformation, "zajmy")
	];

	public static CvSegmentationResult Segment(string normalizedText)
	{
		var warnings = new List<CvImportWarning>();
		var lines = normalizedText.Split('\n');
		var headers = new List<(int LineIndex, CvImportSectionId SectionId)>();

		for (var index = 0; index < lines.Length; index++)
		{
			var line = lines[index].Trim().TrimEnd(':');
			if (!IsProbableHeaderLine(line))
			{
				continue;
			}

			var match = FindBestKeyword(line);
			if (match is not null)
			{
				headers.Add((index, match.SectionId));
			}
		}

		if (headers.Count == 0)
		{
			warnings.Add(new CvImportWarning(TranslationKeys.ImportWarningNoSectionsDetected));
			return new CvSegmentationResult
			{
				HeaderBlock = normalizedText,
				SectionBodies = new Dictionary<CvImportSectionId, string>
				{
					[CvImportSectionId.AdditionalInformation] = normalizedText
				},
				Warnings = warnings
			};
		}

		var bodies = new Dictionary<CvImportSectionId, string>();
		var headerBlockLines = lines.Take(headers[0].LineIndex).ToArray();

		for (var index = 0; index < headers.Count; index++)
		{
			var startLine = headers[index].LineIndex + 1;
			var endLine = index + 1 < headers.Count ? headers[index + 1].LineIndex : lines.Length;
			if (startLine >= endLine)
			{
				continue;
			}

			var body = string.Join('\n', lines.Skip(startLine).Take(endLine - startLine)).Trim();
			if (string.IsNullOrWhiteSpace(body))
			{
				continue;
			}

			var sectionId = headers[index].SectionId;
			if (bodies.TryGetValue(sectionId, out var existing))
			{
				bodies[sectionId] = existing + "\n\n" + body;
			}
			else
			{
				bodies[sectionId] = body;
			}
		}

		return new CvSegmentationResult
		{
			HeaderBlock = string.Join('\n', headerBlockLines).Trim(),
			SectionBodies = bodies,
			Warnings = warnings
		};
	}

	private static bool IsProbableHeaderLine(string line)
	{
		if (string.IsNullOrWhiteSpace(line) || line.Length > 60)
		{
			return false;
		}

		if (Patterns.CvImportPatterns.Email.IsMatch(line)
			|| Patterns.CvImportPatterns.Url.IsMatch(line)
			|| Patterns.DateRangeParser.TryParse(line, out _))
		{
			return false;
		}

		if (IsExportSubheadingLine(line))
		{
			return false;
		}

		return true;
	}

	private static bool IsExportSubheadingLine(string line)
	{
		var label = line.Trim().TrimEnd(':');
		return label.Equals("Technologies", StringComparison.OrdinalIgnoreCase)
			|| label.Equals("Achievements", StringComparison.OrdinalIgnoreCase)
			|| label.Equals("Company URL", StringComparison.OrdinalIgnoreCase)
			|| label.Equals("Institution URL", StringComparison.OrdinalIgnoreCase);
	}

	private static SectionKeyword? FindBestKeyword(string line)
	{
		var normalizedLine = line.Trim().TrimEnd(':');
		if (IsExportSubheadingLine(normalizedLine))
		{
			return null;
		}
		SectionKeyword? best = null;

		foreach (var keyword in Keywords)
		{
			if (normalizedLine.Equals(keyword.Keyword, StringComparison.OrdinalIgnoreCase)
				|| normalizedLine.Contains(keyword.Keyword, StringComparison.OrdinalIgnoreCase)
					&& normalizedLine.Length <= keyword.Keyword.Length + 4)
			{
				if (best is null || keyword.Keyword.Length > best.Keyword.Length)
				{
					best = keyword;
				}
			}
		}

		return best;
	}
}
