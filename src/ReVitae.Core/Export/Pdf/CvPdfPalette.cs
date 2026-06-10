namespace ReVitae.Core.Export.Pdf;

/// <summary>
/// Named constants for colours reused across multiple PDF templates (047 T8). Only genuinely
/// shared, semantically-consistent neutrals live here; per-template brand/accent colours stay
/// with their template. Values are the exact hex strings previously inlined, so rendered output
/// is byte-identical — this is a readability/single-definition refactor, not a re-style.
/// </summary>
public static class CvPdfPalette
{
	/// <summary>Opaque white — text on dark bands, content backgrounds.</summary>
	public const string White = "#FFFFFF";

	/// <summary>Opaque black — default headings/text where set explicitly.</summary>
	public const string Black = "#000000";

	/// <summary>Muted light grey for secondary text/titles on dark sidebars.</summary>
	public const string MutedOnDark = "#E8E8E8";

	/// <summary>Neutral grey used for initials-avatar backgrounds on light sidebars.</summary>
	public const string AvatarNeutral = "#B8B8B8";
}
