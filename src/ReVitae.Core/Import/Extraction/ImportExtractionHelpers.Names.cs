using System.Text.RegularExpressions;
using ReVitae.Core.Cv;
using ReVitae.Core.Cv.AdditionalInformation;
using ReVitae.Core.Cv.Certificates;
using ReVitae.Core.Cv.Education;
using ReVitae.Core.Cv.Languages;
using ReVitae.Core.Cv.Links;
using ReVitae.Core.Cv.Projects;
using ReVitae.Core.Cv.Skills;
using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Import.Patterns;
using ReVitae.Core.Import.Pdf;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Import.Extraction;

internal static partial class ImportFieldExtractionCore
{
	internal static void TryAssignBestPersonNameFromLines(
		IReadOnlyList<string> lines,
		PersonalInformationImport personal,
		ImportSectionExtractionContext context,
		CvImportConfidence confidence)
	{
		string? bestLine = null;
		var bestScore = int.MinValue;
		foreach (var line in lines)
		{
			if (!IsLikelyPersonNameLine(line))
			{
				continue;
			}

			var score = ScorePersonNameLine(line);
			if (score > bestScore)
			{
				bestScore = score;
				bestLine = line;
			}
		}

		if (bestLine is not null)
		{
			TryAssignPersonNameFromLine(bestLine, personal, context, confidence);
		}
	}

	internal static int ScorePersonNameLine(string line)
	{
		var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		var score = 0;
		foreach (var part in parts)
		{
			if (part.Length >= 4)
			{
				score += 4;
			}

			if (part.Any(c => c > 127))
			{
				score += 15;
			}

			if (part.Contains('-', StringComparison.Ordinal))
			{
				score -= 4;
			}

			if (IsSkillStopWord(part) || IsLikelyTechNameToken(part))
			{
				score -= 10;
			}
		}

		return score + Math.Min(line.Length, 40);
	}

	internal static bool TryAssignPersonNameFromLine(
		string line,
		PersonalInformationImport personal,
		ImportSectionExtractionContext context,
		CvImportConfidence confidence)
	{
		if (!IsLikelyPersonNameLine(line))
		{
			return false;
		}

		var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		personal.FirstName = parts[0];
		personal.LastName = string.Join(' ', parts.Skip(1));
		context.AddConfidence(MainPersonalInformationFieldKeys.FirstName, confidence);
		context.AddConfidence(MainPersonalInformationFieldKeys.LastName, confidence);
		return true;
	}

	internal static void TryAssignNameFromSkillsSection(
		CvSegmentationResult segmentation,
		PersonalInformationImport personal,
		ImportSectionExtractionContext context)
	{
		if (!segmentation.SectionBodies.TryGetValue(CvImportSectionId.Skills, out var skillsNameSource))
		{
			return;
		}

		var skillLines = skillsNameSource.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		TryAssignBestPersonNameFromLines(skillLines, personal, context, CvImportConfidence.Medium);
		if (!string.IsNullOrWhiteSpace(personal.FirstName))
		{
			return;
		}

		TryAssignSplitPersonNameFromLines(skillLines, personal, context, CvImportConfidence.Medium);
	}

	internal static void TryAssignNameFromOtherSections(
		CvSegmentationResult segmentation,
		PersonalInformationImport personal,
		ImportSectionExtractionContext context)
	{
		CvImportSectionId[] searchOrder =
		[
			CvImportSectionId.Contact,
			CvImportSectionId.Skills,
			CvImportSectionId.WorkExperience,
			CvImportSectionId.Summary,
		];

		foreach (var sectionId in searchOrder)
		{
			if (!segmentation.SectionBodies.TryGetValue(sectionId, out var body))
			{
				continue;
			}

			var lines = body.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			var candidateLines = SelectNameSearchLines(sectionId, lines);
			TryAssignBestPersonNameFromLines(candidateLines, personal, context, CvImportConfidence.Low);
			if (!string.IsNullOrWhiteSpace(personal.FirstName))
			{
				context.Warnings.Add(new CvImportWarning(TranslationKeys.ImportWarningNameUncertain));
				return;
			}

			TryAssignSplitPersonNameFromLines(candidateLines, personal, context, CvImportConfidence.Low);
			if (!string.IsNullOrWhiteSpace(personal.FirstName))
			{
				context.Warnings.Add(new CvImportWarning(TranslationKeys.ImportWarningNameUncertain));
				return;
			}
		}
	}

	internal static string[] SelectNameSearchLines(CvImportSectionId sectionId, string[] lines) =>
		sectionId switch
		{
			CvImportSectionId.WorkExperience or CvImportSectionId.Skills or CvImportSectionId.Contact => lines,
			_ => lines.Take(25).ToArray(),
		};

