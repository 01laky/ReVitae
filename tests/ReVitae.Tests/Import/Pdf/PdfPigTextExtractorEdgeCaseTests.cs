using ReVitae.Core.Import.Pdf;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Import.Pdf;

public sealed class PdfPigTextExtractorEdgeCaseTests
{
	private static string FixturePath(string fileName) =>
		Path.Combine(AppContext.BaseDirectory, "Import", "Fixtures", "Pdf", fileName);

	[Fact]
	public void Extract_MissingFile_ReturnsFileNotFound()
	{
		var extractor = new PdfPigTextExtractor();

		var result = extractor.Extract(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".pdf"));

		Assert.False(result.Success);
		Assert.Equal(TranslationKeys.ImportErrorFileNotFound, result.ErrorMessageKey);
	}

	[Fact]
	public void Extract_EmptyPath_ReturnsFileNotFound()
	{
		var extractor = new PdfPigTextExtractor();

		var result = extractor.Extract(string.Empty);

		Assert.False(result.Success);
		Assert.Equal(TranslationKeys.ImportErrorFileNotFound, result.ErrorMessageKey);
	}

	[Fact]
	public void Extract_ValidFixture_ReturnsText()
	{
		var path = FixturePath("JohnDoeMinimalArchitect.pdf");
		if (!File.Exists(path))
		{
			// Fallback to repo-root sample when fixture copy layout differs.
			path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "John Doe (minimal architect).pdf"));
		}

		if (!File.Exists(path))
		{
			return;
		}

		var extractor = new PdfPigTextExtractor();
		var result = extractor.Extract(path);

		Assert.True(result.Success);
		Assert.False(string.IsNullOrWhiteSpace(result.Text));
		Assert.True(result.PageCount > 0);
	}

	[Fact]
	public void Extract_WhitespaceOnlyPdf_ReturnsNoTextError()
	{
		var path = CreateBlankPdf();
		try
		{
			var extractor = new PdfPigTextExtractor();
			var result = extractor.Extract(path);

			Assert.False(result.Success);
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Fact]
	public void Extract_InvalidBytes_ReturnsFailure()
	{
		var path = Path.Combine(Path.GetTempPath(), $"invalid-{Guid.NewGuid():N}.pdf");
		File.WriteAllText(path, "not a pdf");
		try
		{
			var extractor = new PdfPigTextExtractor();
			var result = extractor.Extract(path);

			Assert.False(result.Success);
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Fact]
	public void Extract_CollectsHyperlinksWhenPresent()
	{
		var path = FixturePath("JohnDoeMinimalArchitect.pdf");
		if (!File.Exists(path))
		{
			return;
		}

		var extractor = new PdfPigTextExtractor();
		var result = extractor.Extract(path);

		Assert.True(result.Success);
		Assert.NotNull(result.HyperlinkUrls);
	}

	private static string CreateBlankPdf()
	{
		var path = Path.Combine(Path.GetTempPath(), $"blank-{Guid.NewGuid():N}.pdf");
		File.WriteAllBytes(path, Convert.FromBase64String(
			"JVBERi0xLjQKJeLjz9MKMSAwIG9iago8PAovVHlwZSAvQ2F0YWxvZwovUGFnZXMgMiAwIFIKPj4KZW5kb2JqCjIgMCBvYmoKPDwKL1R5cGUgL1BhZ2VzCi9LaWRzIFszIDAgUl0KL0NvdW50IDEKPj4KZW5kb2JqCjMgMCBvYmoKPDwKL1R5cGUgL1BhZ2UKL1BhcmVudCAyIDAgUgovTWVkaWFCb3ggWzAgMCAzIDNdCi9Db250ZW50cyA0IDAgUgo+PgplbmRvYmoKNCAwIG9iago8PAovTGVuZ3RoIDQ0Cj4+CnN0cmVhbQpCVCAKRVQKZW5kb3N0cmVhbQplbmRvYmoKeHJlZgowIDUKMDAwMDAwMDAwMCA2NTUzNSBmIAowMDAwMDAwMDA5IDAwMDAwIG4gCjAwMDAwMDAwNTggMDAwMDAgbiAKMDAwMDAwMDExNSAwMDAwMCBuIAowMDAwMDAwMjA0IDAwMDAwIG4gCnRyYWlsZXIKPDwKL1NpemUgNQovUm9vdCAxIDAgUgo+PgpzdGFydHhyZWYKMjk3CiUlRU9GCg=="));
		return path;
	}
}
