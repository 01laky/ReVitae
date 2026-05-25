namespace ReVitae.Core.Import;

/// <summary>Detected import dialect. Used for routing to <see cref="ICvFormatImporter"/>.</summary>
public enum CvImportFormat
{
	Unknown = 0,

	Pdf = 1,
	Docx = 2,
	Doc = 3,
	Odt = 4,
	Rtf = 5,
	PlainText = 6,
	Markdown = 7,
	Html = 8,
	Latex = 9,
	Abw = 10,

	JsonResume = 20,
	ReVitaeJson = 21,
	EuropassXml = 22,
	HrXml = 23,

	YamlCv = 30,
	CsvTabular = 31,

	Wps = 40,
	Pages = 41,
	RasterImage = 42
}