	internal static bool TryAssignSplitPersonNameFromLines(
		IReadOnlyList<string> lines,
		PersonalInformationImport personal,
		ImportSectionExtractionContext context,
		CvImportConfidence confidence)
	{
		for (var index = 0; index < lines.Count - 1; index++)
		{
			var first = lines[index];
			var second = lines[index + 1];
			if (!IsLikelyNamePart(first)
				|| !IsLikelyNamePart(second)
				|| CvImportPatterns.Email.IsMatch(first)
				|| CvImportPatterns.Email.IsMatch(second))
			{
				continue;
			}

			personal.FirstName = first;
			personal.LastName = second;
			context.AddConfidence(MainPersonalInformationFieldKeys.FirstName, confidence);
			context.AddConfidence(MainPersonalInformationFieldKeys.LastName, confidence);
			return true;
		}

		return false;
	}

	internal static bool IsPlausibleStandaloneSkillToken(string line)
	{
		var token = line.Trim();
		if (!IsPlausibleSkillName(token))
		{
			return false;
		}

		if (token.Contains('.', StringComparison.Ordinal)
			|| token.Contains('#', StringComparison.Ordinal)
			|| token.Contains('+', StringComparison.Ordinal)
			|| token.Contains('/', StringComparison.Ordinal))
		{
			return true;
		}

		return token.Length <= 16 && !token.Contains(' ', StringComparison.Ordinal);
	}

	internal static bool IsLikelyPersonNameLine(string line)
	{
		if (string.IsNullOrWhiteSpace(line) || line.Length > 60)
		{
			return false;
		}

		if (CvImportPatterns.Email.IsMatch(line)
			|| CvImportPatterns.Url.IsMatch(line)
			|| CvImportPatterns.Phone.IsMatch(line)
			|| DateRangeParser.TryParse(line, out _))
		{
			return false;
		}

		if (Regex.IsMatch(line, @"\b(Developer|Engineer|Manager|Director|years|experience|Full|Stack|Senior|Junior|Privileged|Access|Management|Platform|Copilot|Excalibur|OAuth|Cybersecurity|Microservices|integrating|Designed|Developed|Contributed|Implemented|Delivered|Building|frontend|backend|product|standards|technical|authentication)\b", RegexOptions.IgnoreCase))
		{
			return false;
		}

		var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (parts.Length is < 2 or > 4)
		{
			return false;
		}

		return parts.All(IsLikelyNamePart);
	}

	internal static readonly HashSet<string> TechNameTokens = new(StringComparer.OrdinalIgnoreCase)
	{
		"TypeScript",
		"JavaScript",
		"Redis",
		"React",
		"ReactJS",
		"React-Native",
		"NestJS",
		"Node",
		"NodeJS",
		"Node.js",
		"PostgreSQL",
		"Docker",
		"Go",
		"PHP",
		"OAuth",
		"Copilot",
		"Excalibur",
		"Nette",
		"JQuery",
		"Next.js",
		"SharePoint",
		"Android",
		"Azure",
		"Frontend",
		"Backend",
		"Senior",
		"Junior",
		"Medior",
		"General",
		"Work",
		"Contact",
		"Design",
		"Built",
		"Developed",
		"Implemented",
		"Contributed",
		"Collaborated",
		"Delivered",
		"Integrated",
		"Worked",
		"Project",
		"Platforms",
		"Microservices",
		"Cybersecurity",
	};

	internal static void RemovePersonNameSkillGroups(List<SkillsGroupEntry> groups)
	{
		groups.RemoveAll(group =>
			IsLikelyPersonNameLine(group.Category)
			&& group.Skills.Count == 0);
	}

	internal static bool IsPlausibleSkillName(string value)
	{
		var skillName = value.Trim();
		if (skillName.Length is < 2 or > 48)
		{
			return false;
		}

		if (CvImportPatterns.Email.IsMatch(skillName)
			|| CvImportPatterns.Url.IsMatch(skillName)
			|| CvImportPatterns.Phone.IsMatch(skillName)
			|| DateRangeParser.TryParse(skillName, out _))
		{
			return false;
		}

		var words = skillName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (words.Length > 4)
		{
			return false;
		}

		if (words.Length == 1 && IsSkillStopWord(words[0]))
		{
			return false;
		}

		if (words.Length == 2
			&& char.IsUpper(words[0][0])
			&& char.IsUpper(words[1][0])
			&& !skillName.Contains('.', StringComparison.Ordinal)
			&& !skillName.Contains('#', StringComparison.Ordinal)
			&& !skillName.Contains('+', StringComparison.Ordinal))
		{
			return false;
		}

		return !Regex.IsMatch(
			skillName,
			@"\b(and|with|for|the|to|in|on|of|as|at|by|from|into|leading|worked|built|uses|uses|delivered|integrated|designed|implemented|contributed|collaborated|developed|experienced|strong|senior|full|stack|developer|project|technologies|contact|email|phone|location)\b",
			RegexOptions.IgnoreCase);
	}

