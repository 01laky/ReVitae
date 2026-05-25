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
            "Pine Forest", "Pine-green right sidebar — forestry and sustainability CV.")
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
