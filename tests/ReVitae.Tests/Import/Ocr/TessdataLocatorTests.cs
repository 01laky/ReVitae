using ReVitae.Core.Import.Ocr;

namespace ReVitae.Tests.Import.Ocr;

public sealed class TessdataLocatorTests
{
	[Fact]
	public void BuildCandidatePaths_IncludesBaseDirectoryLocalAppDataAndTessdataPrefix()
	{
		var previous = Environment.GetEnvironmentVariable("TESSDATA_PREFIX");
		var customPrefix = Path.Combine(Path.GetTempPath(), $"revitae-tess-prefix-{Guid.NewGuid():N}");

		Environment.SetEnvironmentVariable("TESSDATA_PREFIX", customPrefix);
		try
		{
			var candidates = TessdataLocator.BuildCandidatePaths();

			Assert.Contains(Path.Combine(AppContext.BaseDirectory, "tessdata"), candidates, StringComparer.OrdinalIgnoreCase);

			var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			Assert.Contains(Path.Combine(localAppData, "ReVitae", "tessdata"), candidates, StringComparer.OrdinalIgnoreCase);
			Assert.Contains(customPrefix, candidates, StringComparer.OrdinalIgnoreCase);
		}
		finally
		{
			Environment.SetEnvironmentVariable("TESSDATA_PREFIX", previous);
		}
	}

	[Fact]
	public void HasLanguageFile_ReturnsTrueWhenTrainedDataExists()
	{
		var tessdataDirectory = Path.Combine(AppContext.BaseDirectory, "tessdata");
		if (!Directory.Exists(tessdataDirectory))
		{
			return;
		}

		var hasEng = TessdataLocator.HasLanguageFile(tessdataDirectory, "eng");
		if (File.Exists(Path.Combine(tessdataDirectory, "eng.traineddata")))
		{
			Assert.True(hasEng);
		}
	}

	[Fact]
	public void FindTessdataDirectory_FindsBundledEngInTestOutput()
	{
		var bundled = Path.Combine(AppContext.BaseDirectory, "tessdata", "eng.traineddata");
		if (!File.Exists(bundled))
		{
			return;
		}

		var directory = TessdataLocator.FindTessdataDirectory();

		Assert.NotNull(directory);
		Assert.True(TessdataLocator.HasLanguageFile(directory, "eng"));
	}
}
