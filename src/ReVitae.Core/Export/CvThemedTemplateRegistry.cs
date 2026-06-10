namespace ReVitae.Core.Export;

using ReVitae.Core.Projects;

public sealed record CvThemedTemplateDefinition(
	CvExportTemplateId Id,
	CvThemedTemplateLayoutKind Layout,
	string AccentColor,
	string SidebarColor,
	string HeaderColor,
	string NameKey,
	string NameEnglish,
	string DescriptionKey,
	string DescriptionEnglish);

public static class CvThemedTemplateRegistry
{
	private static readonly IReadOnlyList<CvThemedTemplateDefinition> Definitions =
	[
		Theme(CvExportTemplateId.TealProfessional, CvThemedTemplateLayoutKind.LeftSidebarLight, "#008080", "#E8F4F4", "#008080",
			"Teal Professional", "Light sidebar with teal accents — popular on Canva-style corporate CVs."),
		Theme(CvExportTemplateId.BurgundyExecutive, CvThemedTemplateLayoutKind.FullSidebarDark, "#722F37", "#722F37", "#722F37",
			"Burgundy Executive", "Full-height burgundy sidebar with white type — executive résumé style."),
		Theme(CvExportTemplateId.SlateMinimal, CvThemedTemplateLayoutKind.MinimalCenter, "#708090", "#ECEFF1", "#708090",
			"Slate Minimal", "Centered single column with slate gray bands — clean minimalist look."),
		Theme(CvExportTemplateId.CoralCreative, CvThemedTemplateLayoutKind.TopHeaderBand, "#FF7F50", "#FFF5F0", "#FF7F50",
			"Coral Creative", "Warm coral header band with airy white body — creative portfolio CV."),
		Theme(CvExportTemplateId.MintFresh, CvThemedTemplateLayoutKind.LeftSidebarLight, "#3CB371", "#E8F8EF", "#3CB371",
			"Mint Fresh", "Mint-green sidebar accents on soft mint panel — fresh graduate style."),
		Theme(CvExportTemplateId.CharcoalBold, CvThemedTemplateLayoutKind.TopHeaderSplit, "#36454F", "#F5F5F5", "#36454F",
			"Charcoal Bold", "Charcoal header split with photo and contact — bold modern header."),
		Theme(CvExportTemplateId.LavenderSoft, CvThemedTemplateLayoutKind.RightSidebarLight, "#9370DB", "#F3EFFF", "#9370DB",
			"Lavender Soft", "Right sidebar with lavender accents — soft designer résumé."),
		Theme(CvExportTemplateId.RubyAccent, CvThemedTemplateLayoutKind.AccentBarLeft, "#E0115F", "#FCEEF3", "#E0115F",
			"Ruby Accent", "Ruby vertical accent bar with structured sections — marketing CV style."),
		Theme(CvExportTemplateId.OceanWave, CvThemedTemplateLayoutKind.TopHeaderBand, "#0077BE", "#E8F4FC", "#0077BE",
			"Ocean Wave", "Ocean-blue header with contact strip — nautical professional theme."),
		Theme(CvExportTemplateId.TerracottaWarm, CvThemedTemplateLayoutKind.PhotoLeftAccent, "#E2725B", "#FAF0EB", "#E2725B",
			"Terracotta Warm", "Photo-left layout with terracotta name accent — warm European CV."),
		Theme(CvExportTemplateId.GraphiteTech, CvThemedTemplateLayoutKind.LeftSidebarLight, "#383838", "#EEEEEE", "#383838",
			"Graphite Tech", "Graphite sidebar and dark name band — developer / tech résumé."),
		Theme(CvExportTemplateId.EmeraldMedical, CvThemedTemplateLayoutKind.FullSidebarDark, "#2E8B57", "#2E8B57", "#2E8B57",
			"Emerald Medical", "Emerald full sidebar — healthcare and clinical CV style."),
		Theme(CvExportTemplateId.CrimsonAcademic, CvThemedTemplateLayoutKind.MinimalCenter, "#B22222", "#FDF2F2", "#B22222",
			"Crimson Academic", "Centered academic layout with crimson section titles."),
		Theme(CvExportTemplateId.SageCalm, CvThemedTemplateLayoutKind.RightSidebarLight, "#9DC183", "#F2F7ED", "#9DC183",
			"Sage Calm", "Sage-green right sidebar — wellness and coaching résumé."),
		Theme(CvExportTemplateId.IndigoStartup, CvThemedTemplateLayoutKind.TopHeaderSplit, "#4B0082", "#F3EEFF", "#4B0082",
			"Indigo Startup", "Indigo split header with circular photo — startup founder CV."),
		Theme(CvExportTemplateId.AmberBold, CvThemedTemplateLayoutKind.TimelineLeft, "#FFBF00", "#FFFBEB", "#FFBF00",
			"Amber Bold", "Amber timeline rail on the left — chronological résumé style."),
		Theme(CvExportTemplateId.SteelCorporate, CvThemedTemplateLayoutKind.LeftSidebarLight, "#4682B4", "#EEF4FA", "#4682B4",
			"Steel Corporate", "Steel-blue sidebar — classic corporate two-column CV."),
		Theme(CvExportTemplateId.RoseElegant, CvThemedTemplateLayoutKind.PhotoLeftAccent, "#C71585", "#FDF0F8", "#C71585",
			"Rose Elegant", "Rose accent with left photo block — elegant fashion / beauty CV."),
		Theme(CvExportTemplateId.CopperIndustrial, CvThemedTemplateLayoutKind.AccentBarLeft, "#B87333", "#FBF3EA", "#B87333",
			"Copper Industrial", "Copper accent bar — engineering and trades résumé."),
		Theme(CvExportTemplateId.AzureSky, CvThemedTemplateLayoutKind.TopHeaderBand, "#007FFF", "#EBF5FF", "#007FFF",
			"Azure Sky", "Bright azure header band — airy single-column CV."),
		Theme(CvExportTemplateId.PlumCreative, CvThemedTemplateLayoutKind.RightSidebarLight, "#8E4585", "#F8EFF7", "#8E4585",
			"Plum Creative", "Plum right sidebar with skills focus — arts and media CV."),
		Theme(CvExportTemplateId.OliveEuropean, CvThemedTemplateLayoutKind.LeftSidebarLight, "#6B8E23", "#F4F6ED", "#6B8E23",
			"Olive European", "Olive sidebar accents — European CV / Europass-inspired layout."),
		Theme(CvExportTemplateId.SandNeutral, CvThemedTemplateLayoutKind.MinimalCenter, "#C2B280", "#FAF8F3", "#C2B280",
			"Sand Neutral", "Neutral sand tones with centered name — understated consultant CV."),
		Theme(CvExportTemplateId.MidnightExecutive, CvThemedTemplateLayoutKind.FullSidebarDark, "#191970", "#191970", "#191970",
			"Midnight Executive", "Midnight-navy full sidebar — board-level executive résumé."),
		Theme(CvExportTemplateId.FrostClean, CvThemedTemplateLayoutKind.TopHeaderBand, "#5DADE2", "#F0FAFF", "#5DADE2",
			"Frost Clean", "Frost-blue header on white — crisp office / admin CV."),
		Theme(CvExportTemplateId.BrickBold, CvThemedTemplateLayoutKind.TimelineRight, "#CB4154", "#FDF0F2", "#CB4154",
			"Brick Bold", "Brick-red timeline on the right — reverse-chronological style."),
		Theme(CvExportTemplateId.WineSommelier, CvThemedTemplateLayoutKind.FullSidebarDark, "#722F37", "#5C1A24", "#722F37",
			"Wine Sommelier", "Deep wine sidebar — hospitality and F&B résumé."),
		Theme(CvExportTemplateId.HoneyWarm, CvThemedTemplateLayoutKind.PhotoLeftAccent, "#FFB347", "#FFF8EE", "#FFB347",
			"Honey Warm", "Honey-orange photo header — friendly customer-facing CV."),
		Theme(CvExportTemplateId.ArcticCool, CvThemedTemplateLayoutKind.LeftSidebarLight, "#48A9A6", "#EAF6F6", "#48A9A6",
			"Arctic Cool", "Cool teal sidebar — Scandinavian minimal CV style."),
		Theme(CvExportTemplateId.MossNature, CvThemedTemplateLayoutKind.RightSidebarLight, "#8A9A5B", "#F3F6EC", "#8A9A5B",
			"Moss Nature", "Moss-green right panel — environmental and outdoor CV."),
		Theme(CvExportTemplateId.BronzeClassic, CvThemedTemplateLayoutKind.MinimalCenter, "#CD7F32", "#FBF5EE", "#CD7F32",
			"Bronze Classic", "Bronze centered bands — traditional formal résumé."),
		Theme(CvExportTemplateId.LilacModern, CvThemedTemplateLayoutKind.TopHeaderSplit, "#C8A2C8", "#F9F3F9", "#C8A2C8",
			"Lilac Modern", "Lilac split header with rounded contact area — modern soft CV."),
		Theme(CvExportTemplateId.DenimCasual, CvThemedTemplateLayoutKind.LeftSidebarLight, "#1560BD", "#EBF2FB", "#1560BD",
			"Denim Casual", "Denim-blue sidebar — casual professional two-column CV."),
		Theme(CvExportTemplateId.CherryPop, CvThemedTemplateLayoutKind.TimelineLeft, "#DE3163", "#FDF0F4", "#DE3163",
			"Cherry Pop", "Cherry-pink timeline rail — vibrant creative résumé."),
		Theme(CvExportTemplateId.PewterFormal, CvThemedTemplateLayoutKind.AccentBarLeft, "#899499", "#F4F5F6", "#899499",
			"Pewter Formal", "Pewter accent bar — formal government / legal CV."),
		Theme(CvExportTemplateId.CyanDigital, CvThemedTemplateLayoutKind.TopHeaderBand, "#00A8CC", "#E8FAFE", "#00A8CC",
			"Cyan Digital", "Cyan header band — digital agency and UX résumé."),
		Theme(CvExportTemplateId.MahoganyTraditional, CvThemedTemplateLayoutKind.FullSidebarDark, "#C04000", "#8B3000", "#C04000",
			"Mahogany Traditional", "Mahogany full sidebar — traditional business CV."),
		Theme(CvExportTemplateId.QuartzMinimal, CvThemedTemplateLayoutKind.MinimalCenter, "#51484F", "#F5F3F4", "#51484F",
			"Quartz Minimal", "Quartz-gray centered layout — ultra-minimal single column."),
		Theme(CvExportTemplateId.FlamingoCreative, CvThemedTemplateLayoutKind.PhotoLeftAccent, "#FC8EAC", "#FFF0F4", "#FC8EAC",
			"Flamingo Creative", "Flamingo-pink photo accent — social media and content CV."),
		Theme(CvExportTemplateId.PineForest, CvThemedTemplateLayoutKind.RightSidebarLight, "#01796F", "#E8F5F3", "#01796F",
			"Pine Forest", "Pine-green right sidebar — forestry and sustainability CV."),

			// ---- Prompt 048 — 50 new unique templates ----------------------------------------------

			// MonogramHeaderTwoColumn
			Theme(CvExportTemplateId.CobaltMonogram, CvThemedTemplateLayoutKind.MonogramHeaderTwoColumn, "#2747C0", "#EEF2FD", "#2747C0",
				"Cobalt Monogram", "Cobalt initials monogram header above a light two-column body — modern professional."),
			Theme(CvExportTemplateId.TealMonogram, CvThemedTemplateLayoutKind.MonogramHeaderTwoColumn, "#0E7C7B", "#E6F4F4", "#0E7C7B",
				"Teal Monogram", "Teal initials monogram header with a two-column layout — clean corporate look."),
			Theme(CvExportTemplateId.RustMonogram, CvThemedTemplateLayoutKind.MonogramHeaderTwoColumn, "#A8410E", "#FBEEE7", "#A8410E",
				"Rust Monogram", "Warm rust monogram header over two columns — confident craft / trades CV."),
			Theme(CvExportTemplateId.SlateMonogram, CvThemedTemplateLayoutKind.MonogramHeaderTwoColumn, "#3B4A5A", "#EDF0F3", "#3B4A5A",
				"Slate Monogram", "Muted slate monogram header and two-column body — understated executive style."),

			// BannerContactStrip
			Theme(CvExportTemplateId.NavyBanner, CvThemedTemplateLayoutKind.BannerContactStrip, "#1B2A4A", "#1B2A4A", "#1B2A4A",
				"Navy Banner", "Full-width navy name banner with a contact strip — single-column, ATS-friendly."),
			Theme(CvExportTemplateId.EmeraldBanner, CvThemedTemplateLayoutKind.BannerContactStrip, "#0B6E4F", "#0B6E4F", "#0B6E4F",
				"Emerald Banner", "Emerald name banner over a clean single column — crisp and parseable."),
			Theme(CvExportTemplateId.MaroonBanner, CvThemedTemplateLayoutKind.BannerContactStrip, "#6E1423", "#6E1423", "#6E1423",
				"Maroon Banner", "Deep maroon banner header with a single-column body — formal and bold."),
			Theme(CvExportTemplateId.CharcoalBanner, CvThemedTemplateLayoutKind.BannerContactStrip, "#2B2B2B", "#2B2B2B", "#2B2B2B",
				"Charcoal Banner", "Charcoal banner header with a contact strip — minimalist single column."),

			// AsymmetricCornerBars
			Theme(CvExportTemplateId.ApricotCorner, CvThemedTemplateLayoutKind.AsymmetricCornerBars, "#E8833A", "#FDF2E8", "#E8833A",
				"Apricot Corner", "Asymmetric apricot corner bars framing a single column — creative yet tidy."),
			Theme(CvExportTemplateId.VioletCorner, CvThemedTemplateLayoutKind.AsymmetricCornerBars, "#6D28D9", "#F1ECFD", "#6D28D9",
				"Violet Corner", "Violet corner accents on an airy single column — design / product CV."),
			Theme(CvExportTemplateId.SeafoamCorner, CvThemedTemplateLayoutKind.AsymmetricCornerBars, "#2BB8A3", "#E7F7F4", "#2BB8A3",
				"Seafoam Corner", "Seafoam corner bars with generous whitespace — fresh modern résumé."),

			// SkillChipSidebar
			Theme(CvExportTemplateId.CyanChips, CvThemedTemplateLayoutKind.SkillChipSidebar, "#0891B2", "#E6F6FB", "#0891B2",
				"Cyan Chips", "Cyan chip-style sidebar headings beside the main column — UX / digital CV."),
			Theme(CvExportTemplateId.MagentaChips, CvThemedTemplateLayoutKind.SkillChipSidebar, "#B5179E", "#FBE9F6", "#B5179E",
				"Magenta Chips", "Magenta pill section labels in the sidebar — vibrant creative résumé."),
			Theme(CvExportTemplateId.LimeChips, CvThemedTemplateLayoutKind.SkillChipSidebar, "#4D7C0F", "#F0F6E6", "#4D7C0F",
				"Lime Chips", "Lime chip headings with a structured sidebar — energetic graduate CV."),
			Theme(CvExportTemplateId.IndigoChips, CvThemedTemplateLayoutKind.SkillChipSidebar, "#3730A3", "#ECEBFA", "#3730A3",
				"Indigo Chips", "Indigo pill labels organising the sidebar — engineering / tech résumé."),

			// CardSectionsBody
			Theme(CvExportTemplateId.GraphiteCards, CvThemedTemplateLayoutKind.CardSectionsBody, "#2F3438", "#EEF0F1", "#2F3438",
				"Graphite Cards", "Each section in a bordered graphite card — modular, scannable layout."),
			Theme(CvExportTemplateId.BerryCards, CvThemedTemplateLayoutKind.CardSectionsBody, "#9D174D", "#FBE7EF", "#9D174D",
				"Berry Cards", "Berry-outlined section cards — modern modular résumé."),
			Theme(CvExportTemplateId.OceanCards, CvThemedTemplateLayoutKind.CardSectionsBody, "#15607A", "#E6F1F5", "#15607A",
				"Ocean Cards", "Ocean-blue card blocks per section — clean content modules."),
			Theme(CvExportTemplateId.ForestCards, CvThemedTemplateLayoutKind.CardSectionsBody, "#1F6B3B", "#E7F3EB", "#1F6B3B",
				"Forest Cards", "Forest-green section cards — organised, calm modular CV."),

			// DualToneFullSplit
			Theme(CvExportTemplateId.NocturneSplit, CvThemedTemplateLayoutKind.DualToneFullSplit, "#232946", "#EBEDF5", "#232946",
				"Nocturne Split", "Full-height two-tone split with a centered name band — premium executive feel."),
			Theme(CvExportTemplateId.TerraSplit, CvThemedTemplateLayoutKind.DualToneFullSplit, "#B05A2A", "#F8EEE6", "#B05A2A",
				"Terra Split", "Warm terracotta two-tone split — distinctive European-style CV."),
			Theme(CvExportTemplateId.PetrolSplit, CvThemedTemplateLayoutKind.DualToneFullSplit, "#155E63", "#E5F0F1", "#155E63",
				"Petrol Split", "Petrol-teal full-height split with centered header — modern and structured."),

			// ModernistHeaderRule
			Theme(CvExportTemplateId.InkModernist, CvThemedTemplateLayoutKind.ModernistHeaderRule, "#111827", "#F3F4F6", "#111827",
				"Ink Modernist", "Uppercase name over a thick ink rule — disciplined modernist single column."),
			Theme(CvExportTemplateId.CrimsonModernist, CvThemedTemplateLayoutKind.ModernistHeaderRule, "#9F1239", "#FBE9ED", "#9F1239",
				"Crimson Modernist", "Crimson accent rule under the name — bold minimalist typography."),
			Theme(CvExportTemplateId.PineModernist, CvThemedTemplateLayoutKind.ModernistHeaderRule, "#14532D", "#E7F1EA", "#14532D",
				"Pine Modernist", "Deep pine accent rule and airy spacing — refined modernist CV."),
			Theme(CvExportTemplateId.AmberModernist, CvThemedTemplateLayoutKind.ModernistHeaderRule, "#B45309", "#FBF1E3", "#B45309",
				"Amber Modernist", "Amber rule beneath an uppercase name — warm modernist résumé."),

			// CenteredMonogram
			Theme(CvExportTemplateId.RoseCentered, CvThemedTemplateLayoutKind.CenteredMonogram, "#BE185D", "#FCE9F1", "#BE185D",
				"Rose Centered", "Centered rose monogram and symmetrical sections — elegant boutique CV."),
			Theme(CvExportTemplateId.TealCentered, CvThemedTemplateLayoutKind.CenteredMonogram, "#0F766E", "#E5F3F1", "#0F766E",
				"Teal Centered", "Centered teal monogram layout — balanced, ceremonial résumé."),
			Theme(CvExportTemplateId.NavyCentered, CvThemedTemplateLayoutKind.CenteredMonogram, "#1E3A8A", "#E9EDF8", "#1E3A8A",
				"Navy Centered", "Centered navy monogram with symmetric sections — classic formal CV."),

			// RibbonHeaderCentered
			Theme(CvExportTemplateId.CoralRibbon, CvThemedTemplateLayoutKind.RibbonHeaderCentered, "#EA580C", "#FDEEE3", "#EA580C",
				"Coral Ribbon", "Name in a centered coral ribbon pill — friendly, approachable CV."),
			Theme(CvExportTemplateId.PlumRibbon, CvThemedTemplateLayoutKind.RibbonHeaderCentered, "#7E22CE", "#F3E9FB", "#7E22CE",
				"Plum Ribbon", "Centered plum ribbon header over a single column — creative media CV."),
			Theme(CvExportTemplateId.AquaRibbon, CvThemedTemplateLayoutKind.RibbonHeaderCentered, "#0E7490", "#E4F4F8", "#0E7490",
				"Aqua Ribbon", "Aqua ribbon name pill, centered — clean and contemporary."),

			// HeaderTwoEqualColumns
			Theme(CvExportTemplateId.SapphireColumns, CvThemedTemplateLayoutKind.HeaderTwoEqualColumns, "#1D4ED8", "#E8EEFC", "#1D4ED8",
				"Sapphire Columns", "Sapphire header band over two equal columns — balanced corporate CV."),
			Theme(CvExportTemplateId.MossColumns, CvThemedTemplateLayoutKind.HeaderTwoEqualColumns, "#4D6B2C", "#EFF4E7", "#4D6B2C",
				"Moss Columns", "Moss header with a 50/50 column split — organised, grounded résumé."),
			Theme(CvExportTemplateId.ClayColumns, CvThemedTemplateLayoutKind.HeaderTwoEqualColumns, "#9A3412", "#FAEDE6", "#9A3412",
				"Clay Columns", "Clay header band and twin columns — warm structured CV."),
			Theme(CvExportTemplateId.SteelColumns, CvThemedTemplateLayoutKind.HeaderTwoEqualColumns, "#334155", "#ECEFF3", "#334155",
				"Steel Columns", "Steel header over two equal columns — neutral professional layout."),

			// AccentFooterBar
			Theme(CvExportTemplateId.MarigoldFooter, CvThemedTemplateLayoutKind.AccentFooterBar, "#CA8A04", "#FBF4E0", "#CA8A04",
				"Marigold Footer", "Top rule and marigold footer bar on every page — bright single column."),
			Theme(CvExportTemplateId.SpruceFooter, CvThemedTemplateLayoutKind.AccentFooterBar, "#0D9488", "#E4F4F2", "#0D9488",
				"Spruce Footer", "Spruce accent rule and footer band — tidy single-column résumé."),
			Theme(CvExportTemplateId.ClaretFooter, CvThemedTemplateLayoutKind.AccentFooterBar, "#7F1D3A", "#FAE8EE", "#7F1D3A",
				"Claret Footer", "Claret top rule and footer bar — refined formal CV."),

			// BoxedHeaderSidebar
			Theme(CvExportTemplateId.CharcoalBoxed, CvThemedTemplateLayoutKind.BoxedHeaderSidebar, "#1F2937", "#EEF0F2", "#1F2937",
				"Charcoal Boxed", "Boxed charcoal header with a light right sidebar — structured modern CV."),
			Theme(CvExportTemplateId.EmeraldBoxed, CvThemedTemplateLayoutKind.BoxedHeaderSidebar, "#047857", "#E4F2EC", "#047857",
				"Emerald Boxed", "Outlined emerald header and right sidebar — clean two-column résumé."),
			Theme(CvExportTemplateId.IndigoBoxed, CvThemedTemplateLayoutKind.BoxedHeaderSidebar, "#4338CA", "#ECECFB", "#4338CA",
				"Indigo Boxed", "Boxed indigo header with a sidebar — polished professional layout."),

			// DuoBandHeader
			Theme(CvExportTemplateId.SunsetDuo, CvThemedTemplateLayoutKind.DuoBandHeader, "#DB2777", "#FCE9F2", "#9333EA",
				"Sunset Duo", "Two-tone sunset header bands (magenta over violet) — vivid creative CV."),
			Theme(CvExportTemplateId.TideDuo, CvThemedTemplateLayoutKind.DuoBandHeader, "#0EA5E9", "#E5F0F7", "#0369A1",
				"Tide Duo", "Stacked tide-blue header bands — modern gradient-style header."),
			Theme(CvExportTemplateId.GroveDuo, CvThemedTemplateLayoutKind.DuoBandHeader, "#22C55E", "#E7F2EB", "#166534",
				"Grove Duo", "Two-tone green header bands — fresh, growth-themed résumé."),

			// InitialsSidebarDark
			Theme(CvExportTemplateId.ObsidianInitials, CvThemedTemplateLayoutKind.InitialsSidebarDark, "#C9A227", "#1A1A2E", "#1A1A2E",
				"Obsidian Initials", "Dark obsidian sidebar with a gold initials block — premium executive CV."),
			Theme(CvExportTemplateId.WineInitials, CvThemedTemplateLayoutKind.InitialsSidebarDark, "#D08C9B", "#4A1020", "#4A1020",
				"Wine Initials", "Dark wine sidebar with an initials monogram — hospitality / luxury CV."),
			Theme(CvExportTemplateId.PineInitials, CvThemedTemplateLayoutKind.InitialsSidebarDark, "#7FB89E", "#0B3D2E", "#0B3D2E",
				"Pine Initials", "Dark pine sidebar with initials block — grounded, premium résumé."),
			Theme(CvExportTemplateId.HarborInitials, CvThemedTemplateLayoutKind.InitialsSidebarDark, "#7FA8D0", "#0F2A4A", "#0F2A4A",
				"Harbor Initials", "Dark harbor-navy sidebar with initials — confident corporate CV."),
			Theme(CvExportTemplateId.EspressoInitials, CvThemedTemplateLayoutKind.InitialsSidebarDark, "#D8B48A", "#3B2A20", "#3B2A20",
				"Espresso Initials", "Dark espresso sidebar with a warm initials block — distinctive senior CV.")
	];

