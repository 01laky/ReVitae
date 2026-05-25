using System.Globalization;
using System.Linq;
using System.Text.Json;
using ReVitae.Core.Cv.Certificates;
using ReVitae.Core.Cv.Education;
using ReVitae.Core.Cv.Languages;
using ReVitae.Core.Cv.Links;
using ReVitae.Core.Cv.Projects;
using ReVitae.Core.Cv.Skills;
using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Cv;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Import.Structured;

public static class JsonResumeMapper
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = true
	};

	public static CvImportResult Map(string jsonText)
	{
		JsonDocument doc;
		try
		{
			doc = JsonDocument.Parse(jsonText, new JsonDocumentOptions
			{
				CommentHandling = JsonCommentHandling.Skip,
				AllowTrailingCommas = true
			});
		}
		catch (JsonException)
		{
			return CvImportResult.Failed(TranslationKeys.ImportErrorUnreadableDocument);
		}

		using (doc)
		{
			return MapDocument(doc.RootElement);
		}
	}

	private static CvImportResult MapDocument(JsonElement root)
	{
		var personal = new PersonalInformationImport();
		var work = new List<WorkExperienceEntry>();
		var education = new List<EducationEntry>();
		var skills = new List<SkillsGroupEntry>();
		var languages = new List<LanguageEntry>();
		var certificates = new List<CertificateEntry>();
		var projects = new List<ProjectEntry>();
		var links = new List<LinkEntry>();
		var confidences = new List<ImportedFieldConfidence>();
		var warnings = new List<CvImportWarning>();
		var extras = new List<string>();

		if (root.TryGetProperty("basics", out var basics))
		{
			MapBasics(basics, personal, links, confidences);
		}

		foreach (var job in EnumerateArray(root, "work"))
		{
			work.Add(MapWork(job));
		}

		foreach (var edu in EnumerateArray(root, "education"))
		{
			education.Add(MapEducation(edu));
		}

		foreach (var skill in EnumerateArray(root, "skills"))
		{
			var group = MapSkillGroup(skill);
			if (group.HasUserInput())
			{
				skills.Add(group);
			}
		}

		foreach (var lang in EnumerateArray(root, "languages"))
		{
			languages.Add(MapLanguage(lang));
		}

		foreach (var cert in EnumerateArray(root, "certificates"))
		{
			certificates.Add(MapCertificate(cert));
		}

		foreach (var proj in EnumerateArray(root, "projects"))
		{
			projects.Add(MapProject(proj));
		}

		AppendJsonArray(root, "volunteer", extras, "Volunteer");
		AppendJsonArray(root, "awards", extras, "Awards");
		AppendJsonArray(root, "publications", extras, "Publications");
		AppendJsonArray(root, "references", extras, "References");

		var additional = string.Join(Environment.NewLine + Environment.NewLine, extras.Where(s => !string.IsNullOrWhiteSpace(s)));
		additional = string.IsNullOrWhiteSpace(additional) ? string.Empty : CvTextNormalizer.Normalize(additional).Trim();

		PruneLinksWithPersonal(personal, links, warnings);

		if (!CvStructuredImportMapper.HasImportableCvData(personal, work, education, skills, languages, certificates, projects, links, additional))
		{
			return CvImportResult.Failed(TranslationKeys.ImportErrorNoStructuredData);
		}

		return new CvImportResult
		{
			Success = true,
			Personal = personal,
			WorkExperienceEntries = work,
			EducationEntries = education,
			SkillsGroups = skills,
			LanguageEntries = languages,
			CertificateEntries = certificates,
			ProjectEntries = projects,
			LinkEntries = links,
			AdditionalInformationContent = additional,
			SectionHasData = CvStructuredImportMapper.SectionHasData(personal, work, education, skills, languages, certificates, projects, links, additional),
			Warnings = warnings,
			FieldConfidences = confidences
		};
	}

	private static IEnumerable<JsonElement> EnumerateArray(JsonElement root, string property)
	{
		if (!root.TryGetProperty(property, out var array) || array.ValueKind != JsonValueKind.Array)
		{
			yield break;
		}

		foreach (var item in array.EnumerateArray())
		{
			yield return item;
		}
	}

	private static void AppendJsonArray(JsonElement root, string property, ICollection<string> sink, string label)
	{
		if (!root.TryGetProperty(property, out var array) || array.ValueKind != JsonValueKind.Array || array.GetArrayLength() == 0)
		{
			return;
		}

		sink.Add($"{label}:{JsonSerializer.Serialize(array, JsonOptions)}");
	}

	private static void MapBasics(JsonElement basics, PersonalInformationImport personal, ICollection<LinkEntry> links, ICollection<ImportedFieldConfidence> confidences)
	{
		if (TryGetString(basics, "label", out var label))
		{
			personal.ProfessionalTitle = label;
			confidences.Add(CvStructuredImportMapper.Field(MainPersonalInformationFieldKeys.ProfessionalTitle, CvImportConfidence.Medium));
		}

		if (TryGetString(basics, "email", out var email))
		{
			personal.Email = email;
			confidences.Add(CvStructuredImportMapper.Field(MainPersonalInformationFieldKeys.Email, CvImportConfidence.High));
		}

		if (TryGetString(basics, "phone", out var phone))
		{
			personal.Phone = phone;
			confidences.Add(CvStructuredImportMapper.Field(MainPersonalInformationFieldKeys.Phone, CvImportConfidence.Medium));
		}

		if (TryGetString(basics, "summary", out var summary))
		{
			personal.ShortSummary = summary;
			confidences.Add(CvStructuredImportMapper.Field(MainPersonalInformationFieldKeys.ShortSummary, CvImportConfidence.Medium));
		}

		if (TryGetString(basics, "url", out var url) && !string.IsNullOrWhiteSpace(url))
		{
			AssignPortfolio(url, personal, confidences);
		}

		if (basics.TryGetProperty("location", out var location) && location.ValueKind == JsonValueKind.Object)
		{
			var chunks = new List<string>();
			if (TryGetString(location, "city", out var city))
			{
				chunks.Add(city);
			}

			if (TryGetString(location, "countryCode", out var country))
			{
				chunks.Add(country);
			}

			if (chunks.Count > 0)
			{
				personal.Location = string.Join(", ", chunks);
				confidences.Add(CvStructuredImportMapper.Field(MainPersonalInformationFieldKeys.Location, CvImportConfidence.Medium));
			}
		}

		if (TryGetString(basics, "name", out var name))
		{
			SplitName(name, personal, confidences);
		}

		foreach (var profile in EnumerateArray(basics, "profiles"))
		{
			TryGetString(profile, "url", out var profileUrl);
			TryGetString(profile, "network", out var network);
			if (!string.IsNullOrWhiteSpace(profileUrl))
			{
				ApplyProfile(network, profileUrl!, personal, links, confidences);
			}
		}
	}

	private static WorkExperienceEntry MapWork(JsonElement node)
	{
		var entry = new WorkExperienceEntry();
		TryGetString(node, "position", out var position);
		TryGetString(node, "company", out var company);
		entry.JobTitle = position ?? string.Empty;
		entry.Company = company ?? string.Empty;
		TryGetString(node, "location", out var loc);
		entry.Location = loc ?? string.Empty;
		TryGetString(node, "summary", out var summary);
		entry.Description = summary ?? string.Empty;

		if (node.TryGetProperty("highlights", out var highs) && highs.ValueKind == JsonValueKind.Array)
		{
			var lines = new List<string>();
			foreach (var line in highs.EnumerateArray())
			{
				var text = line.GetString();
				if (!string.IsNullOrWhiteSpace(text))
				{
					lines.Add(text!);
				}
			}

			entry.Achievements = string.Join(Environment.NewLine, lines);
		}

		if (TryGetString(node, "startDate", out var start))
		{
			TryParseYearMonth(start, out var sm, out var sy);
			entry.StartMonth = sm;
			entry.StartYear = sy;
		}

		if (TryGetString(node, "endDate", out var end))
		{
			if (end.Contains("present", StringComparison.OrdinalIgnoreCase))
			{
				entry.IsCurrentlyWorking = true;
			}
			else
			{
				TryParseYearMonth(end, out var em, out var ey);
				entry.EndMonth = em;
				entry.EndYear = ey;
			}
		}

		return entry;
	}

	private static EducationEntry MapEducation(JsonElement node)
	{
		var entry = new EducationEntry();
		TryGetString(node, "institution", out var inst);
		entry.Institution = inst ?? string.Empty;
		TryGetString(node, "studyType", out var study);
		entry.Degree = study ?? string.Empty;
		TryGetString(node, "area", out var area);
		entry.FieldOfStudy = area ?? string.Empty;

		TryGetString(node, "startDate", out var start);
		TryParseYearMonth(start, out var sm, out var sy);
		entry.StartMonth = sm;
		entry.StartYear = sy;

		TryGetString(node, "endDate", out var end);
		TryParseYearMonth(end, out var em, out var ey);
		entry.EndMonth = em;
		entry.EndYear = ey;

		return entry;
	}

	private static SkillsGroupEntry MapSkillGroup(JsonElement node)
	{
		var group = new SkillsGroupEntry();
		TryGetString(node, "name", out var cat);
		group.Category = string.IsNullOrWhiteSpace(cat) ? "General" : cat!;

		if (node.TryGetProperty("keywords", out var keywords) && keywords.ValueKind == JsonValueKind.Array)
		{
			foreach (var kw in keywords.EnumerateArray())
			{
				var name = kw.GetString();
				if (!string.IsNullOrWhiteSpace(name))
				{
					group.Skills.Add(new SkillItem { Name = name! });
				}
			}
		}

		return group;
	}

	private static LanguageEntry MapLanguage(JsonElement node)
	{
		var entry = new LanguageEntry();
		TryGetString(node, "language", out var lang);
		entry.Language = lang ?? string.Empty;
		TryGetString(node, "fluency", out var fluency);
		MapLanguageProficiency(entry, fluency);
		return entry;
	}

	private static CertificateEntry MapCertificate(JsonElement node)
	{
		var entry = new CertificateEntry();
		TryGetString(node, "name", out var name);
		entry.Name = name ?? string.Empty;
		TryGetString(node, "issuer", out var issuer);
		entry.Issuer = issuer ?? string.Empty;

		TryGetString(node, "date", out var dt);
		TryParseYearMonth(dt, out var month, out var year);
		entry.IssueMonth = month;
		entry.IssueYear = year;
		TryGetString(node, "summary", out var summary);
		entry.Description = summary ?? string.Empty;
		return entry;
	}

	private static ProjectEntry MapProject(JsonElement node)
	{
		var entry = new ProjectEntry();
		TryGetString(node, "name", out var name);
		entry.Name = name ?? string.Empty;
		TryGetString(node, "description", out var desc);
		entry.Description = desc ?? string.Empty;
		TryGetString(node, "url", out var url);
		entry.ProjectUrl = url ?? string.Empty;

		if (node.TryGetProperty("keywords", out var keys) && keys.ValueKind == JsonValueKind.Array)
		{
			foreach (var key in keys.EnumerateArray())
			{
				var label = key.GetString();
				if (!string.IsNullOrWhiteSpace(label))
				{
					entry.Technologies.Add(new ProjectTechnologyItem { Name = label! });
				}
			}
		}

		if (node.TryGetProperty("highlights", out var highs) && highs.ValueKind == JsonValueKind.Array)
		{
			var lines = new List<string>();
			foreach (var line in highs.EnumerateArray())
			{
				var text = line.GetString();
				if (!string.IsNullOrWhiteSpace(text))
				{
					lines.Add(text!);
				}
			}

			entry.Highlights = string.Join(Environment.NewLine, lines);
		}

		TryGetString(node, "startDate", out var start);
		TryParseYearMonth(start, out var sm, out var sy);
		entry.StartMonth = sm;
		entry.StartYear = sy;
		if (TryGetString(node, "endDate", out var end))
		{
			if (end.Contains("present", StringComparison.OrdinalIgnoreCase))
			{
				entry.IsCurrentlyActive = true;
			}
			else
			{
				TryParseYearMonth(end, out var em, out var ey);
				entry.EndMonth = em;
				entry.EndYear = ey;
			}
		}

		return entry;
	}

	private static void MapLanguageProficiency(LanguageEntry entry, string? fluency)
	{
		if (string.IsNullOrWhiteSpace(fluency))
		{
			return;
		}

		var token = fluency.Trim();
		if (Enum.TryParse<CefrLevel>(token, ignoreCase: true, out var cefr))
		{
			entry.CefrLevel = cefr;
			return;
		}

		entry.Proficiency = token.ToLowerInvariant() switch
		{
			"native" or "mother tongue" => LanguageProficiency.Native,
			"fluent" or "full professional" => LanguageProficiency.Fluent,
			"advanced" or "upper intermediate" => LanguageProficiency.Advanced,
			"intermediate" or "working proficiency" => LanguageProficiency.Intermediate,
			"elementary" or "basic" or "beginner" => LanguageProficiency.Elementary,
			_ => LanguageProficiency.Intermediate
		};
	}

	private static bool TryGetString(JsonElement obj, string property, out string? value)
	{
		value = null;
		if (!obj.TryGetProperty(property, out var node))
		{
			return false;
		}

		value = node.GetString();
		return !string.IsNullOrWhiteSpace(value);
	}

	private static void TryParseYearMonth(string? token, out int? month, out int? year)
	{
		month = null;
		year = null;
		if (string.IsNullOrWhiteSpace(token))
		{
			return;
		}

		if (DateTime.TryParse(token, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt))
		{
			month = dt.Month;
			year = dt.Year;
			return;
		}

		if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var yOnly) && yOnly > 1900 && yOnly < 2100)
		{
			year = yOnly;
		}
	}

	private static void SplitName(string fullName, PersonalInformationImport personal, ICollection<ImportedFieldConfidence> confidences)
	{
		var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (parts.Length == 0)
		{
			return;
		}

		if (parts.Length == 1)
		{
			personal.FirstName = parts[0];
			confidences.Add(CvStructuredImportMapper.Field(MainPersonalInformationFieldKeys.FirstName, CvImportConfidence.Low));
			return;
		}

		personal.FirstName = parts[0];
		personal.LastName = string.Join(' ', parts.Skip(1));
		confidences.Add(CvStructuredImportMapper.Field(MainPersonalInformationFieldKeys.FirstName, CvImportConfidence.Medium));
		confidences.Add(CvStructuredImportMapper.Field(MainPersonalInformationFieldKeys.LastName, CvImportConfidence.Medium));
	}

	private static void AssignPortfolio(string url, PersonalInformationImport personal, ICollection<ImportedFieldConfidence> confidences)
	{
		if (string.IsNullOrWhiteSpace(personal.PortfolioUrl))
		{
			personal.PortfolioUrl = url;
			confidences.Add(CvStructuredImportMapper.Field(MainPersonalInformationFieldKeys.PortfolioUrl, CvImportConfidence.Medium));
		}
	}

	private static void ApplyProfile(
		string? network,
		string url,
		PersonalInformationImport personal,
		ICollection<LinkEntry> links,
		ICollection<ImportedFieldConfidence> confidences)
	{
		var lower = (network ?? string.Empty).ToLowerInvariant();
		if (lower.Contains("linkedin", StringComparison.OrdinalIgnoreCase) || url.Contains("linkedin.com", StringComparison.OrdinalIgnoreCase))
		{
			if (string.IsNullOrWhiteSpace(personal.LinkedInUrl))
			{
				personal.LinkedInUrl = url;
				confidences.Add(CvStructuredImportMapper.Field(MainPersonalInformationFieldKeys.LinkedInUrl, CvImportConfidence.High));
			}

			return;
		}

		if (lower.Contains("github", StringComparison.OrdinalIgnoreCase) || url.Contains("github.com", StringComparison.OrdinalIgnoreCase))
		{
			if (string.IsNullOrWhiteSpace(personal.GitHubUrl))
			{
				personal.GitHubUrl = url;
				confidences.Add(CvStructuredImportMapper.Field(MainPersonalInformationFieldKeys.GitHubUrl, CvImportConfidence.High));
			}

			return;
		}

		links.Add(new LinkEntry { Url = url, Label = string.IsNullOrWhiteSpace(network) ? "Link" : network! });
	}

	private static void PruneLinksWithPersonal(PersonalInformationImport personal, IList<LinkEntry> links, ICollection<CvImportWarning> warnings)
	{
		var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			personal.LinkedInUrl,
			personal.GitHubUrl,
			personal.PortfolioUrl
		};

		for (var i = links.Count - 1; i >= 0; i--)
		{
			if (excluded.Contains(links[i].Url))
			{
				links.RemoveAt(i);
				warnings.Add(new CvImportWarning(TranslationKeys.ImportWarningPersonalLinksDuplicatedSkipped));
			}
		}
	}
}
