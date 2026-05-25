namespace ReVitae.Tests.Import;

public sealed class ImportPdfSerialCollectionEdgeCaseTests
{
	[Fact]
	public void ImportPdfReimportTests_UseSerialCollection()
	{
		AssertHasCollection<ReVitaePdfReimportEdgeCaseTests>();
		AssertHasCollection<ImportPdfReimportStabilityEdgeCaseTests>();
		AssertHasCollection<JohnDoeImportRegressionMatrixTests>();
	}

	[Fact]
	public void SerialCollectionDefinition_DisablesParallelization()
	{
		var definition = typeof(ImportPdfSerialCollection)
			.GetCustomAttributes(typeof(CollectionDefinitionAttribute), inherit: false)
			.Cast<CollectionDefinitionAttribute>()
			.Single();

		Assert.True(definition.DisableParallelization);
	}

	private static void AssertHasCollection<T>()
	{
		Assert.NotEmpty(typeof(T).GetCustomAttributes(typeof(CollectionAttribute), inherit: false));
	}
}
