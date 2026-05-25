using ReVitae.Core.Import.Extraction;

namespace ReVitae.Core.Import;

public abstract class TextCvFormatImporterBase : ICvFormatImporter
{
	private readonly ICvTextExtractor _extractor;

	protected TextCvFormatImporterBase(CvImportFormat format, ICvTextExtractor extractor)
	{
		Format = format;
		_extractor = extractor;
	}

	public CvImportFormat Format { get; }

	public CvImportResult Import(string filePath)
	{
		return CvTextImportFlows.FromExtractor(_extractor, filePath);
	}

	public CvTextImportAttempt TryImportDetailed(string filePath) =>
		CvTextImportFlows.TryFromExtractor(_extractor, filePath, Format);
}
