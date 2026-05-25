using System.Text.RegularExpressions;
using System.Text;
using System.Linq;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Import.Extraction;

/// <summary>
/// Legacy <c>.doc</c> OLE binaries are fragile without HWPF; extract long printable ASCII/Latin‑1 spans for a heuristic draft.
/// </summary>
public sealed class DocTextExtractor : ICvTextExtractor
{
	private static readonly Regex PrintableRunRegex = new(
		@"[A-Za-z0-9À-ž@.,()%/+\-:_][A-Za-z0-9À-ž\s@.,()%/+\-:_]{24,}",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

	public CvTextExtractionResult Extract(string filePath)
	{
		if (ImportExtractorGuards.TryRejectMissing(filePath, out var fail))
		{
			return fail;
		}

		try
		{
			var bytes = File.ReadAllBytes(filePath);
			if (bytes.Length == 0)
			{
				return new CvTextExtractionResult(false, string.Empty, TranslationKeys.ImportErrorEmptyDocument);
			}

			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			var latin1Text = Encoding.GetEncoding("iso-8859-1").GetString(bytes);
			var reconstructed = StitchPrintableSpans(latin1Text);
			var normalized = CvTextNormalizer.Normalize(reconstructed).Trim();
			List<CvImportWarning> warnings =
			[
				new CvImportWarning(TranslationKeys.ImportWarningPartialDocumentContent)
			];
			return string.IsNullOrWhiteSpace(normalized)
				? new CvTextExtractionResult(false, string.Empty, TranslationKeys.ImportErrorEmptyDocument)
				: new CvTextExtractionResult(true, normalized, null, null, warnings, PageCount: null);
		}
		catch (Exception ex) when (
			ex.Message.Contains("encrypt", StringComparison.OrdinalIgnoreCase)
			|| ex.Message.Contains("password", StringComparison.OrdinalIgnoreCase))
		{
			return new CvTextExtractionResult(false, string.Empty, TranslationKeys.ImportErrorPasswordProtected);
		}
		catch (Exception)
		{
			return new CvTextExtractionResult(false, string.Empty, TranslationKeys.ImportErrorUnreadableDocument);
		}
	}

	private static string StitchPrintableSpans(string raw)
	{
		var matches = PrintableRunRegex.Matches(raw);
		if (matches.Count == 0)
		{
			var sanitized =
				Regex.Replace(raw, @"[^\x09\x20-\x7EÀ-ž\n\r]", " ");
			return Regex.Replace(sanitized, @"\s{3,}", "\n\n").Trim();
		}

		IEnumerable<string> lines = Enumerable.Distinct(
			matches.Select(match => Regex.Replace(match.Value.Trim(), @"\s+", " ")));
		return string.Join("\n\n", lines);
	}
}
