using ReVitae.Core.AppPreferences;
using ReVitae.Core.Ai.Providers;

namespace ReVitae.Tests.AppPreferences;

public sealed class AppPreferencesRepositoryEdgeCaseTests : IDisposable
{
	private readonly AppPreferencesTestPaths _paths = new();

	public void Dispose() => _paths.Dispose();

	[Fact]
	public void SaveAndLoad_RoundTripsV2Document()
	{
		var repository = _paths.CreateRepository();
		var completedAt = new DateTimeOffset(2026, 5, 21, 12, 0, 0, TimeSpan.Zero);
		var document = new AppPreferencesDocument(
			AppPreferencesDocument.CurrentSchemaVersion,
			FirstLaunchAiWizardStatus.RemindLater,
			completedAt,
			HideAiPromotionsInUi: false);

		repository.Save(document);
		var loaded = repository.LoadOrDefault();

		Assert.Equal(AppPreferencesDocument.CurrentSchemaVersion, loaded.SchemaVersion);
		Assert.Equal(FirstLaunchAiWizardStatus.RemindLater, loaded.FirstLaunchAiWizardStatus);
		Assert.Equal(completedAt, loaded.FirstLaunchAiWizardCompletedAtUtc);
		Assert.False(loaded.HideAiPromotionsInUi);
		Assert.True(File.Exists(_paths.AppSettingsPath));
	}

	[Fact]
	public void LoadOrDefault_MissingFile_ReturnsDefaultWithCurrentSchema()
	{
		var loaded = _paths.CreateRepository().LoadOrDefault();

		Assert.Equal(AppPreferencesDocument.CurrentSchemaVersion, loaded.SchemaVersion);
		Assert.Equal(FirstLaunchAiWizardStatus.NotStarted, loaded.FirstLaunchAiWizardStatus);
		Assert.Null(loaded.FirstLaunchAiWizardCompletedAtUtc);
		Assert.False(loaded.HideAiPromotionsInUi);
	}

	[Fact]
	public void LoadOrDefault_CorruptJson_ReturnsDefaultWithoutThrowing()
	{
		File.WriteAllText(_paths.AppSettingsPath, "{ not valid json");

		var loaded = _paths.CreateRepository().LoadOrDefault();

		Assert.Equal(FirstLaunchAiWizardStatus.NotStarted, loaded.FirstLaunchAiWizardStatus);
	}

	[Fact]
	public void LoadOrDefault_InvalidEnumValue_TreatedAsNotStarted()
	{
		File.WriteAllText(
			_paths.AppSettingsPath,
			"""
			{
			  "schemaVersion": 2,
			  "firstLaunchAiWizardStatus": 999,
			  "hideAiPromotionsInUi": true
			}
			""");

		var loaded = _paths.CreateRepository().LoadOrDefault();

		Assert.Equal(FirstLaunchAiWizardStatus.NotStarted, loaded.FirstLaunchAiWizardStatus);
		Assert.True(loaded.HideAiPromotionsInUi);
	}

	[Fact]
	public void LoadOrDefault_HideAiPromotionsNull_DefaultsFalse()
	{
		File.WriteAllText(
			_paths.AppSettingsPath,
			"""
			{
			  "schemaVersion": 2,
			  "firstLaunchAiWizardStatus": 1
			}
			""");

		var loaded = _paths.CreateRepository().LoadOrDefault();

		Assert.False(loaded.HideAiPromotionsInUi);
	}

	[Fact]
	public void ApplyUpgradeMigration_ActiveLocalBackend_AutoCompletesWizard()
	{
		WriteActiveLocalAiSettings();

		var loaded = _paths.CreateRepository().LoadOrDefault();

		Assert.Equal(FirstLaunchAiWizardStatus.Completed, loaded.FirstLaunchAiWizardStatus);
		Assert.NotNull(loaded.FirstLaunchAiWizardCompletedAtUtc);
		Assert.False(loaded.HideAiPromotionsInUi);
	}

	[Fact]
	public void ApplyUpgradeMigration_ActiveOnlineBackend_AutoCompletesWizard()
	{
		new AiSettingsRepository(_paths.AiSettingsPath).Save(AiSettingsDocument.Empty with
		{
			ActiveBackend = AiBackendKind.Online,
			ActiveOnlineProviderId = "openai",
		});

		var loaded = _paths.CreateRepository().LoadOrDefault();

		Assert.Equal(FirstLaunchAiWizardStatus.Completed, loaded.FirstLaunchAiWizardStatus);
	}

