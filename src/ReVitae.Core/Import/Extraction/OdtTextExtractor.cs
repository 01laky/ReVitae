using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using ReVitae.Core.Import.Xml;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Import.Extraction;

public sealed class OdtTextExtractor : ICvTextExtractor
{
	public CvTextExtractionResult Extract(string filePath)
	{
		if (ImportExtractorGuards.TryRejectMissing(filePath, out var fail))
		{
			return fail;
		}

		try
		{
			using var zip = ZipFile.OpenRead(filePath);
			var contentEntry = zip.Entries.FirstOrDefault(e =>
				string.Equals(Path.GetFileName(e.FullName), "content.xml", StringComparison.OrdinalIgnoreCase));
			if (contentEntry is null)
			{
				return new CvTextExtractionResult(false, string.Empty, TranslationKeys.ImportErrorUnreadableDocument);
			}

			XDocument xml;
			using (var reader = contentEntry.Open())
			{
				xml = SecureXmlReaderFactory.LoadXDocument(reader);
			}

			var buffer = new StringBuilder();
			foreach (var element in xml.Descendants())
			{
				if (!string.Equals(element.Name.LocalName, "p", StringComparison.OrdinalIgnoreCase)
					&& !string.Equals(element.Name.LocalName, "h", StringComparison.OrdinalIgnoreCase))
				{
					if (element.Name.LocalName.Contains("Cell", StringComparison.OrdinalIgnoreCase)
						|| string.Equals(element.Name.LocalName, "table-cell", StringComparison.OrdinalIgnoreCase))
					{
						var cellText = element.Value.Trim();
						if (!string.IsNullOrWhiteSpace(cellText))
						{
							buffer.Append('\t').AppendLine(cellText);
						}
					}

					continue;
				}

				var line = element.Value.Trim();
				if (!string.IsNullOrWhiteSpace(line))
				{
					buffer.AppendLine(line);
				}
			}

			var flattened = CvTextNormalizer.Normalize(buffer.ToString()).Trim();
			return string.IsNullOrWhiteSpace(flattened)
				? new CvTextExtractionResult(false, string.Empty, TranslationKeys.ImportErrorEmptyDocument)
				: new CvTextExtractionResult(true, flattened, null);
		}
		catch (Exception)
		{
			return new CvTextExtractionResult(false, string.Empty, TranslationKeys.ImportErrorUnreadableDocument);
		}
	}
}
