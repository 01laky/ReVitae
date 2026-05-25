using DocumentFormat.OpenXml.Packaging;
using ReVitae.Core.Cv.ProfilePhoto;
using ReVitae.Core.Export;

namespace ReVitae.Tests.Export;

public sealed class CvDocxPhotoInserterEdgeCaseTests
{
	[Fact]
	public void WriteDocx_MissingPhotoPath_DoesNotThrow()
	{
		var document = CvExportTestFixtures.CreateRepresentativeDocument() with { PhotoPath = null };
		var bytes = ExportDocx(document);

		Assert.True(bytes.Length > 0);
	}

	[Fact]
	public void WriteDocx_InvalidPhotoPath_DoesNotThrow()
	{
		var document = CvExportTestFixtures.CreateRepresentativeDocument() with
		{
			PhotoPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".png"),
		};
		var bytes = ExportDocx(document);

		Assert.True(bytes.Length > 0);
	}

	[Fact]
	public void WriteDocx_CorruptImageBytes_DoesNotThrow()
	{
		var tempDirectory = ProfilePhotoTestHelpers.CreateTempDirectory();
		try
		{
			var corruptPath = Path.Combine(tempDirectory, "corrupt.png");
			File.WriteAllText(corruptPath, "not-an-image");
			var document = CvExportTestFixtures.CreateRepresentativeDocument() with { PhotoPath = corruptPath };

			var bytes = ExportDocx(document);

			Assert.True(bytes.Length > 0);
		}
		finally
		{
			Directory.Delete(tempDirectory, recursive: true);
		}
	}

	[Fact]
	public void WriteDocx_ValidPng_IncludesImagePart()
	{
		var tempDirectory = ProfilePhotoTestHelpers.CreateTempDirectory();
		try
		{
			var storage = new ProfilePhotoStorage(tempDirectory);
			var saved = storage.TrySaveCopy(ProfilePhotoTestHelpers.WriteMinimalPng(tempDirectory));
			Assert.True(saved.Success);

			var document = CvExportTestFixtures.CreateRepresentativeDocument() with { PhotoPath = saved.StoredPath };
			var bytes = ExportDocx(document);

			using var package = WordprocessingDocument.Open(new MemoryStream(bytes), false);
			Assert.NotEmpty(package.MainDocumentPart!.ImageParts);
		}
		finally
		{
			Directory.Delete(tempDirectory, recursive: true);
		}
	}

	[Fact]
	public void WriteDocx_EmptyPhotoFile_SkipsImagePart()
	{
		var tempDirectory = ProfilePhotoTestHelpers.CreateTempDirectory();
		try
		{
			var emptyPath = Path.Combine(tempDirectory, "empty.jpg");
			File.WriteAllBytes(emptyPath, []);
			var document = CvExportTestFixtures.CreateRepresentativeDocument() with { PhotoPath = emptyPath };
			var bytes = ExportDocx(document);

			using var package = WordprocessingDocument.Open(new MemoryStream(bytes), false);
			Assert.Empty(package.MainDocumentPart!.ImageParts);
		}
		finally
		{
			Directory.Delete(tempDirectory, recursive: true);
		}
	}

	[Fact]
	public void WriteDocx_OversizePhoto_IsRejectedByStorageBeforeExport()
	{
		var tempDirectory = ProfilePhotoTestHelpers.CreateTempDirectory();
		try
		{
			var hugePath = Path.Combine(tempDirectory, "huge.png");
			File.WriteAllBytes(hugePath, new byte[ProfilePhotoFormats.MaxFileSizeBytes + 1]);
			var storage = new ProfilePhotoStorage(tempDirectory);
			var saved = storage.TrySaveCopy(hugePath);

			Assert.False(saved.Success);
		}
		finally
		{
			Directory.Delete(tempDirectory, recursive: true);
		}
	}

	private static byte[] ExportDocx(CvExportDocument document)
	{
		using var stream = new MemoryStream();
		var source = CvExportTestFixtures.CreateRepresentativeSourceData();
		var result = CvDocumentExporter.Export(document, source, CvExportFormat.Docx, stream);
		Assert.True(result.Success);
		return stream.ToArray();
	}
}
