using System.Text;
using System.Text.Json;
using ReVitae.Core.Cv.ProfilePhoto;
using ReVitae.Core.Export;
using ReVitae.Core.Import;
using ReVitae.Core.Import.Structured;

namespace ReVitae.Tests.Export;

public sealed class CvStructuredExportWriterPhotoTests : IDisposable
{
    private readonly string _tempDirectory = ProfilePhotoTestHelpers.CreateTempDirectory();

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup for temp test directories.
        }
    }

    [Fact]
    public void Export_RevitaeJson_WithoutPhoto_UsesVersionOne()
    {
        var document = CvExportTestFixtures.CreatePersonalInfoOnlyDocument();
        var source = CvExportTestFixtures.CreatePersonalOnlySourceData();
        var json = ExportAsText(document, source, CvExportFormat.RevitaeJson);
        using var parsed = JsonDocument.Parse(json);

        Assert.Equal(1, parsed.RootElement.GetProperty("revitaeVersion").GetInt32());
        Assert.False(parsed.RootElement.GetProperty("personalInformation").TryGetProperty("profilePhotoBase64", out _));
    }

    [Fact]
    public void Export_RevitaeJson_WithPhoto_UsesVersionTwoAndEmbedsBase64()
    {
        var (document, source) = CreateExportPairWithPhoto();
        var json = ExportAsText(document, source, CvExportFormat.RevitaeJson);
        using var parsed = JsonDocument.Parse(json);

        Assert.Equal(2, parsed.RootElement.GetProperty("revitaeVersion").GetInt32());
        var personal = parsed.RootElement.GetProperty("personalInformation");
        Assert.True(personal.TryGetProperty("profilePhotoBase64", out var encoded));
        Assert.False(string.IsNullOrWhiteSpace(encoded.GetString()));
        Assert.Equal("image/png", personal.GetProperty("profilePhotoContentType").GetString());
        Assert.False(json.Contains("profilePhotoPath", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Export_RevitaeJson_WithPhoto_RoundTripsThroughMapper()
    {
        var (document, source) = CreateExportPairWithPhoto();
        var json = ExportAsText(document, source, CvExportFormat.RevitaeJson);

        var import = ReVitaeJsonMapper.Map(json);

        Assert.True(import.Success);
        Assert.Equal("Jane", import.Personal.FirstName);
        Assert.True(ProfilePhotoStorage.FileExists(import.Personal.ProfilePhotoPath));
    }

    [Fact]
    public void Export_RevitaeYaml_WithoutPhoto_ImportsThroughDocumentImporter()
    {
        var document = CvExportTestFixtures.CreatePersonalInfoOnlyDocument();
        var source = CvExportTestFixtures.CreatePersonalOnlySourceData();
        var bytes = ExportToBytes(document, source, CvExportFormat.Yaml);
        var tempPath = Path.Combine(_tempDirectory, "no-photo.yaml");
        File.WriteAllBytes(tempPath, bytes);

        var import = CvDocumentImporter.Import(tempPath);

        Assert.True(import.Success, import.ErrorMessageKey ?? Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public void Export_RevitaeYaml_WithPhoto_RoundTripsThroughDocumentImporter()
    {
        var (document, source) = CreateExportPairWithPhoto();
        var bytes = ExportToBytes(document, source, CvExportFormat.Yaml);
        var tempPath = Path.Combine(_tempDirectory, $"photo-{Guid.NewGuid():N}.yaml");
        File.WriteAllBytes(tempPath, bytes);

        var import = CvDocumentImporter.Import(tempPath);

        Assert.True(import.Success, import.ErrorMessageKey ?? Encoding.UTF8.GetString(bytes));
        Assert.True(ProfilePhotoStorage.FileExists(import.Personal.ProfilePhotoPath));
    }

    private (CvExportDocument Document, CvExportSourceData Source) CreateExportPairWithPhoto()
    {
        var storage = new ProfilePhotoStorage(_tempDirectory);
        var png = ProfilePhotoTestHelpers.WriteMinimalPng(_tempDirectory);
        var saved = storage.TrySaveCopy(png);
        Assert.True(saved.Success);

        var document = CvExportTestFixtures.CreatePersonalInfoOnlyDocument() with { PhotoPath = saved.StoredPath };
        var personal = new PersonalInformationImport
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@example.com",
            ProfilePhotoPath = saved.StoredPath!
        };
        var source = new CvExportSourceData(personal, [], [], [], [], [], [], [], null);
        return (document, source);
    }

    private static string ExportAsText(CvExportDocument document, CvExportSourceData source, CvExportFormat format)
    {
        return Encoding.UTF8.GetString(ExportToBytes(document, source, format));
    }

    private static byte[] ExportToBytes(CvExportDocument document, CvExportSourceData source, CvExportFormat format)
    {
        using var stream = new MemoryStream();
        var result = CvDocumentExporter.Export(document, source, format, stream);
        Assert.True(result.Success);
        return stream.ToArray();
    }
}
