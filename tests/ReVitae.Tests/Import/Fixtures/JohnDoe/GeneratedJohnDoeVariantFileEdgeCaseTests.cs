using ReVitae.Core.Export;
using ReVitae.Core.Export.Fixtures;
using ReVitae.Core.Export.Pdf;
using ReVitae.Tests.Import.Fixtures.JohnDoe;

namespace ReVitae.Tests.Import.Fixtures.JohnDoe;

public sealed class GeneratedJohnDoeVariantFileEdgeCaseTests
{
	[Fact]
	public void Write_ProducesNonEmptyFileWithMatchingLength()
	{
		var spec = JohnDoeVariantCatalog.All.First(entry => entry.Id == "02");
		var document = JohnDoeStressCvDataset.CreateDocument(CvExportTemplateId.ClassicSidebar);
		var bytes = new QuestPdfCvExporter().Export(document);

		using var generated = GeneratedJohnDoeVariantFile.Write(spec, bytes);

		Assert.True(File.Exists(generated.Path));
		Assert.Equal(bytes.Length, generated.ByteLength);
		Assert.Equal(bytes.Length, new FileInfo(generated.Path).Length);
		var header = File.ReadAllBytes(generated.Path);
		Assert.Equal((byte)'%', header[0]);
		Assert.Equal((byte)'P', header[1]);
		Assert.Equal((byte)'D', header[2]);
		Assert.Equal((byte)'F', header[3]);
	}

	[Fact]
	public void Write_EmptyBytes_ThrowsBeforeCreatingFile()
	{
		var spec = JohnDoeVariantCatalog.All.First(entry => entry.Id == "01");

		var exception = Assert.Throws<InvalidOperationException>(() =>
			GeneratedJohnDoeVariantFile.Write(spec, ReadOnlyMemory<byte>.Empty));

		Assert.Contains("empty bytes", exception.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void WriteText_EmptyContent_Throws()
	{
		var spec = JohnDoeVariantCatalog.All.First(entry => entry.Id == "11");

		var exception = Assert.Throws<InvalidOperationException>(() =>
			GeneratedJohnDoeVariantFile.WriteText(spec, string.Empty));

		Assert.Contains("empty text", exception.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void Dispose_DeletesFileAndDirectory()
	{
		var spec = JohnDoeVariantCatalog.All.First(entry => entry.Id == "01");
		string path;
		string directory;

		using (var generated = GeneratedJohnDoeVariantFile.Write(spec, [0x25, 0x50, 0x44, 0x46, 0x0A]))
		{
			path = generated.Path;
			directory = generated.TempDirectory;
			Assert.True(File.Exists(path));
		}

		Assert.False(File.Exists(path));
		Assert.False(Directory.Exists(directory));
	}

	[Fact]
	public void VerifyWrittenFile_MissingFile_Throws()
	{
		var exception = Assert.Throws<InvalidOperationException>(() =>
			GeneratedJohnDoeVariantFile.VerifyWrittenFile(
				Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.pdf"),
				10));

		Assert.Contains("does not exist", exception.Message, StringComparison.OrdinalIgnoreCase);
	}
}
