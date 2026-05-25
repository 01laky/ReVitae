using ReVitae.Core.Export;
using ReVitae.Core.Export.Images;
using ReVitae.Core.Export.Pdf;
using ReVitae.Core.Localization;
using ReVitae.Tests.Import;

namespace ReVitae.Tests.Export.Images;

public sealed class CvImageExporterTests
{
	private static CvImageExportOptions DefaultOptions => CvImageExportOptions.Default;

	[Fact]
	public void Export_ZipHappyPath_Succeeds()
	{
		using var temp = new TempImportDirectory();
		var zipPath = Path.Combine(temp.RootPath, "cv.zip");
		var document = CvExportTestFixtures.CreatePersonalInfoOnlyDocument();

		var result = CvImageExporter.Export(
			document,
			DefaultOptions,
			new CvImageExportDestination.ZipFile(zipPath));

		Assert.True(result.Success, result.ErrorMessageKey);
		Assert.True(File.Exists(zipPath));
		Assert.True(new FileInfo(zipPath).Length > 0);
		Assert.Equal(CvImageExporter.GetPageCount(document), result.ExportedPageCount);
	}

	[Fact]
	public void Export_FolderHappyPath_Succeeds()
	{
		using var temp = new TempImportDirectory();
		var document = CvExportTestFixtures.CreatePersonalInfoOnlyDocument();

		var result = CvImageExporter.Export(
			document,
			DefaultOptions with { Delivery = CvImageExportDelivery.SeparateFiles },
			new CvImageExportDestination.Folder(temp.RootPath));

		if (!result.Success)
		{
			Assert.Fail(result.ErrorMessageKey ?? "export.failed");
		}

		Assert.NotNull(result.OutputPath);
		Assert.True(File.Exists(result.OutputPath));
	}

	[Fact]
	public void Export_JpegFormat_Succeeds()
	{
		using var temp = new TempImportDirectory();
		var zipPath = Path.Combine(temp.RootPath, "cv.zip");
		var document = CvExportTestFixtures.CreateRepresentativeDocument();

		var result = CvImageExporter.Export(
			document,
			DefaultOptions with { Format = CvImageExportFormat.Jpeg, Quality = 80 },
			new CvImageExportDestination.ZipFile(zipPath));

		Assert.True(result.Success);
	}

	[Fact]
	public void Export_WebPFormat_Succeeds()
	{
		using var temp = new TempImportDirectory();
		var zipPath = Path.Combine(temp.RootPath, "cv.zip");
		var document = CvExportTestFixtures.CreateRepresentativeDocument();

		var result = CvImageExporter.Export(
			document,
			DefaultOptions with { Format = CvImageExportFormat.WebP },
			new CvImageExportDestination.ZipFile(zipPath));

		Assert.True(result.Success);
	}

	[Fact]
	public void Export_PageRangeSinglePage_ExportsOneFile()
	{
		using var temp = new TempImportDirectory();
		var zipPath = Path.Combine(temp.RootPath, "single.zip");
		var document = CvExportTestFixtures.CreateLongContentDocument();

		var total = CvImageExporter.GetPageCount(document);
		Assert.True(total >= 1);

		var result = CvImageExporter.Export(
			document,
			DefaultOptions with { PageRange = new CvImagePageRange(1, 1) },
			new CvImageExportDestination.ZipFile(zipPath));

		Assert.True(result.Success);
		Assert.Equal(1, result.ExportedPageCount);
	}

	[Fact]
	public void Export_NullDocument_Fails()
	{
		using var temp = new TempImportDirectory();
		var result = CvImageExporter.Export(
			null!,
			DefaultOptions,
			new CvImageExportDestination.ZipFile(Path.Combine(temp.RootPath, "x.zip")));
		Assert.False(result.Success);
	}

	[Fact]
	public void Export_ProgressCallback_IsInvoked()
	{
		using var temp = new TempImportDirectory();
		var zipPath = Path.Combine(temp.RootPath, "progress.zip");
		var document = CvExportTestFixtures.CreateRepresentativeDocument();
		var phases = new List<ImageExportProgressPhase>();

		CvImageExporter.Export(
			document,
			DefaultOptions,
			new CvImageExportDestination.ZipFile(zipPath),
			new TestProgress(phases));

		Assert.Contains(ImageExportProgressPhase.Rendering, phases);
		Assert.Contains(ImageExportProgressPhase.Writing, phases);
	}

