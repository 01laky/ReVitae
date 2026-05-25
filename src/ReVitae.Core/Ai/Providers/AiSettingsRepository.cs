using System.Text.Json;
using ReVitae.Core.Ai;

namespace ReVitae.Core.Ai.Providers;

internal sealed record AiSettingsDocumentDto(
	int SchemaVersion,
	AiBackendKind ActiveBackend,
	string? ActiveLocalModelId,
	string? ActiveOnlineProviderId,
	LocalAiSettingsRecord? Local,
	Dictionary<string, AiProviderConnectionConfig>? OnlineProviders);

internal sealed record AiSettingsLegacyDto(
	string SelectedModelId,
	string OllamaModelTag,
	DateTimeOffset DownloadedAtUtc);

public sealed class AiSettingsRepository
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = true,
	};

	private readonly string _filePath;

	public AiSettingsRepository()
		: this(ReVitaeLocalDataPaths.GetAiSettingsFilePath())
	{
	}

	public AiSettingsRepository(string filePath)
	{
		_filePath = filePath;
	}

	public AiSettingsDocument LoadOrDefault()
	{
		if (!File.Exists(_filePath))
		{
			return AiSettingsDocument.Empty;
		}

		try
		{
			var json = File.ReadAllText(_filePath);
			using var document = JsonDocument.Parse(json);
			if (document.RootElement.TryGetProperty("schemaVersion", out _))
			{
				var dto = JsonSerializer.Deserialize<AiSettingsDocumentDto>(json, JsonOptions);
				return dto is null ? AiSettingsDocument.Empty : FromDto(dto);
			}

			var legacy = JsonSerializer.Deserialize<AiSettingsLegacyDto>(json, JsonOptions);
			return legacy is null ? AiSettingsDocument.Empty : MigrateLegacy(legacy);
		}
		catch
		{
			return AiSettingsDocument.Empty;
		}
	}

	public void Save(AiSettingsDocument settings)
	{
		var directory = Path.GetDirectoryName(_filePath);
		if (!string.IsNullOrWhiteSpace(directory))
		{
			Directory.CreateDirectory(directory);
		}

		var dto = new AiSettingsDocumentDto(
			AiSettingsDocument.CurrentSchemaVersion,
			settings.ActiveBackend,
			settings.ActiveLocalModelId,
			settings.ActiveOnlineProviderId,
			settings.Local,
			settings.OnlineProviders.ToDictionary(
				pair => pair.Key,
				pair => pair.Value,
				StringComparer.Ordinal));

		var json = JsonSerializer.Serialize(dto, JsonOptions);
		var tempPath = _filePath + ".tmp";
		File.WriteAllText(tempPath, json);
		File.Move(tempPath, _filePath, overwrite: true);
	}

	public void Clear()
	{
		if (File.Exists(_filePath))
		{
			File.Delete(_filePath);
		}
	}

	public static AiSettingsDocument MigrateLegacy(AiSettingsSnapshot legacy) =>
		MigrateLegacy(new AiSettingsLegacyDto(
			legacy.SelectedModelId,
			legacy.OllamaModelTag,
			legacy.DownloadedAtUtc));

	private static AiSettingsDocument MigrateLegacy(AiSettingsLegacyDto legacy) =>
		new(
			AiSettingsDocument.CurrentSchemaVersion,
			AiBackendKind.Local,
			legacy.SelectedModelId,
			null,
			new LocalAiSettingsRecord(
				legacy.SelectedModelId,
				legacy.OllamaModelTag,
				legacy.DownloadedAtUtc),
			new Dictionary<string, AiProviderConnectionConfig>(StringComparer.Ordinal));

	private static AiSettingsDocument FromDto(AiSettingsDocumentDto dto) =>
		new(
			dto.SchemaVersion,
			dto.ActiveBackend,
			dto.ActiveLocalModelId,
			dto.ActiveOnlineProviderId,
			dto.Local,
			dto.OnlineProviders ?? new Dictionary<string, AiProviderConnectionConfig>(StringComparer.Ordinal));
}
