using ReVitae.Core.Import;
using ReVitae.Tests.Import.Fixtures.JohnDoe;

namespace ReVitae.Tests.Import;

[Trait("Category", "ImportMatrix")]
[Collection(nameof(ImportPdfSerialCollection))]
public sealed class JohnDoeImportRegressionMatrixTests : IDisposable
{
	public static IEnumerable<object[]> AllVariants =>
		JohnDoeVariantCatalog.All.Select(spec => new object[] { spec });

	[Theory]
	[MemberData(nameof(AllVariants))]
	public void Import_JohnDoeVariant_RecoversCanonicalData(JohnDoeVariantSpec spec)
	{
		using var generated = JohnDoeVariantGenerator.Generate(spec);
		Assert.True(File.Exists(generated.Path), $"Generated file missing for variant {spec.Id}.");

		var result = CvDocumentImporter.Import(generated.Path);
		JohnDoeImportAssertions.AssertMatchesExpectations(result, spec);
	}

	[Fact]
	public void Generate_DisposeDeletesTempCvFileAndDirectory()
	{
		var spec = JohnDoeVariantCatalog.All[0];
		string path;
		string directory;

		using (var generated = JohnDoeVariantGenerator.Generate(spec))
		{
			path = generated.Path;
			directory = generated.TempDirectory;
			Assert.True(File.Exists(path));
			Assert.True(Directory.Exists(directory));
		}

		Assert.False(File.Exists(path));
		Assert.False(Directory.Exists(directory));
	}

	[Fact]
	public void Catalog_ContainsFiftyOneUniqueVariants() => Assert.Equal(51, JohnDoeVariantCatalog.All.Count);

	public void Dispose() => JohnDoeMatrixTempDirectory.CleanupStaleRoots(TimeSpan.FromHours(1));
}