	internal static bool IsSkillStopWord(string token) =>
		token.Equals("and", StringComparison.OrdinalIgnoreCase)
		|| token.Equals("or", StringComparison.OrdinalIgnoreCase)
		|| token.Equals("with", StringComparison.OrdinalIgnoreCase)
		|| token.Equals("for", StringComparison.OrdinalIgnoreCase)
		|| token.Equals("the", StringComparison.OrdinalIgnoreCase)
		|| token.Equals("as", StringComparison.OrdinalIgnoreCase);

	internal static SkillsGroupEntry EnsureDefaultSkillsGroup(List<SkillsGroupEntry> groups)
	{
		const string defaultCategory = "General";
		var existing = groups.FirstOrDefault(group => group.Category.Equals(defaultCategory, StringComparison.OrdinalIgnoreCase));
		if (existing is not null)
		{
			return existing;
		}

		var created = new SkillsGroupEntry { Category = defaultCategory };
		groups.Add(created);
		return created;
	}

	internal static bool IsLanguageProficiencySubline(string line)
	{
		var labeled = CvImportPatterns.LabeledValue.Match(line.Trim());
		if (!labeled.Success)
		{
			return false;
		}

		var label = labeled.Groups["label"].Value.Trim();
		return label.Equals("Reading", StringComparison.OrdinalIgnoreCase)
			|| label.Equals("Writing", StringComparison.OrdinalIgnoreCase)
			|| label.Equals("Speaking", StringComparison.OrdinalIgnoreCase)
			|| label.Equals("Listening", StringComparison.OrdinalIgnoreCase)
			|| label.Equals("Translation", StringComparison.OrdinalIgnoreCase);
	}

	internal static void MapLanguageProficiency(string token, LanguageEntry entry, ImportSectionExtractionContext context)
	{
		token = token.Trim().Trim('(', ')');
		if (Enum.TryParse<CefrLevel>(token, ignoreCase: true, out var cefr))
		{
			entry.CefrLevel = cefr;
			context.AddConfidence(LanguagesFieldKeys.Build(entry.Id, LanguagesFieldKeys.CefrLevel), CvImportConfidence.High);
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
		context.AddConfidence(LanguagesFieldKeys.Build(entry.Id, LanguagesFieldKeys.Proficiency), CvImportConfidence.Medium);
	}

	internal static string InferLabelFromUrl(string url)
	{
		if (url.Contains("behance.net", StringComparison.OrdinalIgnoreCase))
		{
			return "Behance";
		}

		if (url.Contains("orcid.org", StringComparison.OrdinalIgnoreCase))
		{
			return "ORCID";
		}

		if (url.Contains("stackoverflow.com", StringComparison.OrdinalIgnoreCase))
		{
			return "Stack Overflow";
		}

		if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
		{
			var host = uri.Host.Replace("www.", string.Empty, StringComparison.OrdinalIgnoreCase);
			var label = host.Split('.')[0];
			return char.ToUpperInvariant(label[0]) + label[1..];
		}

		return "Link";
	}

	internal static string NormalizeImportedBoundedText(string text, int maxLength)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return string.Empty;
		}

		var normalized = text.Trim();
		return normalized.Length <= maxLength ? normalized : normalized[..maxLength].TrimEnd();
	}

	internal static string CollapseRepeatedImportParagraphs(string text)
	{
		var paragraphs = text.Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (paragraphs.Length <= 1)
		{
			return CollapseRepeatedImportSentence(text);
		}

		var unique = new List<string>();
		foreach (var paragraph in paragraphs)
		{
			if (unique.Count == 0 || !unique[^1].Equals(paragraph, StringComparison.Ordinal))
			{
				unique.Add(paragraph);
			}
		}

		return string.Join("\n\n", unique);
	}

	internal static string CollapseRepeatedImportSentence(string text)
	{
		var trimmed = text.Trim();
		if (trimmed.Length < 160)
		{
			return trimmed;
		}

		var maxUnit = Math.Min(400, trimmed.Length / 2);
		for (var unitLength = maxUnit; unitLength >= 80; unitLength--)
		{
			var unit = trimmed[..unitLength].TrimEnd();
			if (string.IsNullOrWhiteSpace(unit))
			{
				continue;
			}

			var remainder = trimmed[unitLength..].Replace(unit, string.Empty, StringComparison.Ordinal).Trim();
			if (remainder.Length <= unit.Length / 2)
			{
				return unit;
			}
		}

		return trimmed;
	}
}