	[Theory]
	[InlineData("Downloading")]
	[InlineData("Paused")]
	[InlineData("Interrupted")]
	[InlineData("Failed")]
	public void ApplyUpgradeMigration_ResumableDownloadJob_AutoCompletesWizard(string state)
	{
		WriteDownloadJob(state);

		var loaded = _paths.CreateRepository().LoadOrDefault();

		Assert.Equal(FirstLaunchAiWizardStatus.Completed, loaded.FirstLaunchAiWizardStatus);
	}

	[Theory]
	[InlineData("Completed")]
	[InlineData("Idle")]
	[InlineData("Cancelled")]
	public void ApplyUpgradeMigration_NonResumableDownloadJob_DoesNotAutoComplete(string state)
	{
		WriteDownloadJob(state);

		var loaded = _paths.CreateRepository().LoadOrDefault();

		Assert.Equal(FirstLaunchAiWizardStatus.NotStarted, loaded.FirstLaunchAiWizardStatus);
	}

	[Fact]
	public void ApplyUpgradeMigration_CorruptDownloadJob_DoesNotAutoComplete()
	{
		File.WriteAllText(_paths.AiDownloadJobPath, "{ broken");

		var loaded = _paths.CreateRepository().LoadOrDefault();

		Assert.Equal(FirstLaunchAiWizardStatus.NotStarted, loaded.FirstLaunchAiWizardStatus);
	}

	[Fact]
	public void ApplyUpgradeMigration_DownloadJobMissingState_DoesNotAutoComplete()
	{
		File.WriteAllText(_paths.AiDownloadJobPath, """{"modelId":"gemma2-2b"}""");

		var loaded = _paths.CreateRepository().LoadOrDefault();

		Assert.Equal(FirstLaunchAiWizardStatus.NotStarted, loaded.FirstLaunchAiWizardStatus);
	}

	[Fact]
	public void ApplyUpgradeMigration_AlreadyRemindLater_DoesNotOverwriteStatus()
	{
		WriteActiveLocalAiSettings();
		File.WriteAllText(
			_paths.AppSettingsPath,
			"""
			{
			  "schemaVersion": 1,
			  "firstLaunchAiWizardStatus": 1,
			  "firstLaunchAiWizardCompletedAtUtc": "2026-05-20T08:00:00Z",
			  "hideAiPromotionsInUi": false
			}
			""");

		var loaded = _paths.CreateRepository().LoadOrDefault();

		Assert.Equal(FirstLaunchAiWizardStatus.RemindLater, loaded.FirstLaunchAiWizardStatus);
		Assert.Equal(new DateTimeOffset(2026, 5, 20, 8, 0, 0, TimeSpan.Zero), loaded.FirstLaunchAiWizardCompletedAtUtc);
	}

	[Fact]
	public void ApplyUpgradeMigration_DeclinedOffline_PreservesHidePromotionsFlag()
	{
		File.WriteAllText(
			_paths.AppSettingsPath,
			"""
			{
			  "schemaVersion": 1,
			  "firstLaunchAiWizardStatus": 3,
			  "hideAiPromotionsInUi": true
			}
			""");

		var loaded = _paths.CreateRepository().LoadOrDefault();

		Assert.Equal(FirstLaunchAiWizardStatus.DeclinedOffline, loaded.FirstLaunchAiWizardStatus);
		Assert.True(loaded.HideAiPromotionsInUi);
		Assert.Equal(AppPreferencesDocument.CurrentSchemaVersion, loaded.SchemaVersion);
	}

	[Fact]
	public void Save_WritesThroughTempFile()
	{
		var repository = _paths.CreateRepository();
		repository.Save(AppPreferencesDocument.Default with
		{
			FirstLaunchAiWizardStatus = FirstLaunchAiWizardStatus.Completed,
		});

		Assert.False(File.Exists(_paths.AppSettingsPath + ".tmp"));
		Assert.Contains(
			"\"firstLaunchAiWizardStatus\": 2",
			File.ReadAllText(_paths.AppSettingsPath),
			StringComparison.Ordinal);
	}

	private void WriteActiveLocalAiSettings()
	{
		new AiSettingsRepository(_paths.AiSettingsPath).Save(AiSettingsDocument.Empty with
		{
			ActiveBackend = AiBackendKind.Local,
			ActiveLocalModelId = "gemma2-2b",
		});
	}

	private void WriteDownloadJob(string state)
	{
		File.WriteAllText(
			_paths.AiDownloadJobPath,
			$$"""
			{
			  "state": "{{state}}",
			  "modelId": "gemma2-2b"
			}
			""");
	}
}
