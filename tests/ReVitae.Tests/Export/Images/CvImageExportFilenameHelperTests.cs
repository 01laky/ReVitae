using ReVitae.Core.Export.Images;
using ReVitae.Tests.Import;

namespace ReVitae.Tests.Export.Images;

public sealed class CvImageExportFilenameHelperTests
{
	[Fact]
	public void SuggestImageZipFilename_WithNames()
	{
		Assert.Equal("Jane_Doe_CV_images.zip", CvImageExportFilenameHelper.SuggestImageZipFilename("Jane", "Doe"));
	}

	[Fact]
	public void SuggestImageZipFilename_MissingNames()
	{
		Assert.Equal("ReVitae_CV_images.zip", CvImageExportFilenameHelper.SuggestImageZipFilename(null, null));
	}

	[Fact]
	public void SuggestImageZipFilename_SanitizesInvalidCharacters()
	{
		var filename = CvImageExportFilenameHelper.SuggestImageZipFilename("Jane/D", "Doe");
		Assert.EndsWith("_CV_images.zip", filename);
		Assert.DoesNotContain("/", filename);
	}

	[Fact]
	public void SuggestImageZipFilename_PreservesUnicode()
	{
		var filename = CvImageExportFilenameHelper.SuggestImageZipFilename("Ladislav", "Kostolný");
		Assert.Contains("Kostolný", filename);
	}

	[Fact]
	public void SuggestImagePageFilename_FormatsPageOne()
	{
		Assert.Equal(
			"Jane_Doe_CV_page-01.png",
			CvImageExportFilenameHelper.SuggestImagePageFilename("Jane", "Doe", 1, CvImageExportFormat.Png));
	}

	[Fact]
	public void SuggestImagePageFilename_FormatsPageTen()
	{
		Assert.Equal(
			"Jane_Doe_CV_page-10.jpg",
			CvImageExportFilenameHelper.SuggestImagePageFilename("Jane", "Doe", 10, CvImageExportFormat.Jpeg));
	}

	[Fact]
	public void SuggestImagePageFilename_FormatsPageOneHundred()
	{
		Assert.Equal(
			"Jane_Doe_CV_page-100.webp",
			CvImageExportFilenameHelper.SuggestImagePageFilename("Jane", "Doe", 100, CvImageExportFormat.WebP));
	}

	[Fact]
	public void FormatZipEntryName_UsesShortPattern()
	{
		Assert.Equal("page-02.png", CvImageExportFilenameHelper.FormatZipEntryName(2, CvImageExportFormat.Png));
	}

	[Fact]
	public void ResolveCollisionSafePath_AppendsSuffixWhenExists()
	{
		using var temp = new TempImportDirectory();
		var first = Path.Combine(temp.RootPath, "Jane_Doe_CV_page-01.png");
		File.WriteAllText(first, "existing");

		var resolved = CvImageExportFilenameHelper.ResolveCollisionSafePath(
			temp.RootPath,
			"Jane_Doe_CV_page-01.png");

		Assert.Equal(Path.Combine(temp.RootPath, "Jane_Doe_CV_page-01-2.png"), resolved);
	}

	[Fact]
	public void ResolveCollisionSafePath_ReturnsOriginalWhenFree()
	{
		using var temp = new TempImportDirectory();
		var resolved = CvImageExportFilenameHelper.ResolveCollisionSafePath(
			temp.RootPath,
			"Jane_Doe_CV_page-01.png");
		Assert.Equal(Path.Combine(temp.RootPath, "Jane_Doe_CV_page-01.png"), resolved);
	}
}
