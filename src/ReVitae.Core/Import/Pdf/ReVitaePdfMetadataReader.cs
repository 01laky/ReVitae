using ReVitae.Core.Export;
using UglyToad.PdfPig;

namespace ReVitae.Core.Import.Pdf;

public static class ReVitaePdfMetadataReader
{
	public const string ProducerName = "ReVitae";
	public const string TemplateKeywordPrefix = "template:";

	public static (CvExportTemplateId? TemplateId, bool IsReVitaeProducer) Read(PdfDocument document)
	{
		var information = document.Information;
		var isReVitae = IsReVitaeProducer(information.Producer)
			|| IsReVitaeCreator(information.Creator);

		var templateId = TryParseTemplateId(information.Keywords);
		return (templateId, isReVitae);
	}

	public static bool IsReVitaeProducer(string? producer) =>
		string.Equals(producer?.Trim(), ProducerName, StringComparison.OrdinalIgnoreCase);

	public static bool IsReVitaeCreator(string? creator) =>
		creator?.Trim().StartsWith($"{ProducerName}/", StringComparison.OrdinalIgnoreCase) == true;

	public static CvExportTemplateId? TryParseTemplateId(string? keywords)
	{
		if (string.IsNullOrWhiteSpace(keywords))
		{
			return null;
		}

		foreach (var token in keywords.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			if (!token.StartsWith(TemplateKeywordPrefix, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			var templateName = token[TemplateKeywordPrefix.Length..];
			if (Enum.TryParse<CvExportTemplateId>(templateName, ignoreCase: false, out var templateId))
			{
				return templateId;
			}
		}

		return null;
	}
}
