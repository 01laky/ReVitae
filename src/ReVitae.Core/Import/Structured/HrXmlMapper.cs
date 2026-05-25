using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ReVitae.Core.Cv;
using ReVitae.Core.Cv.Education;
using ReVitae.Core.Cv.Languages;
using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Import;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Import.Structured;

/// <summary>Heuristic importer for HR‑XML and similar résumé XML fragments.</summary>
public static class HrXmlMapper
{
	private static readonly Regex YearPattern =
		new(@"\b(19|20)\d{2}\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);

	private static readonly string[] BodyHints =
	[
		"structuredxmlresume", "employmenthistory", "positionhistory", "educationhistory",
		"employerorgname", "positiontitle", "<resume", "<candidate", "hr-xml", "hropen.org"
	];

	public static CvImportResult Map(XDocument doc)
	{
		if (doc.Root is null || !HasSignals(doc))
		{
			return CvImportResult.Failed(TranslationKeys.ImportErrorUnsupportedStructuredFormat);
		}

		var personal = HarvestPersonal(doc);
		var roles = HarvestWorkExperience(doc);
		var educationEntries = HarvestEducation(doc);
		var languages = HarvestLanguages(doc);

		if (!CvStructuredImportMapper.HasImportableCvData(personal,
				roles,
				educationEntries,
				[],
				languages,
				[],
				[],
				[],
				string.Empty))
		{
			return CvImportResult.Failed(TranslationKeys.ImportErrorNoStructuredData);
		}

		return new CvImportResult
		{
			Success = true,
			Personal = personal,
			WorkExperienceEntries = roles,
			EducationEntries = educationEntries,
			SkillsGroups = [],
			LanguageEntries = languages,
			CertificateEntries = [],
			ProjectEntries = [],
			LinkEntries = [],
			AdditionalInformationContent = string.Empty,
			SectionHasData =
				CvStructuredImportMapper.SectionHasData(personal,
					roles,
					educationEntries, [], languages, [], [], [], string.Empty),
			Warnings = [],
			FieldConfidences = Confidence(personal)
		};
	}

	private static bool HasSignals(XDocument doc)
		=> BodyHints.Any(hint =>
			$"{doc}".Contains(hint, StringComparison.OrdinalIgnoreCase));

	private static PersonalInformationImport HarvestPersonal(XDocument doc)
	{
		var personal = new PersonalInformationImport();

		personal.FirstName = Pick(personal.FirstName, LocateText(doc, "GivenName", "PreferredGivenName"));
		personal.LastName = Pick(personal.LastName, LocateText(doc, "FamilyName", "PreferredFamilyName"));
		personal.Email = Pick(personal.Email, LocateText(doc, "CommunicationAddress", "Email", "InternetEmailAddress"));
		personal.Phone = Pick(personal.Phone, LocateText(doc, "TelephoneFormattedNumber", "Mobile", "Telephone"));
		personal.Location = Pick(personal.Location, LocateText(doc, "PostalCode", "CountryCode"));
		personal.ProfessionalTitle = Pick(personal.ProfessionalTitle, LocateText(doc, "Profession"));
		personal.ShortSummary =
			Pick(personal.ShortSummary, LocateText(doc, "ExecutiveSummary", "Objective"));

		return personal;
	}

	private static List<WorkExperienceEntry> HarvestWorkExperience(XDocument doc)
	{
		var aggregate = new List<WorkExperienceEntry>();

		foreach (var block in ByLocal(doc.Descendants(),
					 "EmploymentHistoryItem", "EmploymentHistoriesItem", "PositionHistory", "EmploymentHistory"))
		{
			var entry = new WorkExperienceEntry();

			entry.JobTitle = Pick(entry.JobTitle, LocateText(block, "PositionTitle", "Title"));
			entry.Company =
				Pick(entry.Company,
					LocateText(block, "EmployerOrgName", "OrganizationName"));

			entry.Location =
				Pick(entry.Location,
					LocateText(block, "PositionLocation", "Municipality", "PostalCode"));

			entry.Description =
				Pick(entry.Description,
					LocateText(block, "Description", "PositionDescription"));

			ApplyWorkYears(entry, $"{block}");

			if (!string.IsNullOrWhiteSpace(entry.JobTitle)
				|| !string.IsNullOrWhiteSpace(entry.Company))
			{
				aggregate.Add(entry);
			}
		}

		return aggregate;
	}

	private static List<EducationEntry> HarvestEducation(XDocument doc)
	{
		var rows = new List<EducationEntry>();

		foreach (var edu in ByLocal(doc.Descendants(), "EducationHistoryItem", "SchoolOrInstitution"))
		{
			var entry = new EducationEntry();

			entry.Institution =
				Pick(entry.Institution, LocateText(edu, "SchoolName"));

			entry.Degree =
				Pick(entry.Degree,
					LocateText(edu, "DegreeName", "EducationDegree"));

			entry.FieldOfStudy =
				Pick(entry.FieldOfStudy,
					LocateText(edu, "DegreeMajor"));

			entry.Description =
				Pick(entry.Description,
					LocateText(edu, "EducationDescription"));

			ApplyEducationYears(entry, $"{edu}");

			if (entry.HasUserInput())
			{
				rows.Add(entry);
			}
		}

		return rows;
	}

	private static List<LanguageEntry> HarvestLanguages(XDocument doc)
	{
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var list = new List<LanguageEntry>();

		foreach (var node in ByLocal(doc.Descendants(),
					 "Language", "MotherTongue"))
		{
			var label =
				LocateText(node, "LanguageCode", "LanguageName");

			label = string.IsNullOrWhiteSpace(label) ? Compress(node!.Value).Trim() : label;

			if (string.IsNullOrWhiteSpace(label) || !seen.Add(label.Trim()))
			{
				continue;
			}

			list.Add(new LanguageEntry { Language = label.Trim() });
		}

		return list;
	}

	private static void ApplyWorkYears(WorkExperienceEntry entry, string material)
	{
		if (material.Contains("present", StringComparison.OrdinalIgnoreCase))
		{
			entry.IsCurrentlyWorking = true;
		}

		var chronological = SortedYears(material).ToArray();
		if (chronological.Length == 0)
		{
			return;
		}

		if (chronological.Length == 1)
		{
			entry.StartYear = chronological[0];
			return;
		}

		entry.StartYear = chronological[0];
		entry.EndYear = chronological[^1];
	}

	private static void ApplyEducationYears(EducationEntry education, string material)
	{
		if (material.Contains("present", StringComparison.OrdinalIgnoreCase))
		{
			education.IsCurrentlyStudying = true;
		}

		var chronological = SortedYears(material).ToArray();
		if (chronological.Length == 0)
		{
			return;
		}

		if (chronological.Length == 1)
		{
			education.StartYear ??= chronological[0];
			return;
		}

		education.StartYear ??= chronological[0];
		education.EndYear ??= chronological[^1];
	}

	private static IEnumerable<XElement> ByLocal(IEnumerable<XElement> iterable, params string[] names)
		=> iterable.Where(candidate => names.Contains(candidate.Name.LocalName,
			StringComparer.OrdinalIgnoreCase));

	private static IEnumerable<int> SortedYears(string corpus)
		=> YearPattern.Matches(corpus).Cast<Match>()
			.Select(match => int.Parse(match.ValueSpan, CultureInfo.InvariantCulture)).Where(year => year > 1900 && year < 2200).Distinct().OrderBy(year => year);

	private static string? LocateText(XDocument doc, params string[] names)
		=> LocateText(doc.Root, names);

	private static string? LocateText(XElement? subtree, params string[] names)
	{
		if (subtree is null)
		{
			return null;
		}

		foreach (var descendant in subtree.Descendants().Prepend(subtree))
		{
			if (names.Contains(descendant.Name.LocalName, StringComparer.OrdinalIgnoreCase))
			{
				var value = descendant.Value.Trim();
				if (!string.IsNullOrWhiteSpace(value))
				{
					return Compress(value);
				}
			}
		}

		return null;
	}

	private static string Pick(string incumbent, string? candidate)
		=> string.IsNullOrWhiteSpace(candidate)
			? incumbent
			: string.IsNullOrWhiteSpace(incumbent)
				? candidate.Trim()
				: incumbent;

	private static string Compress(string input)
		=> Regex.Replace(input, @"\s+", " ").Trim();

	private static List<ImportedFieldConfidence> Confidence(PersonalInformationImport payload)
	{
		var sink = new List<ImportedFieldConfidence>();

		ConfidenceIfText(sink, MainPersonalInformationFieldKeys.FirstName, payload.FirstName);
		ConfidenceIfText(sink, MainPersonalInformationFieldKeys.LastName, payload.LastName);
		ConfidenceIfText(sink, MainPersonalInformationFieldKeys.Email, payload.Email);
		ConfidenceIfText(sink, MainPersonalInformationFieldKeys.Phone, payload.Phone);
		ConfidenceIfText(sink, MainPersonalInformationFieldKeys.Location, payload.Location);

		ConfidenceIfText(sink, MainPersonalInformationFieldKeys.ProfessionalTitle,
			payload.ProfessionalTitle);

		ConfidenceIfText(sink, MainPersonalInformationFieldKeys.ShortSummary, payload.ShortSummary);

		return sink;
	}

	private static void ConfidenceIfText(ICollection<ImportedFieldConfidence> sink,
		string descriptor,
		string? field)
	{
		if (!string.IsNullOrWhiteSpace(field))
		{
			sink.Add(CvStructuredImportMapper.Field(descriptor, CvImportConfidence.Low));
		}
	}
}