	private static readonly IReadOnlyDictionary<CvExportTemplateId, CvThemedTemplateDefinition> ById =
		Definitions.ToDictionary(definition => definition.Id);

	public static IReadOnlyList<CvThemedTemplateDefinition> All => Definitions;

	public static bool IsThemed(CvExportTemplateId templateId) => ById.ContainsKey(templateId);

	public static CvThemedTemplateDefinition Get(CvExportTemplateId templateId) =>
		ById.TryGetValue(templateId, out var definition)
			? definition
			: throw new ArgumentOutOfRangeException(nameof(templateId), templateId, "Not a themed template.");

	public static bool TryGet(CvExportTemplateId templateId, out CvThemedTemplateDefinition definition) =>
		ById.TryGetValue(templateId, out definition!);

	private static CvThemedTemplateDefinition Theme(
		CvExportTemplateId id,
		CvThemedTemplateLayoutKind layout,
		string accent,
		string sidebar,
		string header,
		string nameEnglish,
		string descriptionEnglish)
	{
		var slug = CvExportTemplateIdJson.ToJsonId(id);
		return new(
			id,
			layout,
			accent,
			sidebar,
			header,
			$"template.{slug}.name",
			nameEnglish,
			$"template.{slug}.description",
			descriptionEnglish);
	}
}
