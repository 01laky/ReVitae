namespace ReVitae.Core.Import;

public interface ICvFormatImporter
{
	CvImportFormat Format { get; }

	CvImportResult Import(string filePath);
}
