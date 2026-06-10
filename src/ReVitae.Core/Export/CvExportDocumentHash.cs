using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ReVitae.Core.Export;

/// <summary>
/// Stable content hash of a <see cref="CvExportDocument"/> (047 T4) — used to skip redundant
/// preview re-renders when the document has not changed. Two documents with identical content
/// hash equal; any content change produces a different hash. Falls back to a unique value if the
/// document cannot be serialized, so callers re-render rather than show stale output.
/// </summary>
public static class CvExportDocumentHash
{
	public static string Compute(CvExportDocument document)
	{
		ArgumentNullException.ThrowIfNull(document);

		try
		{
			var json = JsonSerializer.Serialize(document);
			var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
			return Convert.ToHexString(hash);
		}
		catch (NotSupportedException)
		{
			return Guid.NewGuid().ToString("N");
		}
	}
}
