using ReVitae.Core.Export;

namespace ReVitae.Core.Import.Pdf;

public enum ReVitaePdfColumnKind
{
    TwoColumnSidebar,
    SingleColumn,
    PhotoLeftBand,
    NavyProfileSplit
}

public sealed record ReVitaePdfLayoutProfile(
    ReVitaePdfColumnKind ColumnKind,
    double? SidebarWidthRatio);

/// <summary>Per-template PdfPig column geometry for ReVitae QuestPDF exports.</summary>
public static class ReVitaePdfLayoutProfiles
{
    public const double DefaultSidebarRatio = 0.34;
    public const double ExecutiveBlueSidebarRatio = 0.35;
    public const double PhotoLeftBandRatio = 0.28;

    private static readonly IReadOnlyDictionary<CvExportTemplateId, ReVitaePdfLayoutProfile> Profiles =
        new Dictionary<CvExportTemplateId, ReVitaePdfLayoutProfile>
        {
            [CvExportTemplateId.ModernSidebar] = TwoColumn(DefaultSidebarRatio),
            [CvExportTemplateId.ClassicSidebar] = TwoColumn(0.36),
            [CvExportTemplateId.DarkSidebarAccent] = TwoColumn(DefaultSidebarRatio),
            [CvExportTemplateId.ForestGreenSidebar] = TwoColumn(DefaultSidebarRatio),
            [CvExportTemplateId.RoyalBlueSidebar] = TwoColumn(DefaultSidebarRatio),
            [CvExportTemplateId.ExecutiveBlueSidebar] = TwoColumn(ExecutiveBlueSidebarRatio),
            [CvExportTemplateId.PhotoLeftBand] = new(ReVitaePdfColumnKind.PhotoLeftBand, PhotoLeftBandRatio),
            [CvExportTemplateId.NavyProfileSplit] = new(ReVitaePdfColumnKind.NavyProfileSplit, DefaultSidebarRatio),
            [CvExportTemplateId.CleanTopHeader] = SingleColumn(),
            [CvExportTemplateId.CenteredMinimal] = SingleColumn(),
            [CvExportTemplateId.OrangeTimeline] = TwoColumn(DefaultSidebarRatio),
            [CvExportTemplateId.BlueAccentSummary] = TwoColumn(DefaultSidebarRatio),
            [CvExportTemplateId.PeachDesigner] = TwoColumn(DefaultSidebarRatio),
            [CvExportTemplateId.YellowSkillDots] = TwoColumn(DefaultSidebarRatio),
            [CvExportTemplateId.PillHeaderSplit] = TwoColumn(DefaultSidebarRatio),
            [CvExportTemplateId.NavyOverlapPhoto] = TwoColumn(DefaultSidebarRatio),
        };

    public static IReadOnlyList<CvExportTemplateId> MatrixPdfTemplateIds { get; } =
    [
        CvExportTemplateId.ModernSidebar,
        CvExportTemplateId.ClassicSidebar,
        CvExportTemplateId.CleanTopHeader,
        CvExportTemplateId.RoyalBlueSidebar,
        CvExportTemplateId.NavyProfileSplit,
        CvExportTemplateId.PhotoLeftBand,
        CvExportTemplateId.DarkSidebarAccent,
        CvExportTemplateId.ExecutiveBlueSidebar,
        CvExportTemplateId.CenteredMinimal,
        CvExportTemplateId.OrangeTimeline,
        CvExportTemplateId.ForestGreenSidebar,
        CvExportTemplateId.BlueAccentSummary
    ];

    public static ReVitaePdfLayoutProfile Get(CvExportTemplateId templateId) =>
        Profiles.TryGetValue(templateId, out var profile) ? profile : DefaultTwoColumn;

    public static ReVitaePdfLayoutProfile DefaultTwoColumn => TwoColumn(DefaultSidebarRatio);

    public static ReVitaePdfLayoutProfile ForHints(ReVitaePdfExportHints? hints) =>
        hints?.TemplateId is { } templateId
            ? Get(templateId)
            : hints?.SidebarSplitRatio is { } ratio
                ? TwoColumn(ratio)
                : DefaultTwoColumn;

    private static ReVitaePdfLayoutProfile TwoColumn(double ratio) =>
        new(ReVitaePdfColumnKind.TwoColumnSidebar, ratio);

    private static ReVitaePdfLayoutProfile SingleColumn() =>
        new(ReVitaePdfColumnKind.SingleColumn, null);
}
