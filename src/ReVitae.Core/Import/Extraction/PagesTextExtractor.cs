using System.IO.Compression;
using ReVitae.Core.Import.Pdf;

namespace ReVitae.Core.Import.Extraction;

/// <summary>Apple Pages bundles may embed preview PDF snapshots that PdfPig can read without OCR.</summary>
public sealed class PagesTextExtractor : ICvTextExtractor
{
	public CvTextExtractionResult Extract(string filePath)
	{
		if (ImportExtractorGuards.TryRejectMissing(filePath, out var fail))
		{
			return fail;
		}

		try
		{
			using ZipArchive archive = ZipFile.OpenRead(filePath);
			ZipArchiveEntry? preview = archive.Entries.FirstOrDefault(entry =>
				string.Equals(Path.GetFileName(entry.FullName), "preview.pdf", StringComparison.OrdinalIgnoreCase));

			if (preview is null)
			{
				return new CvTextExtractionResult(false, string.Empty, ReVitae.Core.Localization.TranslationKeys.ImportErrorUnsupportedFormat);
			}

			var tempPdf = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}_pages_preview.pdf");
			try
			{
				using (var pdfStream = File.Create(tempPdf))
				using (var entryStream = preview.Open())
				{
					entryStream.CopyTo(pdfStream);
				}

				ICvTextExtractor adapter = new PdfTextExtractorAdapter(new PdfPigTextExtractor());
				return adapter.Extract(tempPdf);
			}
			finally
			{
				TryDelete(tempPdf);
			}
		}
		catch (Exception)
		{
			return new CvTextExtractionResult(false, string.Empty, ReVitae.Core.Localization.TranslationKeys.ImportErrorUnreadableDocument);
		}
	}

	private static void TryDelete(string path)
	{
		try
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
		catch
		{
			// Best effort cleanup — temp files are benign if left behind.
		}
	}
}
