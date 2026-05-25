using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ReVitae.Core.Import.Extraction;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Import.TextExtractors;

public sealed class DocxTextExtractorTests
{
	[Fact]
	public void Extract_ReturnsFileNotFoundForMissingPath()
	{
		var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".docx");
		var result = new DocxTextExtractor().Extract(missing);

		Assert.False(result.Success);
		Assert.Equal(TranslationKeys.ImportErrorFileNotFound, result.ErrorMessageKey);
	}

	[Fact]
	public void Extract_ReadsParagraphText_FromMinimalDocx()
	{
		using var dir = new TempImportDirectory();
		var path = Path.Combine(dir.RootPath, "minimal.docx");
		WriteMinimalDocx(path, "Jane Doe", "jane@example.com");

		var result = new DocxTextExtractor().Extract(path);

		Assert.True(result.Success);
		Assert.Contains("Jane Doe", result.Text, StringComparison.Ordinal);
		Assert.Contains("jane@example.com", result.Text, StringComparison.Ordinal);
	}

	private static void WriteMinimalDocx(string path, params string[] lines)
	{
		using WordprocessingDocument document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
		MainDocumentPart main = document.AddMainDocumentPart();
		main.Document = new Document(new Body(lines.Select(static line =>
			new Paragraph(new Run(new Text(line))))));
		main.Document.Save();
	}
}
