using System.Text.Json;
using ReVitae.Core.Ai;
using ReVitae.Core.Ai.Download;
using ReVitae.Core.Ai.Providers;

namespace ReVitae.Core.AppPreferences;

internal sealed record AppPreferencesDocumentDto(
	int SchemaVersion,
	int FirstLaunchAiWizardStatus,
	DateTimeOffset? FirstLaunchAiWizardCompletedAtUtc,
	bool? HideAiPromotionsInUi);

public sealed class AppPreferencesRepository
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = true,
	};

	private readonly string _filePath;
	private readonly string _aiSettingsFilePath;
	private readonly string _aiDownloadJobFilePath;

	public AppPreferencesRepository()
		: this(ReVitaeLocalDataPaths.GetAppSettingsFilePath())
	{
	}

	public AppPreferencesRepository(string filePath)
		: this(
			filePath,
			ReVitaeLocalDataPaths.GetAiSettingsFilePath(),
			ReVitaeLocalDataPaths.GetAiDownloadJobFilePath())
	{
	}

	internal AppPreferencesRepository(
		string filePath,
		string aiSettingsFilePath,
		string aiDownloadJobFilePath)
	{
		_filePath = filePath;
		_aiSettingsFilePath = aiSettingsFilePath;
		_aiDownloadJobFilePath = aiDownloadJobFilePath;
	}

	public AppPreferencesDocument LoadOrDefault()
	{
		if (!File.Exists(_filePath))
		{
			return ApplyUpgradeMigration(AppPreferencesDocument.Default);
		}

		try
		{
			var json = File.ReadAllText(_filePath);
			var dto = JsonSerializer.Deserialize<AppPreferencesDocumentDto>(json, JsonOptions);
			if (dto is null)
			{
				return ApplyUpgradeMigration(AppPreferencesDocument.Default);
			}

			return ApplyUpgradeMigration(FromDto(dto));
		}
		catch
		{
			return ApplyUpgradeMigration(AppPreferencesDocument.Default);
		}
	}

	public void Save(AppPreferencesDocument document)
	{
		var directory = Path.GetDirectoryName(_filePath);
		if (!string.IsNullOrWhiteSpace(directory))
		{
			Directory.CreateDirectory(directory);
		}

		var dto = new AppPreferencesDocumentDto(
			AppPreferencesDocument.CurrentSchemaVersion,
			(int)document.FirstLaunchAiWizardStatus,
			document.FirstLaunchAiWizardCompletedAtUtc,
			document.HideAiPromotionsInUi);

		var json = JsonSerializer.Serialize(dto, JsonOptions);
		var tempPath = _filePath + ".tmp";
		File.WriteAllText(tempPath, json);
		File.Move(tempPath, _filePath, overwrite: true);
	}

	public AppPreferencesDocument ApplyUpgradeMigration(AppPreferencesDocument document)
	{
		if (document.FirstLaunchAiWizardStatus != FirstLaunchAiWizardStatus.NotStarted)
		{
			return document with { SchemaVersion = AppPreferencesDocument.CurrentSchemaVersion };
		}

		if (ShouldAutoCompleteFromExistingAiSetup())
		{
			var migrated = document with
			{
				SchemaVersion = AppPreferencesDocument.CurrentSchemaVersion,
				FirstLaunchAiWizardStatus = FirstLaunchAiWizardStatus.Completed,
				FirstLaunchAiWizardCompletedAtUtc = DateTimeOffset.UtcNow,
				HideAiPromotionsInUi = false,
			};
			Save(migrated);
			return migrated;
		}

		return document with { SchemaVersion = AppPreferencesDocument.CurrentSchemaVersion };
	}

	internal static AppPreferencesDocument FromDto(AppPreferencesDocumentDto dto)
	{
		var statusValue = dto.FirstLaunchAiWizardStatus;
		var status = Enum.IsDefined(typeof(FirstLaunchAiWizardStatus), statusValue)
			? (FirstLaunchAiWizardStatus)statusValue
			: FirstLaunchAiWizardStatus.NotStarted;

		return new AppPreferencesDocument(
			dto.SchemaVersion,
			status,
			dto.FirstLaunchAiWizardCompletedAtUtc,
			dto.HideAiPromotionsInUi ?? false);
	}

	private bool ShouldAutoCompleteFromExistingAiSetup()
	{
		var settings = new AiSettingsRepository(_aiSettingsFilePath).LoadOrDefault();
		if (settings.ActiveBackend != AiBackendKind.None)
		{
			return true;
		}

		if (!File.Exists(_aiDownloadJobFilePath))
		{
			return false;
		}

		try
		{
			var json = File.ReadAllText(_aiDownloadJobFilePath);
			using var document = JsonDocument.Parse(json);
			if (!document.RootElement.TryGetProperty("state", out var stateElement))
			{
				return false;
			}

			var state = stateElement.GetString();
			return state is "Downloading" or "Paused" or "Interrupted" or "Failed";
		}
		catch
		{
			return false;
		}
	}
}
