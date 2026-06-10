using ReVitae.Core.Export;
using ReVitae.Core.Export.Fixtures;
using ReVitae.Core.Export.Images;
using ReVitae.Core.Export.Pdf;
using SixLabors.ImageSharp;

// Renders every CV template to a PNG (page 1, and page 2 when present) using the same
// QuestPDF + Docnet engines the app uses, so the output matches the real export/preview.
// Usage: dotnet run --project scripts/GenerateTemplatePreviews [outputDir] [templateFilter]

var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var outputDir = args.Length > 0 ? args[0] : Path.Combine(repoRoot, "template-previews");
var filter = args.Length > 1 ? args[1] : null;
Directory.CreateDirectory(outputDir);

var exporter = new QuestPdfCvExporter();
var rasterizer = new DocnetPdfPageRasterizer();

var templates = Enum.GetValues<CvExportTemplateId>()
	.Where(id => filter is null || id.ToString().Contains(filter, StringComparison.OrdinalIgnoreCase))
	.ToList();

Console.WriteLine($"Rendering {templates.Count} templates to {outputDir}");

var ordinal = 0;
foreach (var templateId in templates)
{
	ordinal++;
	try
	{
		var document = JohnDoeMinimalArchitectCvDataset.CreateDocument(templateId);
		var pdfBytes = exporter.Export(document);
		var pageCount = rasterizer.GetPageCount(pdfBytes);
		var pagesToRender = Math.Min(pageCount, 2);

		for (var page = 0; page < pagesToRender; page++)
		{
			using var image = rasterizer.RenderPage(pdfBytes, page, CvImageExportScale.Standard);
			var suffix = pageCount > 1 ? $"-p{page + 1}" : string.Empty;
			var fileName = $"{ordinal:D2}-{templateId}{suffix}.png";
			image.SaveAsPng(Path.Combine(outputDir, fileName));
		}

		Console.WriteLine($"  [{ordinal:D2}] {templateId} — {pageCount} page(s), {pdfBytes.Length:N0} bytes");
	}
	catch (Exception ex)
	{
		Console.WriteLine($"  [{ordinal:D2}] {templateId} — FAILED: {ex.Message}");
	}
}

Console.WriteLine("Done.");
