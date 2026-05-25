using ReVitae.Core.Import;
using ReVitae.Core.Import.Extraction;
using ReVitae.Core.Localization;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace ReVitae.Core.Import.Pdf;

public sealed class PdfPigTextExtractor : IPdfTextExtractor
{
	public PdfTextExtractionResult Extract(string filePath)
	{
		CvImportDiagnosticsLogger.LogStep("pdfpig", $"Extract started: {Path.GetFileName(filePath)}");

		if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
		{
			CvImportDiagnosticsLogger.LogStep("pdfpig", "File not found");
			return new PdfTextExtractionResult(false, string.Empty, 0, TranslationKeys.ImportErrorFileNotFound);
		}

		try
		{
			using var document = PdfDocument.Open(filePath);
			CvImportDiagnosticsLogger.LogStep("pdfpig", $"Opened PDF: {document.NumberOfPages} page(s)");

			var (metadataTemplateId, metadataIsReVitae) = ReVitaePdfMetadataReader.Read(document);
			var layoutProfile = metadataTemplateId is { } templateId
				? ReVitaePdfLayoutProfiles.Get(templateId)
				: ReVitaePdfLayoutProfiles.DefaultTwoColumn;

			var mainChunks = new List<string>();
			string? deferredSidebar = null;
			var hyperlinkUrls = new List<string>();
			var pageIndex = 0;

			foreach (var page in document.GetPages())
			{
				pageIndex++;
				var (mainText, sidebarText) = ExtractPageColumns(page, pageIndex, layoutProfile);
				if (!string.IsNullOrWhiteSpace(mainText))
				{
					mainChunks.Add(mainText);
				}

				if (!string.IsNullOrWhiteSpace(sidebarText) && deferredSidebar is null)
				{
					deferredSidebar = sidebarText;
					CvImportDiagnosticsLogger.LogStep(
						"pdfpig",
						$"Page {pageIndex}: deferred sidebar captured ({sidebarText.Length} chars)");
				}

				foreach (var hyperlink in page.GetHyperlinks())
				{
					if (string.IsNullOrWhiteSpace(hyperlink.Uri))
					{
						continue;
					}

					var uri = hyperlink.Uri.Trim();
					if (!hyperlinkUrls.Contains(uri, StringComparer.OrdinalIgnoreCase))
					{
						hyperlinkUrls.Add(uri);
					}
				}
			}

			var usesDeferredSidebar = !string.IsNullOrWhiteSpace(deferredSidebar) && mainChunks.Count > 1;
			var chunks = new List<string>(mainChunks);
			if (!string.IsNullOrWhiteSpace(deferredSidebar))
			{
				chunks.Add(deferredSidebar);
				CvImportDiagnosticsLogger.LogStep(
					"pdfpig",
					$"Appended deferred sidebar at end ({deferredSidebar.Length} chars)");
			}

			var text = string.Join("\n\n", chunks).Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				CvImportDiagnosticsLogger.LogStep("pdfpig", "No extractable text — empty PDF");
				return new PdfTextExtractionResult(false, string.Empty, document.NumberOfPages, TranslationKeys.ImportErrorEmptyPdf);
			}

			var hints = ReVitaePdfExportHintsBuilder.Build(
				metadataTemplateId,
				metadataIsReVitae,
				text,
				usesDeferredSidebar);

			if (hints.TemplateId is { } resolvedTemplateId)
			{
				layoutProfile = ReVitaePdfLayoutProfiles.Get(resolvedTemplateId);
			}

			CvImportDiagnosticsLogger.LogStep(
				"pdfpig",
				$"Success: {text.Length} chars, {text.Count(c => !char.IsWhiteSpace(c))} non-whitespace, " +
				$"mainChunks={mainChunks.Count}, hyperlinks={hyperlinkUrls.Count}, " +
				$"reVitae={hints.IsLikelyReVitaeExport}, template={hints.TemplateId?.ToString() ?? "none"}, " +
				$"split={layoutProfile.SidebarWidthRatio?.ToString("F2") ?? "single-column"}");

			return new PdfTextExtractionResult(
				true,
				text,
				document.NumberOfPages,
				null,
				hyperlinkUrls,
				hints);
		}
		catch (UglyToad.PdfPig.Exceptions.PdfDocumentEncryptedException)
		{
			CvImportDiagnosticsLogger.LogStep("pdfpig", "Password-protected PDF");
			return new PdfTextExtractionResult(false, string.Empty, 0, TranslationKeys.ImportErrorPasswordProtected);
		}
		catch (Exception ex)
		{
			CvImportDiagnosticsLogger.LogStep("pdfpig", $"Extract failed: {ex.GetType().Name}: {ex.Message}");
			return new PdfTextExtractionResult(false, string.Empty, 0, TranslationKeys.ImportErrorUnreadablePdf);
		}
	}

	private static (string MainText, string? SidebarText) ExtractPageColumns(
		Page page,
		int pageIndex,
		ReVitaePdfLayoutProfile layoutProfile)
	{
		var words = page.GetWords().ToArray();
		if (words.Length == 0)
		{
			var fallback = page.Text;
			CvImportDiagnosticsLogger.LogStep(
				"pdfpig",
				$"Page {pageIndex}: no words, fallback page.Text ({fallback.Length} chars)");
			return (fallback, null);
		}

		if (layoutProfile.ColumnKind is ReVitaePdfColumnKind.SingleColumn)
		{
			var single = ExtractColumnText(words);
			CvImportDiagnosticsLogger.LogStep(
				"pdfpig",
				$"Page {pageIndex}: single-column profile, {words.Length} words → {single.Length} chars");
			return (single, null);
		}

		var columns = SplitIntoColumns(page, words, layoutProfile);
		if (columns.Count != 2)
		{
			var single = ExtractColumnText(columns[0]);
			CvImportDiagnosticsLogger.LogStep(
				"pdfpig",
				$"Page {pageIndex}: single column, {words.Length} words → {single.Length} chars");
			return (single, null);
		}

		var ordered = columns.OrderByDescending(ColumnWidth).ToArray();
		var main = ExtractColumnText(ordered[0]);
		var sidebar = ExtractColumnText(ordered[1]);
		var splitRatio = layoutProfile.SidebarWidthRatio ?? ReVitaePdfLayoutProfiles.DefaultSidebarRatio;
		CvImportDiagnosticsLogger.LogStep(
			"pdfpig",
			$"Page {pageIndex}: two columns — main={main.Length} chars, sidebar={sidebar.Length} chars " +
			$"(splitX={page.Width * splitRatio:F0}, kind={layoutProfile.ColumnKind})");
		return (main, sidebar);
	}

	private static double ColumnWidth(IReadOnlyList<Word> words) =>
		words.Max(word => word.BoundingBox.Right) - words.Min(word => word.BoundingBox.Left);

	private static IReadOnlyList<IReadOnlyList<Word>> SplitIntoColumns(
		Page page,
		IReadOnlyList<Word> words,
		ReVitaePdfLayoutProfile layoutProfile)
	{
		var pageWidth = page.Width;
		if (pageWidth < 80)
		{
			return [words];
		}

		var splitRatio = layoutProfile.SidebarWidthRatio ?? ReVitaePdfLayoutProfiles.DefaultSidebarRatio;
		var splitX = pageWidth * splitRatio;
		var left = words.Where(word => WordCenterX(word) <= splitX).ToArray();
		var right = words.Where(word => WordCenterX(word) > splitX).ToArray();

		if (left.Length >= 3 && right.Length >= 3)
		{
			return [left, right];
		}

		return [words];
	}

	private static double WordCenterX(Word word) =>
		(word.BoundingBox.Left + word.BoundingBox.Right) / 2;

	private static string ExtractColumnText(IReadOnlyList<Word> words)
	{
		var lines = new List<List<Word>>();
		foreach (var word in words
					 .OrderByDescending(word => word.BoundingBox.Bottom)
					 .ThenBy(word => word.BoundingBox.Left))
		{
			var line = lines.FirstOrDefault(candidate =>
				Math.Abs(candidate[0].BoundingBox.Bottom - word.BoundingBox.Bottom) < 3);
			if (line is null)
			{
				lines.Add([word]);
			}
			else
			{
				line.Add(word);
			}
		}

		return string.Join(
			"\n",
			lines.Select(line => string.Join(
				" ",
				line.OrderBy(word => word.BoundingBox.Left).Select(word => word.Text))));
	}
}
