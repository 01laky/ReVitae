using ReVitae.Core.Export;
using ReVitae.Core.Import;
using ReVitae.Core.Projects;

namespace ReVitae.Tests.Projects;

public sealed class CvProjectServiceTests : IDisposable
{
	private readonly string _root;

	public CvProjectServiceTests()
	{
		_root = Path.Combine(Path.GetTempPath(), "revitae-service-tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_root);
	}

	[Fact]
	public void SaveThenLoad_PreservesIdentity()
	{
		var path = Path.Combine(_root, "roundtrip.revitae.json");
		var request = new CvProjectSaveRequest(
			CreateSource(),
			CvProjectSettings.CreateDefault(CvExportTemplateId.ModernSidebar));

		CvProjectService.Save(path, request);
		var loaded = CvProjectService.Load(path);

		Assert.True(loaded.Success);
		Assert.Equal("Jane", loaded.Import!.Personal.FirstName);
		Assert.Equal(CvExportTemplateId.ModernSidebar, loaded.Settings!.SelectedTemplateId);
	}

	[Fact]
	public void RecoverySerializerRoundTrip_WritesReadableRecoveryFile()
	{
		var recoveryPath = Path.Combine(_root, "autosave.recovery.revitae.json");
		var request = new CvProjectSaveRequest(
			CreateSource(),
			CvProjectSettings.CreateDefault(CvExportTemplateId.ClassicSidebar));

		CvProjectSerializer.Save(recoveryPath, request);
		Assert.True(File.Exists(recoveryPath));

		var loaded = CvProjectSerializer.Load(recoveryPath);
		Assert.True(loaded.Success);
		Assert.Equal("Jane", loaded.Import!.Personal.FirstName);

		File.Delete(recoveryPath);
		Assert.False(File.Exists(recoveryPath));
	}

	[Fact]
	public void ExportInterchangeThenProjectLoad_UsesDefaultEditorSettings()
	{
		var interchangePath = Path.Combine(_root, "export.revitae.json");
		File.WriteAllText(interchangePath, """
            {
              "revitaeVersion": 1,
              "personalInformation": {
                "firstName": "Export",
                "lastName": "Only"
              },
              "workExperience": [
                {
                  "jobTitle": "Dev",
                  "company": "Org"
                }
              ]
            }
            """);

		var loaded = CvProjectService.Load(interchangePath);

		Assert.True(loaded.Success);
		Assert.Null(loaded.Settings);
		Assert.Single(loaded.Import!.WorkExperienceEntries);
	}

	[Fact]
	public void SaveProjectWithDismissedHints_ReloadPreservesKeys()
	{
		var path = Path.Combine(_root, "hints.revitae.json");
		var settings = new CvProjectSettings(
			CvProjectConstants.CurrentProjectSettingsSchemaVersion,
			CvExportTemplateId.ModernSidebar,
			["work.generic-description||description", "personal.summary-too-short||shortSummary"],
			null,
			null,
			CvProjectApplicationInfo.Version);

		CvProjectService.Save(path, new CvProjectSaveRequest(CreateSource(), settings));
		var loaded = CvProjectService.Load(path);

		Assert.Equal(2, loaded.Settings!.DismissedQualityHintKeys.Count);
		Assert.Contains("work.generic-description||description", loaded.Settings.DismissedQualityHintKeys);
	}

	private static CvExportSourceData CreateSource() =>
		CvExportSourceDataFactory.Create(
			new PersonalInformationImport
			{
				FirstName = "Jane",
				LastName = "Smith",
				Email = "jane@example.com",
				ShortSummary = "Platform engineer with cloud experience."
			},
			[],
			[],
			[],
			[],
			[],
			[],
			[],
			null);

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
