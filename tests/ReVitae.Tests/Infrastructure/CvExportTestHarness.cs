using System.Text;
using ReVitae.Core.Export;
using ReVitae.Core.Localization;
using UglyToad.PdfPig;

namespace ReVitae.Tests.Infrastructure;

/// <summary>
/// Shared export/PDF helpers for the 0.3.0 test-expansion suites (prompt 049, Part D).
/// One canonical place to export a document to bytes, read a PDF text layer, and
/// enumerate templates / languages — so the new suites stop hand-rolling these.
/// </summary>
internal static class CvExportTestHarness
{
	public static IEnumerable<CvExportTemplateId> AllTemplateIds => Enum.GetValues<CvExportTemplateId>();

	public static IReadOnlyList<string> SupportedLanguageCodes =>
		AppLocalizer.SupportedLanguages.Select(language => language.Code).ToArray();

	/// <summary>Document-rendered formats (PDF + document/web/text), driven by <c>CvExportDocument</c>.</summary>
	public static IReadOnlyList<CvExportFormat> DocumentFormats =>
	[
		CvExportFormat.Pdf,
		CvExportFormat.Docx,
		CvExportFormat.Odt,
		CvExportFormat.Rtf,
		CvExportFormat.Html,
		CvExportFormat.Markdown,
		CvExportFormat.Txt,
		CvExportFormat.Latex
	];

	/// <summary>Structured formats, driven by <c>CvExportSourceData</c>.</summary>
	public static IReadOnlyList<CvExportFormat> StructuredFormats =>
	[
		CvExportFormat.RevitaeJson,
		CvExportFormat.JsonResume,
		CvExportFormat.Yaml,
		CvExportFormat.EuropassXml,
		CvExportFormat.HrXml,
		CvExportFormat.Csv,
		CvExportFormat.Tsv
	];

	public static byte[] ExportBytes(
		CvExportDocument document,
		CvExportSourceData source,
		CvExportFormat format)
	{
		using var stream = new MemoryStream();
		var result = CvDocumentExporter.Export(document, source, format, stream);
		if (!result.Success)
		{
			throw new InvalidOperationException($"Export to {format} failed: {result.ErrorMessageKey}");
		}

		return stream.ToArray();
	}

	public static bool TryExport(
		CvExportDocument document,
		CvExportSourceData source,
		CvExportFormat format,
		out byte[] bytes)
	{
		using var stream = new MemoryStream();
		var result = CvDocumentExporter.Export(document, source, format, stream);
		bytes = result.Success ? stream.ToArray() : [];
		return result.Success;
	}

	public static string ExportText(
		CvExportDocument document,
		CvExportSourceData source,
		CvExportFormat format) =>
		Encoding.UTF8.GetString(ExportBytes(document, source, format));

	public static string ExtractPdfText(byte[] pdfBytes)
	{
		using var stream = new MemoryStream(pdfBytes);
		using var document = PdfDocument.Open(stream);
		return string.Join('\n', document.GetPages().Select(page => page.Text));
	}

	public static int PdfPageCount(byte[] pdfBytes)
	{
		using var stream = new MemoryStream(pdfBytes);
		using var document = PdfDocument.Open(stream);
		return document.NumberOfPages;
	}

	/// <summary>
	/// PdfPig's text extraction differs across platforms: a large centered/spaced heading is
	/// extracted with inter-letter whitespace on some runners, and Windows runners interleave
	/// NUL / control characters. Stripping both whitespace and control chars keeps the
	/// assertions about content, not the extractor's spacing/padding noise.
	/// </summary>
	public static string RemoveWhitespace(string text) =>
		string.Concat(text.Where(character => !char.IsWhiteSpace(character) && !char.IsControl(character)));
}
