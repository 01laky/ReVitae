namespace ReVitae.Core.Export.Pdf;

using System.Reflection;
using QuestPDF.Drawing;

/// <summary>
/// Registers the bundled <b>Arimo</b> font (Apache-2.0, metric-compatible with Arial) with QuestPDF
/// so PDF export is byte-deterministic across Windows / macOS / Linux.
/// <para>
/// The templates previously used the family name <c>"Arial"</c>, which resolves to whatever the host
/// happens to have installed. The Linux CI runner has no Arial, so SkiaSharp substituted a different
/// face — shifting text metrics, line wrapping and pagination. That made the same CV export
/// differently per platform, which broke the cross-platform render golden (047 QG1) and the PDF
/// re-import round-trip (the import parser could no longer locate dates in the reflowed layout).
/// Embedding the font and forcing every template onto <see cref="FamilyName"/> removes the host
/// dependency entirely.
/// </para>
/// </summary>
public static class CvPdfFonts
{
	/// <summary>The font family every PDF template renders with.</summary>
	public const string FamilyName = "Arimo";

	private static readonly object Gate = new();
	private static bool _registered;

	/// <summary>
	/// Registers the embedded Arimo faces with QuestPDF exactly once per process. Idempotent and
	/// thread-safe; safe to call from every export entry point.
	/// </summary>
	public static void EnsureRegistered()
	{
		if (_registered)
		{
			return;
		}

		lock (Gate)
		{
			if (_registered)
			{
				return;
			}

			var assembly = typeof(CvPdfFonts).Assembly;
			foreach (var resourceName in assembly.GetManifestResourceNames())
			{
				if (resourceName.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) &&
					resourceName.Contains(".Fonts.Arimo", StringComparison.Ordinal))
				{
					using var stream = assembly.GetManifestResourceStream(resourceName);
					if (stream is not null)
					{
						FontManager.RegisterFont(stream);
					}
				}
			}

			_registered = true;
		}
	}
}
