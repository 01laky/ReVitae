using ReVitae.Core.Export;

namespace ReVitae.Core.Projects;

public sealed record CvProjectSettings(
    int SchemaVersion,
    CvExportTemplateId? SelectedTemplateId,
    IReadOnlyList<string> DismissedQualityHintKeys,
    IReadOnlyDictionary<string, bool>? SectionExpandState,
    DateTimeOffset? SavedAtUtc,
    string? ApplicationVersion)
{
    public static CvProjectSettings CreateDefault(CvExportTemplateId templateId) =>
        new(
            CvProjectConstants.CurrentProjectSettingsSchemaVersion,
            templateId,
            [],
            null,
            null,
            CvProjectApplicationInfo.Version);
}