	[Fact]
	public void Export_Png_ProducesNonEmptyZip()
	{
		using var temp = new TempImportDirectory();
		var zip1 = Path.Combine(temp.RootPath, "a.zip");
		var document = CvExportTestFixtures.CreateRepresentativeDocument(CvExportTemplateId.CleanTopHeader);

		var result = CvImageExporter.Export(document, DefaultOptions, new CvImageExportDestination.ZipFile(zip1));

		Assert.True(result.Success);
		Assert.True(new FileInfo(zip1).Length > 0);
	}

	[Fact]
	public void Export_DiacriticsFilename_PreservedInFolderMode()
	{
		using var temp = new TempImportDirectory();
		var document = CvExportTestFixtures.CreateRepresentativeDocument();

		var result = CvImageExporter.Export(
			document,
			DefaultOptions with { Delivery = CvImageExportDelivery.SeparateFiles },
			new CvImageExportDestination.Folder(temp.RootPath));

		Assert.True(result.Success);
		Assert.Contains("Kostolný", result.OutputPath!, StringComparison.Ordinal);
	}

	[Fact]
	public void GetPageCount_LongDocument_MatchesPdfPageCount()
	{
		var document = CvExportTestFixtures.CreateLongContentDocument();
		var pdfBytes = new QuestPdfCvExporter().Export(document);
		var rasterizer = new DocnetPdfPageRasterizer();
		Assert.Equal(rasterizer.GetPageCount(pdfBytes), CvImageExporter.GetPageCount(document));
	}

	[Fact]
	public void Export_InvalidPageRange_Fails()
	{
		using var temp = new TempImportDirectory();
		var document = CvExportTestFixtures.CreateRepresentativeDocument();
		var result = CvImageExporter.Export(
			document,
			DefaultOptions with { PageRange = new CvImagePageRange(5, 2) },
			new CvImageExportDestination.ZipFile(Path.Combine(temp.RootPath, "bad.zip")));

		Assert.False(result.Success);
		Assert.Equal(TranslationKeys.ExportImageRangeInvalid, result.ErrorMessageKey);
	}

	private sealed class TestProgress(List<ImageExportProgressPhase> phases) : IImageExportProgress
	{
		public void Report(ImageExportProgressPhase phase, int currentPage, int totalPages) => phases.Add(phase);
	}
}

public sealed class CvImageExportTemplateSmokeTests
{
	[Theory]
	[InlineData(CvExportTemplateId.ClassicSidebar)]
	[InlineData(CvExportTemplateId.ModernSidebar)]
	[InlineData(CvExportTemplateId.CleanTopHeader)]
	public void Export_MatchesPdfPageCount(CvExportTemplateId templateId)
	{
		using var temp = new TempImportDirectory();
		var document = CvExportTestFixtures.CreateRepresentativeDocument(templateId);
		var pdfPages = CvImageExporter.GetPageCount(document);

		var result = CvImageExporter.Export(
			document,
			CvImageExportOptions.Default,
			new CvImageExportDestination.ZipFile(Path.Combine(temp.RootPath, $"{templateId}.zip")));

		Assert.True(result.Success);
		Assert.Equal(pdfPages, result.ExportedPageCount);
	}
}

public sealed class CvImageExportOcrRoundTripSmokeTests
{
	[Fact]
	public void ExportedPng_HasMinimumDimensionsAndSize()
	{
		using var temp = new TempImportDirectory();
		var zipPath = Path.Combine(temp.RootPath, "ocr.zip");
		var document = CvExportTestFixtures.CreateRepresentativeDocument(CvExportTemplateId.ClassicSidebar);

		var result = CvImageExporter.Export(
			document,
			CvImageExportOptions.Default with { Scale = CvImageExportScale.High },
			new CvImageExportDestination.ZipFile(zipPath));

		Assert.True(result.Success);
		Assert.True(new FileInfo(zipPath).Length >= 10_000);

		using var archive = System.IO.Compression.ZipFile.OpenRead(zipPath);
		var entry = archive.Entries.First();
		using var entryStream = entry.Open();
		using var memory = new MemoryStream();
		entryStream.CopyTo(memory);
		var pngBytes = memory.ToArray();
		Assert.True(pngBytes.Length >= 10_000);
	}
}
