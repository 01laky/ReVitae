using ReVitae.Core.Export;

namespace ReVitae.Tests.Import.Fixtures.JohnDoe;

public sealed record JohnDoeVariantSpec(
	string Id,
	string Name,
	JohnDoeVariantKind Kind,
	CvExportTemplateId? PdfTemplate,
	JohnDoeTextFormattingProfile? TextProfile,
	JohnDoeExpectationMode ExpectationMode,
	string FileExtension,
	int? MinimumEducationEntries = null)
{
	public string FileName => $"john-doe-{Id}{FileExtension}";
}
