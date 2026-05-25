using ReVitae.Core.Localization;

namespace ReVitae.Core.Export;

public static class CvExportSaveDialogDefaults
{
	public static string GetFileTypeLabelKey(CvExportFormat format) => format switch
	{
		CvExportFormat.Pdf => TranslationKeys.ExportPdfFileType,
		CvExportFormat.Docx => TranslationKeys.ExportDocxFileType,
		CvExportFormat.Odt => TranslationKeys.ExportOdtFileType,
		CvExportFormat.Rtf => TranslationKeys.ExportRtfFileType,
		CvExportFormat.Html => TranslationKeys.ExportHtmlFileType,
		CvExportFormat.Markdown => TranslationKeys.ExportMarkdownFileType,
		CvExportFormat.Txt => TranslationKeys.ExportTxtFileType,
		CvExportFormat.Latex => TranslationKeys.ExportLatexFileType,
		CvExportFormat.RevitaeJson => TranslationKeys.ExportRevitaeJsonFileType,
		CvExportFormat.JsonResume => TranslationKeys.ExportJsonResumeFileType,
		CvExportFormat.Yaml => TranslationKeys.ExportYamlFileType,
		CvExportFormat.EuropassXml => TranslationKeys.ExportEuropassXmlFileType,
		CvExportFormat.HrXml => TranslationKeys.ExportHrXmlFileType,
		CvExportFormat.Csv => TranslationKeys.ExportCsvFileType,
		CvExportFormat.Tsv => TranslationKeys.ExportTsvFileType,
		_ => TranslationKeys.ExportSaveDialogTitle
	};

	public static IReadOnlyList<string> GetPatterns(CvExportFormat format) => format switch
	{
		CvExportFormat.Pdf => ["*.pdf"],
		CvExportFormat.Docx => ["*.docx"],
		CvExportFormat.Odt => ["*.odt"],
		CvExportFormat.Rtf => ["*.rtf"],
		CvExportFormat.Html => ["*.html"],
		CvExportFormat.Markdown => ["*.md"],
		CvExportFormat.Txt => ["*.txt"],
		CvExportFormat.Latex => ["*.tex"],
		CvExportFormat.RevitaeJson => ["*.revitae.json"],
		CvExportFormat.JsonResume => ["*.json"],
		CvExportFormat.Yaml => ["*.yaml", "*.yml"],
		CvExportFormat.EuropassXml => ["*.xml"],
		CvExportFormat.HrXml => ["*.xml"],
		CvExportFormat.Csv => ["*.csv"],
		CvExportFormat.Tsv => ["*.tsv"],
		_ => ["*.*"]
	};

	public static IReadOnlyList<string> GetMimeTypes(CvExportFormat format) => format switch
	{
		CvExportFormat.Pdf => ["application/pdf"],
		CvExportFormat.Docx => ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
		CvExportFormat.Odt => ["application/vnd.oasis.opendocument.text"],
		CvExportFormat.Rtf => ["application/rtf"],
		CvExportFormat.Html => ["text/html"],
		CvExportFormat.Markdown => ["text/markdown"],
		CvExportFormat.Txt => ["text/plain"],
		CvExportFormat.Latex => ["application/x-tex"],
		CvExportFormat.RevitaeJson => ["application/json"],
		CvExportFormat.JsonResume => ["application/json"],
		CvExportFormat.Yaml => ["application/x-yaml"],
		CvExportFormat.EuropassXml => ["application/xml"],
		CvExportFormat.HrXml => ["application/xml"],
		CvExportFormat.Csv => ["text/csv"],
		CvExportFormat.Tsv => ["text/tab-separated-values"],
		_ => []
	};

	public static string GetDefaultExtension(CvExportFormat format) =>
		CvExportFormatCatalog.GetExtension(format).TrimStart('.');
}
