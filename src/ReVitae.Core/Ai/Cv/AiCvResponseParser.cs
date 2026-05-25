using ReVitae.Core.Cv;
using ReVitae.Core.Cv.Projects;
using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Ai.Cv;

public sealed class AiCvResponseParseException : Exception
{
	public AiCvResponseParseException(string errorMessageKey)
		: base(errorMessageKey)
	{
		ErrorMessageKey = errorMessageKey;
	}

	public string ErrorMessageKey { get; }
}

public static class AiCvResponseParser
{
	public static string Parse(string rawModelOutput, AiCvTaskKind task)
	{
		var text = StripMarkdownFences(rawModelOutput).Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			throw new AiCvResponseParseException(TranslationKeys.AiCvEmptyResponse);
		}

		var maxLength = ResolveMaxLength(task);
		if (text.Length > maxLength)
		{
			throw new AiCvResponseParseException(TranslationKeys.AiCvResponseTooLong);
		}

		return text;
	}

	internal static int ResolveMaxLength(AiCvTaskKind task) =>
		task switch
		{
			AiCvTaskKind.ImproveProfessionalSummary or AiCvTaskKind.DraftProfessionalSummary => 800,
			AiCvTaskKind.ImproveWorkDescription or AiCvTaskKind.DraftWorkDescription => WorkExperienceSchema.DescriptionMaxLength,
			AiCvTaskKind.ImproveProjectDescription => ProjectsSchema.DescriptionMaxLength,
			_ => 2000,
		};

	internal static string StripMarkdownFences(string raw)
	{
		var trimmed = raw.Trim();
		if (!trimmed.StartsWith("```", StringComparison.Ordinal))
		{
			return trimmed;
		}

		var firstNewline = trimmed.IndexOf('\n');
		if (firstNewline < 0)
		{
			return trimmed.Trim('`', ' ');
		}

		var body = trimmed[(firstNewline + 1)..];
		var closingIndex = body.LastIndexOf("```", StringComparison.Ordinal);
		if (closingIndex >= 0)
		{
			body = body[..closingIndex];
		}

		return body.Trim();
	}
}
