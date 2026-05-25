using ReVitae.Core.Import.Importers;

namespace ReVitae.Core.Import;

internal static class CvFormatImporterRegistry
{
	private static readonly IReadOnlyDictionary<CvImportFormat, ICvFormatImporter> Importers =
		new Dictionary<CvImportFormat, ICvFormatImporter>
		{
			[CvImportFormat.Pdf] = new PdfCvFormatImporter(),
			[CvImportFormat.Docx] = new DocxCvFormatImporter(),
			[CvImportFormat.Doc] = new DocCvFormatImporter(),
			[CvImportFormat.Odt] = new OdtCvFormatImporter(),
			[CvImportFormat.Rtf] = new RtfCvFormatImporter(),
			[CvImportFormat.PlainText] = new PlainTextCvFormatImporter(),
			[CvImportFormat.Markdown] = new MarkdownCvFormatImporter(),
			[CvImportFormat.Html] = new HtmlCvFormatImporter(),
			[CvImportFormat.Latex] = new LatexCvFormatImporter(),
			[CvImportFormat.Abw] = new AbwCvFormatImporter(),
			[CvImportFormat.Pages] = new PagesCvFormatImporter(),
			[CvImportFormat.Wps] = new WpsCvFormatImporter(),
			[CvImportFormat.JsonResume] = new JsonResumeCvFormatImporter(),
			[CvImportFormat.ReVitaeJson] = new ReVitaeJsonCvFormatImporter(),
			[CvImportFormat.YamlCv] = new YamlCvFormatImporter(),
			[CvImportFormat.CsvTabular] = new TabularCvFormatImporter(),
			[CvImportFormat.EuropassXml] = new EuropassXmlCvFormatImporter(),
			[CvImportFormat.HrXml] = new HrXmlCvFormatImporter(),
			[CvImportFormat.RasterImage] = new RasterImageCvFormatImporter()
		};

	public static ICvFormatImporter? Get(CvImportFormat format)
	{
		return Importers.TryGetValue(format, out var importer) ? importer : null;
	}
}
