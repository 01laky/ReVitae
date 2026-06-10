using System.Text;
using ReVitae.Core.Export;
using ReVitae.Core.Export.Fixtures;
using ReVitae.Core.Export.Pdf;

namespace ReVitae.Tests.Export;

/// <summary>
/// Guards that PDF export bundles and embeds the Arimo font instead of referencing a host-installed
/// "Arial". Relying on system fonts made rendering non-deterministic across platforms (the Linux CI
/// runner substituted a different face), which broke the render golden and the PDF re-import
/// round-trip. These tests fail fast if anyone reverts to a system font family.
/// </summary>
public sealed class CvPdfFontEmbeddingTests
{
	[Fact]
	public void FamilyName_IsBundledArimo()
	{
		Assert.Equal("Arimo", CvPdfFonts.FamilyName);
	}

	[Fact]
	public void EnsureRegistered_IsIdempotent()
	{
		// Must not throw on repeated calls (registration is process-wide and guarded).
		CvPdfFonts.EnsureRegistered();
		CvPdfFonts.EnsureRegistered();
	}

	[Theory]
	[InlineData(CvExportTemplateId.ClassicSidebar)]
	[InlineData(CvExportTemplateId.CenteredMinimal)]
	[InlineData(CvExportTemplateId.OrangeTimeline)]
	public void Export_EmbedsArimo_AndNeverReferencesArial(CvExportTemplateId templateId)
	{
		var pdf = new QuestPdfCvExporter().Export(
			JohnDoeMinimalArchitectCvDataset.CreateDocument(templateId));

		// PDF font dictionaries store the base font name as Latin1 text in the byte stream.
		var contents = Encoding.Latin1.GetString(pdf);
		Assert.Contains("Arimo", contents);
		Assert.DoesNotContain("Arial", contents);
	}
}
