using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ReVitae.Core.Export.Fixtures;
using ReVitae.Core.Export.Pdf;
using UglyToad.PdfPig;

namespace ReVitae.Core.Export;

/// <summary>
/// Computes a deterministic, cross-platform <b>layout signature</b> for a CV template (047 QG1).
/// Renders the template with the fixed minimal demo dataset, then hashes the exported PDF's
/// text content and rounded word positions read back via PdfPig. The signature is independent
/// of the PDF's non-deterministic metadata (creation date / id) and of pixel rendering, so it
/// is stable run-to-run and acts as a golden regression oracle for layout-affecting refactors.
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
			builder.Append(pdf.NumberOfPages.ToString(CultureInfo.InvariantCulture)).Append('\n');
			foreach (var page in pdf.GetPages())
			{
				builder.Append("P").Append(page.Number).Append('\n');
				foreach (var word in page.GetWords())
				{
					// Round coordinates so sub-point float noise never flips the hash, while
					// real position changes (a misaligned element) still do.
					var x = (int)Math.Round(word.BoundingBox.Left);
					var y = (int)Math.Round(word.BoundingBox.Bottom);
					builder.Append(word.Text).Append('@').Append(x).Append(',').Append(y).Append('\n');
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
