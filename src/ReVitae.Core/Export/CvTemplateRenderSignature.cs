using System.Security.Cryptography;
using System.Text;
using ReVitae.Core.Export.Fixtures;
using ReVitae.Core.Export.Pdf;
using UglyToad.PdfPig;

namespace ReVitae.Core.Export;

/// <summary>
/// Computes a deterministic, cross-platform <b>content signature</b> for a CV template (047 QG1).
/// Renders the template with the fixed minimal demo dataset, then hashes the exported PDF's raw
/// text content (the glyph characters in content-stream order) read back via PdfPig.
/// <para>
/// The signature is deliberately <b>position-free</b>: glyph coordinates, word segmentation, and
/// pagination all depend on the platform's font metrics (macOS vs the Linux CI runner render the
/// same text at different sub-point offsets and wrap points), so hashing positions made the golden
/// fail on CI. The character sequence QuestPDF emits is identical across platforms, so it is a
/// stable run-to-run / cross-platform oracle that still catches content- and structure-affecting
/// refactors (added/removed/reordered text). Pure visual regressions are covered by the manual
/// template render audit, not this hash.
/// </para>
/// </summary>
public static class CvTemplateRenderSignature
{
	public static string Compute(CvExportTemplateId templateId)
	{
		var document = JohnDoeMinimalArchitectCvDataset.CreateDocument(templateId);
		var pdfBytes = new QuestPdfCvExporter().Export(document);
		return ComputeFromPdf(pdfBytes);
	}

	public static string ComputeFromPdf(byte[] pdfBytes)
	{
		var builder = new StringBuilder();
		using (var stream = new MemoryStream(pdfBytes))
		using (var pdf = PdfDocument.Open(stream))
		{
			// Position-free on purpose: concatenate the raw glyph characters in content-stream
			// order across all pages. Coordinates and pagination differ by platform font metrics;
			// the character sequence does not. See the class summary.
			foreach (var page in pdf.GetPages())
			{
				foreach (var letter in page.Letters)
				{
					builder.Append(letter.Value);
				}
			}
		}

		var hash = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
		return Convert.ToHexString(hash);
	}

	/// <summary>Signatures for every template, ordered by enum declaration.</summary>
	public static IReadOnlyList<(CvExportTemplateId TemplateId, string Signature)> ComputeAll() =>
		Enum.GetValues<CvExportTemplateId>()
			.Select(id => (id, Compute(id)))
			.ToList();
}
