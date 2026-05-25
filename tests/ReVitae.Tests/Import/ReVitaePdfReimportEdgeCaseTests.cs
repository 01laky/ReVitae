using ReVitae.Core.Export;
using ReVitae.Core.Export.Fixtures;
using ReVitae.Core.Export.Pdf;
using ReVitae.Core.Import;
using ReVitae.Core.Import.Pdf;
using ReVitae.Core.Localization;
using ReVitae.Tests.Import.Fixtures.JohnDoe;

namespace ReVitae.Tests.Import;

[Trait("Category", "ImportPdfReimport")]
[Collection(nameof(ImportPdfSerialCollection))]
public sealed class ReVitaePdfReimportEdgeCaseTests
{
	public static IEnumerable<object[]> MatrixVariants =>
		JohnDoeVariantCatalog.All.Select(spec => new object[] { spec });

	[Theory]
	[InlineData("01")]
	public void Variant01_ModernSidebar_MeetsPdfFull(string variantId) =>
		AssertVariant(variantId);

	[Theory]
	[InlineData("08")]
	public void Variant08_ExecutiveBlue_HasSidebarContactUrls(string variantId)
	{
		var result = ImportVariant(variantId);
		Assert.Contains("john.doe@example.com", result.Personal.Email, StringComparison.OrdinalIgnoreCase);
		Assert.True(result.WorkExperienceEntries.Count >= 15);
	}

	[Theory]
	[InlineData("09")]
	public void Variant09_CenteredMinimal_HasEmailAndWork(string variantId)
	{
		var result = ImportVariant(variantId);
		Assert.Contains("john.doe@example.com", result.Personal.Email, StringComparison.OrdinalIgnoreCase);
		Assert.True(result.WorkExperienceEntries.Count >= 15);
	}

	[Fact]
	public void Variant51_DeferredSidebar_ContactAfterMainPages()
	{
		var result = ImportVariant("51");
		Assert.Contains("jane.sidebar@example.com", result.Personal.Email, StringComparison.OrdinalIgnoreCase);
		Assert.True(result.WorkExperienceEntries.Count >= 1);
	}

	[Theory]
	[InlineData("12")]
	public void TextVariant12_SplitLinkedInUrl_FullUrl(string variantId)
	{
		var result = ImportVariant(variantId);
		Assert.Equal(JohnDoeCanonicalExpectations.LinkedInUrl, result.Personal.LinkedInUrl);
	}

	[Theory]
	[InlineData("23")]
	public void TextVariant23_SkillsColonCategories_AtLeastTenGroups(string variantId)
	{
		var result = ImportVariant(variantId);
		Assert.True(result.SkillsGroups.Count >= 10);
	}

	[Theory]
	[InlineData("31")]
	public void TextVariant31_CertificateIssuedSplit_TwentyFourCertificates(string variantId)
	{
		var result = ImportVariant(variantId);
		Assert.Equal(24, result.CertificateEntries.Count);
	}

	[Fact]
	public void HyperlinkOnlyGitHubUrl_PopulatesPersonalGitHub()
	{
		var path = Path.Combine(Path.GetTempPath(), $"revitae-github-link-{Guid.NewGuid():N}.pdf");
		try
		{
			File.WriteAllBytes(path, HyperlinkLayoutPdfWriter.CreateGitHubHyperlinkOnlySidebar());
			var extraction = new PdfTextExtractorAdapter(new PdfPigTextExtractor()).Extract(path);
			var result = CvTextImportPipeline.Import(
				extraction.Text,
				extraction.HyperlinkUrls,
				reVitaeHints: extraction.ReVitaeHints);

			Assert.True(result.Success);
			Assert.Contains("github.com/johndoe", result.Personal.GitHubUrl, StringComparison.OrdinalIgnoreCase);
		}
		finally
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
	}

	[Fact]
	public void MetadataStampedExport_UsesTemplateProfile()
	{
		var path = Path.Combine(Path.GetTempPath(), $"revitae-meta-{Guid.NewGuid():N}.pdf");
		try
		{
			var document = JohnDoeStressCvDataset.CreateDocument(CvExportTemplateId.ExecutiveBlueSidebar);
			File.WriteAllBytes(path, new QuestPdfCvExporter().Export(document));

			var extraction = new PdfTextExtractorAdapter(new PdfPigTextExtractor()).Extract(path);

			Assert.NotNull(extraction.ReVitaeHints);
			Assert.Equal(CvExportTemplateId.ExecutiveBlueSidebar, extraction.ReVitaeHints.TemplateId);
			Assert.Equal(ReVitaePdfLayoutProfiles.ExecutiveBlueSidebarRatio, extraction.ReVitaeHints.SidebarSplitRatio);
		}
		finally
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
	}

	[Fact]
	public void GenericNonReVitaePdf_HintsFalse()
	{
		var path = Path.Combine(Path.GetTempPath(), $"revitae-generic-{Guid.NewGuid():N}.pdf");
		try
		{
			File.WriteAllBytes(path, SidebarLayoutPdfWriter.Create(SidebarLayoutPdfWriter.CreateSinglePageTwoColumnLayout()));
			var extraction = new PdfTextExtractorAdapter(new PdfPigTextExtractor()).Extract(path);

			Assert.NotNull(extraction.ReVitaeHints);
			Assert.False(extraction.ReVitaeHints.IsLikelyReVitaeExport);
		}
		finally
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
	}

	[Fact]
	public void PasswordProtectedPdf_ReturnsPasswordProtectedError()
	{
		var path = Path.Combine(AppContext.BaseDirectory, "Import", "Fixtures", "Pdf", "password-protected.pdf");
		if (!File.Exists(path))
		{
			return;
		}

		var extraction = new PdfTextExtractorAdapter(new PdfPigTextExtractor()).Extract(path);
		Assert.False(extraction.Success);
		Assert.Equal(TranslationKeys.ImportErrorPasswordProtected, extraction.ErrorMessageKey);
	}

	[Fact]
	public void Variant01_RegressionGuard_StillPdfFullAfterLayoutProfiles()
	{
		AssertVariant("01");
	}

	[Fact]
	public void CommittedJohnDoeStressPdf_ParsesWithoutSkip()
	{
		var path = Path.Combine(AppContext.BaseDirectory, "Import", "Fixtures", "JohnDoeStressCv.pdf");
		Assert.True(File.Exists(path));

		var result = CvDocumentImporter.Import(path);
		Assert.True(result.Success);
		Assert.Equal(20, result.WorkExperienceEntries.Count);
		Assert.Equal("https://github.com/johndoe", result.Personal.GitHubUrl);
	}

	private static void AssertVariant(string variantId)
	{
		var spec = JohnDoeVariantCatalog.All.First(entry => entry.Id == variantId);
		using var generated = JohnDoeVariantGenerator.Generate(spec);
		var result = CvDocumentImporter.Import(generated.Path);
		JohnDoeImportAssertions.AssertMatchesExpectations(result, spec);
	}

	private static CvImportResult ImportVariant(string variantId)
	{
		var spec = JohnDoeVariantCatalog.All.First(entry => entry.Id == variantId);
		using var generated = JohnDoeVariantGenerator.Generate(spec);
		var result = CvDocumentImporter.Import(generated.Path);
		Assert.True(result.Success, result.ErrorMessageKey);
		return result;
	}
}
