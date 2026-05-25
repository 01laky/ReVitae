using ReVitae.Core.Export.Pdf;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Export;

public static class CvDocumentExporter
{
	private static readonly ICvPdfExporter PdfExporter = new QuestPdfCvExporter();

	public static CvExportResult Export(CvExportDocument document, CvExportSourceData source, CvExportFormat format, Stream output)
	{
		if (document is null)
		{
			return new CvExportResult(false, TranslationKeys.ExportFailed);
		}

		if (output is null)
		{
			return new CvExportResult(false, TranslationKeys.ExportFailed);
		}

		try
		{
			switch (format)
			{
				case CvExportFormat.Pdf:
					PdfExporter.Export(document, output);
					break;
				case CvExportFormat.Docx:
					CvVisualExportWriter.WriteDocx(document, output);
					break;
				case CvExportFormat.Odt:
					CvVisualExportWriter.WriteOdt(document, output);
					break;
				case CvExportFormat.Rtf:
					CvVisualExportWriter.WriteRtf(document, output);
					break;
				case CvExportFormat.Html:
					CvVisualExportWriter.WriteHtml(document, output);
					break;
				case CvExportFormat.Markdown:
					CvVisualExportWriter.WriteMarkdown(document, output);
					break;
				case CvExportFormat.Txt:
					CvVisualExportWriter.WritePlainText(document, output);
					break;
				case CvExportFormat.Latex:
					CvVisualExportWriter.WriteLatex(document, output);
					break;
				case CvExportFormat.RevitaeJson:
					CvStructuredExportWriter.WriteRevitaeJson(source, output);
					break;
				case CvExportFormat.JsonResume:
					CvStructuredExportWriter.WriteJsonResume(source, output);
					break;
				case CvExportFormat.Yaml:
					CvStructuredExportWriter.WriteYaml(source, output);
					break;
				case CvExportFormat.EuropassXml:
					CvStructuredExportWriter.WriteEuropassXml(source, output);
					break;
				case CvExportFormat.HrXml:
					CvStructuredExportWriter.WriteHrXml(source, output);
					break;
				case CvExportFormat.Csv:
					CvStructuredExportWriter.WriteCsv(source, output, ',');
					break;
				case CvExportFormat.Tsv:
					CvStructuredExportWriter.WriteCsv(source, output, '\t');
					break;
				default:
					return new CvExportResult(false, TranslationKeys.ExportFailed);
			}

			return new CvExportResult(true);
		}
		catch
		{
			return new CvExportResult(false, TranslationKeys.ExportFailed);
		}
	}
}
