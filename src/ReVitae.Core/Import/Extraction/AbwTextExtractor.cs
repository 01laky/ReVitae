using System.IO.Compression;
using System.Text;
using ReVitae.Core.Import.Xml;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Import.Extraction;

public sealed class AbwTextExtractor : ICvTextExtractor
{
	public CvTextExtractionResult Extract(string filePath)
	{
		if (ImportExtractorGuards.TryRejectMissing(filePath, out var fail))
		{
			return fail;
		}

		try
		{
			var markup = DecodePayload(File.ReadAllBytes(filePath));
			var xml = SecureXmlReaderFactory.ParseDocument(markup);
			var paragraphValues = xml.Descendants()
				.Where(e => string.Equals(e.Name.LocalName, "p", StringComparison.OrdinalIgnoreCase))
				.Select(e => e.Value.Trim())
				.Where(line => line.Length > 0);

			var buffer = string.Join(Environment.NewLine, paragraphValues);
			var normalized = CvTextNormalizer.Normalize(buffer).Trim();
			return string.IsNullOrWhiteSpace(normalized)
				? new CvTextExtractionResult(false, string.Empty, TranslationKeys.ImportErrorEmptyDocument)
				: new CvTextExtractionResult(true, normalized, null);
		}
		catch (Exception)
		{
			return new CvTextExtractionResult(false, string.Empty, TranslationKeys.ImportErrorUnreadableDocument);
		}
	}

	private static string DecodePayload(ReadOnlySpan<byte> payload)
	{
		if (payload.Length >= 2 && payload[0] == 0x1F && payload[1] == 0x8B)
		{
			using var compressed = new MemoryStream(payload.ToArray());
			using var gzip = new GZipStream(compressed, CompressionMode.Decompress);
			using var output = new MemoryStream();
			gzip.CopyTo(output);
			return Encoding.UTF8.GetString(output.ToArray());
		}

		return Encoding.UTF8.GetString(payload);
	}
}
