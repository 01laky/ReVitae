using System.IO.Compression;
using System.Text;

namespace ReVitae.Tests.Import;

internal static class MinimalOdtWriter
{
	public static byte[] CreateWithParagraphs(params string[] paragraphs)
	{
		var paragraphMarkup = string.Join(
			string.Empty,
			paragraphs.Select(line => $"<text:p>{EscapeParagraphText(line)}</text:p>"));

		var fullContent = $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <office:document-content xmlns:office="urn:oasis:names:tc:opendocument:xmlns:office:1.0"
                                     xmlns:text="urn:oasis:names:tc:opendocument:xmlns:text:1.0">
              <office:body>
                <office:text>
                  {paragraphMarkup}
                </office:text>
              </office:body>
            </office:document-content>
            """;

		using var stream = new MemoryStream();
		using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
		{
			var mimeEntry = archive.CreateEntry("mimetype", CompressionLevel.NoCompression);
			using (var mimeWriter = new StreamWriter(mimeEntry.Open(), new UTF8Encoding(false)))
			{
				mimeWriter.Write("application/vnd.oasis.opendocument.text");
			}

			var contentEntry = archive.CreateEntry("content.xml", CompressionLevel.Fastest);
			using (var contentWriter = new StreamWriter(contentEntry.Open(), new UTF8Encoding(false)))
			{
				contentWriter.Write(fullContent);
			}
		}

		return stream.ToArray();
	}

	private static string EscapeParagraphText(string line)
	{
		return line
			.Replace("&", "&amp;", StringComparison.Ordinal)
			.Replace("<", "&lt;", StringComparison.Ordinal)
			.Replace(">", "&gt;", StringComparison.Ordinal);
	}
}
