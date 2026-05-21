namespace ReVitae.Core.Export;

using ReVitae.Core.Localization;

public sealed record CvExportTemplateDescriptor(
    CvExportTemplateId Id,
    string NameKey,
    string DescriptionKey,
    string AccentColor);

public static class CvExportTemplateCatalog
{
    private static readonly IReadOnlyList<CvExportTemplateDescriptor> Templates =
    [
        new(CvExportTemplateId.ClassicSidebar, TranslationKeys.ClassicSidebar, TranslationKeys.ClassicSidebarDescription, "#F47C2C"),
        new(CvExportTemplateId.ModernSidebar, TranslationKeys.ModernSidebar, TranslationKeys.ModernSidebarDescription, "#444444"),
        new(CvExportTemplateId.CleanTopHeader, TranslationKeys.CleanTopHeader, TranslationKeys.CleanTopHeaderDescription, "#5A9BD5"),
        new(CvExportTemplateId.DarkSidebarAccent, TranslationKeys.DarkSidebarAccent, TranslationKeys.DarkSidebarAccentDescription, "#5B9BB0"),
        new(CvExportTemplateId.CenteredMinimal, TranslationKeys.CenteredMinimal, TranslationKeys.CenteredMinimalDescription, "#212121"),
        new(CvExportTemplateId.PhotoLeftBand, TranslationKeys.PhotoLeftBand, TranslationKeys.PhotoLeftBandDescription, "#E67E22"),
        new(CvExportTemplateId.ExecutiveBlueSidebar, TranslationKeys.ExecutiveBlueSidebar, TranslationKeys.ExecutiveBlueSidebarDescription, "#1E3A5F"),
        new(CvExportTemplateId.PeachDesigner, TranslationKeys.PeachDesigner, TranslationKeys.PeachDesignerDescription, "#E9B083"),
        new(CvExportTemplateId.NavyProfileSplit, TranslationKeys.NavyProfileSplit, TranslationKeys.NavyProfileSplitDescription, "#E67E22"),
        new(CvExportTemplateId.ForestGreenSidebar, TranslationKeys.ForestGreenSidebar, TranslationKeys.ForestGreenSidebarDescription, "#2F5D3A"),
        new(CvExportTemplateId.YellowSkillDots, TranslationKeys.YellowSkillDots, TranslationKeys.YellowSkillDotsDescription, "#F5C400"),
        new(CvExportTemplateId.RoyalBlueSidebar, TranslationKeys.RoyalBlueSidebar, TranslationKeys.RoyalBlueSidebarDescription, "#4A76C0"),
        new(CvExportTemplateId.OrangeTimeline, TranslationKeys.OrangeTimeline, TranslationKeys.OrangeTimelineDescription, "#E67E22"),
        new(CvExportTemplateId.BlueAccentSummary, TranslationKeys.BlueAccentSummary, TranslationKeys.BlueAccentSummaryDescription, "#2C4A93"),
        new(CvExportTemplateId.PillHeaderSplit, TranslationKeys.PillHeaderSplit, TranslationKeys.PillHeaderSplitDescription, "#E9967A"),
        new(CvExportTemplateId.NavyOverlapPhoto, TranslationKeys.NavyOverlapPhoto, TranslationKeys.NavyOverlapPhotoDescription, "#1E3A5F")
    ];

    public static IReadOnlyList<CvExportTemplateDescriptor> All => Templates;

    public static CvExportTemplateDescriptor Get(CvExportTemplateId id) =>
        Templates.First(t => t.Id == id);

    public static string GetAccentColor(CvExportTemplateId id) => Get(id).AccentColor;
}
