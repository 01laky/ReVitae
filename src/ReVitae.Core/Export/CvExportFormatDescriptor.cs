namespace ReVitae.Core.Export;

public sealed record CvExportFormatDescriptor(
	CvExportFormat Format,
	CvExportFormatCategory Category,
	string IconSlug,
	string LabelKey,
	string? HintKey,
	bool IsRecommended,
	bool IsEnabled,
	string? DisabledTooltipKey = null);
