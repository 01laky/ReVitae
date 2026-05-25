using ReVitae.Core.Import;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Import;

public sealed class CvDocumentImporterEdgeCaseTests
{
	private const string MinimalJsonResume = """
        {
          "basics": {
            "name": "Jane Doe",
            "email": "jane@example.com",
            "summary": "Backend developer."
          }
        }
        """;

	private const string MinimalTextCv = """
        Jane Doe
        jane@example.com

        Work Experience
        Developer at Acme
        2020 - 2024
        """;

	[Fact]
	public void Import_ReturnsFileNotFoundForMissingPath()
	{
		var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
		var result = CvDocumentImporter.Import(missing);

		Assert.False(result.Success);
		Assert.Equal(TranslationKeys.ImportErrorFileNotFound, result.ErrorMessageKey);
	}

	[Fact]
	public void Import_ReturnsFileNotFoundForWhitespacePath()
	{
		var result = CvDocumentImporter.Import("  ");

		Assert.False(result.Success);
		Assert.Equal(TranslationKeys.ImportErrorFileNotFound, result.ErrorMessageKey);
	}

	[Fact]
	public void Import_ReturnsFileTooLargeWhenPayloadExceedsLimit()
	{
		using var dir = new TempImportDirectory();
		var path = Path.Combine(dir.RootPath, "huge.pdf");
		using (var fs = File.Create(path))
		{
			fs.SetLength(CvImportLimits.MaxFileBytes + 1);
		}

		var result = CvDocumentImporter.Import(path);

		Assert.False(result.Success);
		Assert.Equal(TranslationKeys.ImportErrorFileTooLarge, result.ErrorMessageKey);
	}

	[Fact]
	public void Import_NormalizesEmptyPdfErrorToGenericEmptyDocument()
	{
		using var dir = new TempImportDirectory();
		var path = dir.FilePath("blank.pdf", MinimalPdfWriter.CreateFromLines([]));

		var result = CvDocumentImporter.Import(path);

		Assert.False(result.Success);
		Assert.Equal(TranslationKeys.ImportErrorEmptyDocument, result.ErrorMessageKey);
	}

	[Fact]
	public void Import_ReturnsUnsupportedFormatForUnknownExtension()
	{
		using var dir = new TempImportDirectory();
		var path = dir.FilePath("odd.ext", MinimalTextCv);

		var result = CvDocumentImporter.Import(path);

		Assert.False(result.Success);
		Assert.Equal(TranslationKeys.ImportErrorUnsupportedFormat, result.ErrorMessageKey);
	}

	[Fact]
	public void Import_ReturnsUnsupportedFormatForUnrecognizedJsonShape()
	{
		using var dir = new TempImportDirectory();
		var path = dir.FilePath("settings.json", """{"featureFlags":{"darkMode":true}}""");

		var result = CvDocumentImporter.Import(path);

		Assert.False(result.Success);
		Assert.Equal(TranslationKeys.ImportErrorUnsupportedFormat, result.ErrorMessageKey);
	}

	[Fact]
	public void Import_ReturnsUnreadableStructuredDocumentForMalformedRevitaeSuffixFile()
	{
		using var dir = new TempImportDirectory();
		var path = dir.FilePath("broken.revitae.json", "{");

		var result = CvDocumentImporter.Import(path);

		Assert.False(result.Success);
		Assert.Equal(TranslationKeys.ImportErrorUnreadableDocument, result.ErrorMessageKey);
	}

	[Fact]
	public void Import_HappyPath_TextPopulatesParsedCv()
	{
		using var dir = new TempImportDirectory();
		var path = dir.FilePath("cv.txt", MinimalTextCv);

		var result = CvDocumentImporter.Import(path);

		Assert.True(result.Success);
		Assert.Equal("jane@example.com", result.Personal.Email);
		Assert.NotEmpty(result.WorkExperienceEntries);
	}

	[Fact]
	public void Import_HappyPath_JsonResumePopulatesStructuredCv()
	{
		using var dir = new TempImportDirectory();
		var path = dir.FilePath("resume.json", MinimalJsonResume);

		var result = CvDocumentImporter.Import(path);

		Assert.True(result.Success);
		Assert.Equal("jane@example.com", result.Personal.Email);
		Assert.Equal("Backend developer.", result.Personal.ShortSummary);
	}

	[Fact]
	public void Import_ReturnsEmptyDocumentForWhitespaceOnlyTextFile()
	{
		using var dir = new TempImportDirectory();
		var path = dir.FilePath("blank.txt", " \n\t ");

		var result = CvDocumentImporter.Import(path);

		Assert.False(result.Success);
		Assert.Equal(TranslationKeys.ImportErrorEmptyDocument, result.ErrorMessageKey);
	}
}
