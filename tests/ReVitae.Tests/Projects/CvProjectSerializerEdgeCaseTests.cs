using ReVitae.Core.Export;
using ReVitae.Core.Import;
using ReVitae.Core.Localization;
using ReVitae.Core.Projects;

namespace ReVitae.Tests.Projects;

public sealed class CvProjectSerializerEdgeCaseTests : IDisposable
{
    private readonly string _root;

    public CvProjectSerializerEdgeCaseTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "revitae-project-edge", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void Load_InvalidJson_ReturnsFailure()
    {
        var path = Path.Combine(_root, "invalid.revitae.json");
        File.WriteAllText(path, "{ not-json");

        var loaded = CvProjectSerializer.Load(path);

        Assert.False(loaded.Success);
    }

    [Fact]
    public void Load_EmptyShell_ReturnsNoStructuredDataError()
    {
        var path = Path.Combine(_root, "empty.revitae.json");
        File.WriteAllText(path, """{ "revitaeVersion": 1 }""");

        var loaded = CvProjectSerializer.Load(path);

        Assert.False(loaded.Success);
        Assert.Equal(TranslationKeys.ImportErrorNoStructuredData, loaded.ErrorMessageKey);
    }

    [Fact]
    public void Load_UnsupportedVersion_ReturnsUnsupportedFormatError()
    {
        var path = Path.Combine(_root, "v99.revitae.json");
        File.WriteAllText(path, """
            {
              "revitaeVersion": 99,
              "personalInformation": { "firstName": "Future" }
            }
            """);

        var loaded = CvProjectSerializer.Load(path);

        Assert.False(loaded.Success);
        Assert.Equal(TranslationKeys.ImportErrorUnsupportedStructuredFormat, loaded.ErrorMessageKey);
    }

    [Fact]
    public void SaveLoad_SectionExpandState_AllKnownSections()
    {
        var path = Path.Combine(_root, "expand.revitae.json");
        var expand = new Dictionary<string, bool>(StringComparer.Ordinal)
        {
            ["personalInformation"] = true,
            ["workExperience"] = false,
            ["education"] = true,
            ["skills"] = false,
            ["languages"] = true,
            ["certificates"] = false,
            ["projects"] = true,
            ["links"] = false,
            ["additionalInformation"] = true
        };

        CvProjectSerializer.Save(path, new CvProjectSaveRequest(
            CvExportSourceDataFactory.Create(
                new PersonalInformationImport { FirstName = "Expand", LastName = "Test" },
                [], [], [], [], [], [], [], null),
            new CvProjectSettings(
                CvProjectConstants.CurrentProjectSettingsSchemaVersion,
                CvExportTemplateId.PeachDesigner,
                [],
                expand,
                null,
                CvProjectApplicationInfo.Version)));

        var loaded = CvProjectSerializer.Load(path);

        Assert.True(loaded.Success);
        Assert.Equal(expand, loaded.Settings!.SectionExpandState);
        Assert.Equal(CvExportTemplateId.PeachDesigner, loaded.Settings.SelectedTemplateId);
    }

    [Fact]
    public void Save_UsesAtomicWrite_NoTempFileLeftBehind()
    {
        var path = Path.Combine(_root, "atomic.revitae.json");
        CvProjectSerializer.Save(path, new CvProjectSaveRequest(
            CvExportSourceDataFactory.Create(
                new PersonalInformationImport { FirstName = "Atomic" },
                [], [], [], [], [], [], [], null),
            CvProjectSettings.CreateDefault(CvExportTemplateId.CleanTopHeader)));

        Assert.True(File.Exists(path));
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void Load_ProjectSettingsMissingSchemaVersion_MarksMalformed()
    {
        var path = Path.Combine(_root, "no-schema.revitae.json");
        File.WriteAllText(path, """
            {
              "revitaeVersion": 1,
              "personalInformation": { "firstName": "NoSchema" },
              "projectSettings": { "selectedTemplateId": "modernSidebar" }
            }
            """);

        var loaded = CvProjectSerializer.Load(path);

        Assert.True(loaded.Success);
        Assert.True(loaded.SettingsMalformed);
        Assert.Null(loaded.Settings);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
        catch
        {
            // Temp cleanup best effort.
        }
    }
}
