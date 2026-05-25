using ReVitae.Core.Export;
using ReVitae.Core.Import;
using ReVitae.Tests.Import.Fixtures.JohnDoe;

namespace ReVitae.Tests.Import;

[Trait("Category", "ImportPdfReimport")]
[Collection(nameof(ImportPdfSerialCollection))]
public sealed class ImportPdfReimportStabilityEdgeCaseTests
{
	[Fact]
	public void ClassicSidebarVariant02_StressRepeatImport_SucceedsTenTimes()
	{
		var spec = JohnDoeVariantCatalog.All.First(entry => entry.Id == "02");

		for (var attempt = 0; attempt < 10; attempt++)
		{
			using var generated = JohnDoeVariantGenerator.Generate(spec);
			Assert.True(generated.ByteLength > 1024, $"Attempt {attempt}: PDF too small ({generated.ByteLength} bytes).");

			var result = CvDocumentImporter.Import(generated.Path);
			JohnDoeImportAssertions.AssertMatchesExpectations(result, spec);
		}
	}

	[Theory]
	[InlineData("02")]
	[InlineData("07")]
	public void PdfSidebarCountVariants_MeetExpectationsUnderSerialCollection(string variantId)
	{
		var spec = JohnDoeVariantCatalog.All.First(entry => entry.Id == variantId);
		using var generated = JohnDoeVariantGenerator.Generate(spec);
		var result = CvDocumentImporter.Import(generated.Path);
		JohnDoeImportAssertions.AssertMatchesExpectations(result, spec);
	}
}
