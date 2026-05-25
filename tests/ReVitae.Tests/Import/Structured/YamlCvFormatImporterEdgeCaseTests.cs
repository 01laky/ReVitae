using ReVitae.Core.Import.Importers;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Import.Structured;

public sealed class YamlCvFormatImporterEdgeCaseTests
{
    [Fact]
    public void Import_EmptyFile_ReturnsImportErrorEmptyDocument()
    {
        var path = WriteTempYaml("   \n\t  ");
        var importer = new YamlCvFormatImporter();

        var result = importer.Import(path);

        Assert.False(result.Success);
        Assert.Equal(TranslationKeys.ImportErrorEmptyDocument, result.ErrorMessageKey);
    }

    [Fact]
    public void Import_UnsupportedYamlRoot_ReturnsImportErrorUnsupportedStructuredFormat()
    {
        var path = WriteTempYaml("""
            unrelated:
              field: value
            """);
        var importer = new YamlCvFormatImporter();

        var result = importer.Import(path);

        Assert.False(result.Success);
        Assert.Equal(TranslationKeys.ImportErrorUnsupportedStructuredFormat, result.ErrorMessageKey);
    }

    [Fact]
    public void Import_JsonResumeYamlShape_ReturnsSuccess()
    {
        var path = WriteTempYaml("""
            basics:
              name: Jane Doe
              email: jane@example.com
              summary: Backend developer.
            """);
        var importer = new YamlCvFormatImporter();

        var result = importer.Import(path);

        Assert.True(result.Success);
        Assert.Equal("Jane", result.Personal.FirstName);
        Assert.Equal("Doe", result.Personal.LastName);
    }

    private static string WriteTempYaml(string yaml)
    {
        var path = Path.Combine(Path.GetTempPath(), $"revitae-yaml-{Guid.NewGuid():N}.yaml");
        File.WriteAllText(path, yaml);
        return path;
    }
}
