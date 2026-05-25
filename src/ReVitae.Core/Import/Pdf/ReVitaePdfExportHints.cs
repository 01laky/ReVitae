using ReVitae.Core.Export;

namespace ReVitae.Core.Import.Pdf;

public sealed record ReVitaePdfExportHints(
	bool IsLikelyReVitaeExport,
	CvExportTemplateId? TemplateId,
	double? SidebarSplitRatio,
	bool UsesDeferredSidebar)
{
	public static ReVitaePdfExportHints None { get; } = new(false, null, null, false);
}
