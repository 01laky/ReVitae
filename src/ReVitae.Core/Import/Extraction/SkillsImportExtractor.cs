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
	internal static IReadOnlyList<SkillsGroupEntry> ExtractSkills(string body, ImportSectionExtractionContext context)
	{
		if (string.IsNullOrWhiteSpace(body))
		{
			return [];
		}

		body = MergeSplitReVitaeSkillLines(body);
		body = TrimReVitaeSkillBodyPrefix(body);

		var groups = new List<SkillsGroupEntry>();
		SkillsGroupEntry? currentGroup = null;
		var skillLines = body.Split('\n', StringSplitOptions.TrimEntries);
		var reVitaeSkillFormat = skillLines.Any(line => TryParseSkillPreviewLine(line, out _));

		for (var lineIndex = 0; lineIndex < skillLines.Length; lineIndex++)
		{
			var line = skillLines[lineIndex];
			if (string.IsNullOrWhiteSpace(line) || IsExportSubheadingLine(line) || LooksLikeWorkBleedLine(line) || IsLikelyPersonNameLine(line))
			{
				continue;
			}

			if (TryParseSkillPreviewLine(line, out var previewSkillName))
			{
				currentGroup ??= EnsureDefaultSkillsGroup(groups);
				if (IsPlausibleSkillName(previewSkillName))
				{
					currentGroup.Skills.Add(new SkillItem { Name = previewSkillName });
				}

				continue;
			}

			if (line.Contains(':', StringComparison.Ordinal))
			{
				var parts = line.Split(':', 2);
				var category = parts[0].Trim();
				if (IsExportSubheadingLine(category) || string.IsNullOrWhiteSpace(parts[1]))
				{
					continue;
				}

				var group = new SkillsGroupEntry { Category = category };
				foreach (var skillName in SplitCommaList(parts[1]))
				{
					if (IsPlausibleSkillName(skillName))
					{
						group.Skills.Add(new SkillItem { Name = skillName });
					}
				}

				if (group.HasUserInput())
				{
					groups.Add(group);
					currentGroup = null;
				}

				continue;
			}

			if (line.StartsWith("- ", StringComparison.Ordinal))
			{
				currentGroup ??= EnsureDefaultSkillsGroup(groups);
				var bulletSkill = line[2..].Trim();
				if (IsPlausibleSkillName(bulletSkill))
				{
					currentGroup.Skills.Add(new SkillItem { Name = bulletSkill });
				}

				continue;
			}

			if (line.Contains(',', StringComparison.Ordinal))
			{
				var commaSkills = SplitCommaList(line).Where(IsPlausibleSkillName).ToArray();
				if (commaSkills.Length < 2)
				{
					continue;
				}

				currentGroup ??= EnsureDefaultSkillsGroup(groups);
				foreach (var skillName in commaSkills)
				{
					currentGroup.Skills.Add(new SkillItem { Name = skillName });
				}

				continue;
			}

			if (IsReVitaeExportSkillCategory(line, reVitaeSkillFormat, skillLines, lineIndex))
			{
				currentGroup = new SkillsGroupEntry { Category = line.Trim() };
				groups.Add(currentGroup);
				continue;
			}

			if (!reVitaeSkillFormat && IsPlausibleStandaloneSkillToken(line))
			{
				currentGroup ??= EnsureDefaultSkillsGroup(groups);
				currentGroup.Skills.Add(new SkillItem { Name = line.Trim() });
			}
		}

		RemovePersonNameSkillGroups(groups);

		if (groups.Count > 0)
		{
			context.AddConfidence("skills.import.defaultCategory", CvImportConfidence.Medium);
		}

		return groups;
	}

	internal static bool TryParseSkillPreviewLine(string line, out string skillName)
	{
		skillName = string.Empty;
		if (!line.Contains('·', StringComparison.Ordinal) || LooksLikeWorkBleedLine(line))
		{
			return false;
		}

		var parts = line.Split('·', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length is < 2 or > 3)
		{
			return false;
		}

		skillName = parts[0];
		if (!IsPlausibleSkillName(skillName))
		{
			return false;
		}

		var tail = string.Join(" · ", parts.Skip(1));
		return !tail.Contains("Full-time", StringComparison.OrdinalIgnoreCase)
			&& !tail.Contains("Part-time", StringComparison.OrdinalIgnoreCase)
			&& !tail.Contains(" s.r.o.", StringComparison.OrdinalIgnoreCase)
			&& !DateRangeParser.TryParse(tail, out _);
	}

	internal static bool IsKnownSkillCategoryLabel(string category) =>
		category.Equals("General", StringComparison.OrdinalIgnoreCase)
		|| category.Equals("Backend", StringComparison.OrdinalIgnoreCase)
		|| category.Equals("Frontend", StringComparison.OrdinalIgnoreCase)
		|| category.Equals("Programming", StringComparison.OrdinalIgnoreCase)
		|| category.Equals("DevOps", StringComparison.OrdinalIgnoreCase)
		|| category.Equals("Languages", StringComparison.OrdinalIgnoreCase)
		|| category.Equals("Languages & Runtimes", StringComparison.OrdinalIgnoreCase)
		|| category.Equals("Backend & APIs", StringComparison.OrdinalIgnoreCase)
		|| category.Equals("Data & Storage", StringComparison.OrdinalIgnoreCase)
		|| category.Equals("Cloud & DevOps", StringComparison.OrdinalIgnoreCase)
		|| category.Equals("Observability", StringComparison.OrdinalIgnoreCase)
		|| category.Equals("Security", StringComparison.OrdinalIgnoreCase)
		|| category.Equals("Architecture", StringComparison.OrdinalIgnoreCase)
		|| category.Equals("Leadership", StringComparison.OrdinalIgnoreCase)
		|| category.Equals("Testing", StringComparison.OrdinalIgnoreCase)
		|| category.Equals("Frontend", StringComparison.OrdinalIgnoreCase)
		|| category.Equals("Mobile & Edge", StringComparison.OrdinalIgnoreCase)
		|| category.Equals("AI & Automation", StringComparison.OrdinalIgnoreCase);

	internal static bool IsReVitaeExportSkillCategory(
		string line,
		bool reVitaeSkillFormat,
		IReadOnlyList<string> skillLines,
		int lineIndex)
	{
		if (!reVitaeSkillFormat)
		{
			return IsPlausibleSkillCategory(line);
		}

		var category = line.Trim();
		if (Regex.IsMatch(
				category,
				@"^(Designed|Implemented|Built|Contributed|Collaborated|Delivered|Integrated|Worked|Senior|mobile|Developed|OAuth|Project|Platforms|development|focus|high|Uses)\b",
				RegexOptions.IgnoreCase))
		{
			return false;
		}

		if (category.Contains('·', StringComparison.Ordinal)
			|| category.Contains(':', StringComparison.Ordinal)
			|| category.StartsWith("- ", StringComparison.Ordinal)
			|| TryParseSkillPreviewLine(category, out _)
			|| IsExportSubheadingLine(category)
			|| LooksLikeWorkBleedLine(category)
			|| IsLikelyPersonNameLine(category))
		{
			return false;
		}

		var nextSkillPreviewIndex = FindNextSkillPreviewLineIndex(skillLines, lineIndex + 1);
		if (IsKnownSkillCategoryLabel(category)
			|| category.Contains('&', StringComparison.Ordinal)
			|| category.Contains(" and ", StringComparison.OrdinalIgnoreCase))
		{
			return nextSkillPreviewIndex >= 0
				|| IsPlausibleSkillCategory(category);
		}

		if (nextSkillPreviewIndex < 0)
		{
			return false;
		}

		return !IsPlausibleStandaloneSkillToken(category)
			&& !LooksLikeWorkBleedLine(category)
			&& IsPlausibleSkillCategory(category);
	}

	internal static int FindNextSkillPreviewLineIndex(IReadOnlyList<string> skillLines, int startIndex)
	{
		for (var index = startIndex; index < skillLines.Count && index < startIndex + 4; index++)
		{
			if (string.IsNullOrWhiteSpace(skillLines[index]))
			{
				continue;
			}

			if (TryParseSkillPreviewLine(skillLines[index], out _))
			{
				return index;
			}
		}

		return -1;
	}

	internal static bool IsPlausibleSkillCategory(string line)
	{
		var category = line.Trim();
		if (category.Length is < 2 or > 40)
		{
			return false;
		}

		if (IsKnownSkillCategoryLabel(category))
		{
			return true;
		}

		if (category.Contains(' ', StringComparison.Ordinal))
		{
			return false;
		}

		return !category.Contains('·', StringComparison.Ordinal)
			&& !category.Contains(',', StringComparison.Ordinal)
			&& !DateRangeParser.TryParse(category, out _)
			&& !LooksLikeWorkBleedLine(category)
			&& !IsExportSubheadingLine(category)
			&& !IsLikelyPersonNameLine(category)
			&& !IsPlausibleStandaloneSkillToken(category)
			&& !Regex.IsMatch(
				category,
				@"^(Designed|Implemented|Built|Contributed|Collaborated|Delivered|Integrated|Worked|Senior|Project|Platforms|development|OAuth|focus|high|Uses)\b",
				RegexOptions.IgnoreCase);
	}

	internal static bool LooksLikeWorkBleedLine(string line) =>
		line.Contains(" s.r.o.", StringComparison.OrdinalIgnoreCase)
		|| line.Contains(" a.s.", StringComparison.OrdinalIgnoreCase)
		|| line.Contains("Full-time", StringComparison.OrdinalIgnoreCase)
		|| line.Contains("Part-time", StringComparison.OrdinalIgnoreCase)
		|| line.Contains(" · Full-time", StringComparison.OrdinalIgnoreCase)
		|| line.Contains(" · Part-time", StringComparison.OrdinalIgnoreCase)
		|| DateRangeParser.TryParse(line, out _);

	internal static bool IsExportSubheadingLine(string line)
	{
		var label = line.Trim().TrimEnd(':');
		return label.Equals("Technologies", StringComparison.OrdinalIgnoreCase)
			|| label.Equals("Achievements", StringComparison.OrdinalIgnoreCase)
			|| label.Equals("Company URL", StringComparison.OrdinalIgnoreCase)
			|| label.Equals("Institution URL", StringComparison.OrdinalIgnoreCase);
	}

}
