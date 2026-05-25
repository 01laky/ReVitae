using System.Text;
using System.Text.RegularExpressions;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Import.Extraction;

public sealed class LatexTextExtractor : ICvTextExtractor
{
	public CvTextExtractionResult Extract(string filePath)
	{
		if (ImportExtractorGuards.TryRejectMissing(filePath, out var fail))
		{
			return fail;
		}

		try
		{
			var raw = File.ReadAllText(filePath, new UTF8Encoding(false, false));
			var normalized = NormalizeLatex(raw);
			if (string.IsNullOrWhiteSpace(normalized))
			{
				return new CvTextExtractionResult(false, string.Empty, TranslationKeys.ImportErrorEmptyDocument);
			}

			return new CvTextExtractionResult(
				true,
				normalized.Trim(),
				null,
				null,
				[new CvImportWarning(TranslationKeys.ImportWarningLatexPartiallyNormalized)],
				null);
		}
		catch (Exception)
		{
			return new CvTextExtractionResult(false, string.Empty, TranslationKeys.ImportErrorUnreadableDocument);
		}
	}

	private static string NormalizeLatex(string input)
	{
		var working = PercentCommentRegex.Replace(input, string.Empty);
		working = SectionCommandRegex.Replace(working, "$1\n");

		for (var pass = 0; pass < 5; pass++)
		{
			working = SimpleBraceCommandRegex.Replace(working, "$1");
		}

		working = Regex.Replace(working, @"\\item\s*", "\n- ", RegexOptions.IgnoreCase);
		working = Regex.Replace(working, @"\\\[[^\]]*\]", " ");
		working = Regex.Replace(working, @"\{|\}|[\\@%&$_#^~]", " ");
		working = Regex.Replace(working, @"[ \t]+\n", "\n");
		working = Regex.Replace(working, @"\n{3,}", "\n\n");
		return working.Trim();
	}

	private static readonly Regex PercentCommentRegex = new(@"(?m)(?<!\\)%.*$");

	private static readonly Regex SectionCommandRegex =
		new(@"\\(?:section\*?|subsection\*?|subsubsection\*?|paragraph\*?)\{([^}]*)\}", RegexOptions.IgnoreCase);

	private static readonly Regex SimpleBraceCommandRegex =
		new(@"\\(?:textbf|textit|emph|underline)\{([^}]*)\}", RegexOptions.IgnoreCase);
}
