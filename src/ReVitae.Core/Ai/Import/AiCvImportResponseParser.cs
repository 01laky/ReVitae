using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ReVitae.Core.Import;
using ReVitae.Core.Import.Structured;

namespace ReVitae.Core.Ai.Import;

public static partial class AiCvImportResponseParser
{
	private static readonly string[] PhotoKeys =
	[
		"profilePhotoBase64",
		"profilePhotoContentType",
		"profilePhotoPath",
	];

	public sealed record ParseResult(bool Success, JsonObject? Fragment, string? SanitizedError, bool ShouldRetry);

	public static ParseResult TryParse(string rawModelOutput, AiImportPhase phase)
	{
		if (string.IsNullOrWhiteSpace(rawModelOutput))
		{
			return new ParseResult(false, null, "Empty model response.", true);
		}

		var jsonText = StripCodeFence(rawModelOutput.Trim());
		try
		{
			var node = JsonNode.Parse(jsonText);
			if (node is not JsonObject obj)
			{
				return new ParseResult(false, null, "Root JSON must be an object.", true);
			}

			StripPhotoKeys(obj);
			NormalizePhaseFragment(obj, phase);
			return new ParseResult(true, obj, null, false);
		}
		catch (JsonException ex)
		{
			return new ParseResult(false, null, ex.Message, true);
		}
	}

	public static string ToMergedJson(JsonObject accumulated) =>
		accumulated.ToJsonString(new JsonSerializerOptions { WriteIndented = false });

	public static CvImportResult MapAccumulated(JsonObject accumulated, IReadOnlyList<CvImportWarning> extraWarnings)
	{
		accumulated["revitaeVersion"] = 1;
		var json = accumulated.ToJsonString();
		var mapped = ReVitaeJsonMapper.Map(json);
		if (!mapped.Success || extraWarnings.Count == 0)
		{
			return mapped;
		}

		var warnings = mapped.Warnings.ToList();
		warnings.AddRange(extraWarnings);
		return new CvImportResult
		{
			Success = mapped.Success,
			ErrorMessageKey = mapped.ErrorMessageKey,
			Personal = mapped.Personal,
			WorkExperienceEntries = mapped.WorkExperienceEntries,
			EducationEntries = mapped.EducationEntries,
			SkillsGroups = mapped.SkillsGroups,
			LanguageEntries = mapped.LanguageEntries,
			CertificateEntries = mapped.CertificateEntries,
			ProjectEntries = mapped.ProjectEntries,
			LinkEntries = mapped.LinkEntries,
			AdditionalInformationContent = mapped.AdditionalInformationContent,
			SectionHasData = mapped.SectionHasData,
			Warnings = warnings,
			FieldConfidences = mapped.FieldConfidences,
		};
	}

	private static void NormalizePhaseFragment(JsonObject obj, AiImportPhase phase)
	{
		// Accept bare arrays for repeatable phases.
		if (phase is AiImportPhase.Work && obj["workExperience"] is null && obj.Count == 0)
		{
			return;
		}
	}

	private static void StripPhotoKeys(JsonObject obj)
	{
		if (obj["personalInformation"] is JsonObject personal)
		{
			foreach (var key in PhotoKeys)
			{
				personal.Remove(key);
			}
		}

		foreach (var key in PhotoKeys)
		{
			obj.Remove(key);
		}
	}

	private static string StripCodeFence(string text)
	{
		var match = CodeFenceRegex().Match(text);
		return match.Success ? match.Groups[1].Value.Trim() : text;
	}

	[GeneratedRegex(@"```(?:json)?\s*([\s\S]*?)```", RegexOptions.IgnoreCase)]
	private static partial Regex CodeFenceRegex();
}
