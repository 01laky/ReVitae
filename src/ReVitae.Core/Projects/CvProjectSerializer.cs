using System.Text.Json;
using ReVitae.Core.Export;
using ReVitae.Core.Import;
using ReVitae.Core.Import.Structured;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Projects;

public sealed record CvProjectSaveRequest(
	CvExportSourceData Source,
	CvProjectSettings Settings);

public sealed record CvProjectLoadResult(
	bool Success,
	CvImportResult? Import,
	CvProjectSettings? Settings,
	string? ErrorMessageKey,
	bool SettingsPartiallyIgnored = false,
	bool SettingsMalformed = false);

public static class CvProjectSerializer
{
	private const string ProjectSettingsProperty = "projectSettings";

	public static void Save(string filePath, CvProjectSaveRequest request)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
		ArgumentNullException.ThrowIfNull(request);

		var root = RevitaeJsonDtoBuilder.Build(request.Source);
		root[ProjectSettingsProperty] = BuildProjectSettingsDto(request.Settings with
		{
			SavedAtUtc = DateTimeOffset.UtcNow,
			ApplicationVersion = CvProjectApplicationInfo.Version
		});

		AtomicJsonFileWriter.WriteObject(filePath, root);
	}

	public static CvProjectLoadResult Load(string filePath)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

		if (!File.Exists(filePath))
		{
			return new CvProjectLoadResult(false, null, null, TranslationKeys.ProjectOpenFailed);
		}

		var fileInfo = new FileInfo(filePath);
		if (fileInfo.Length > CvImportLimits.MaxFileBytes)
		{
			return new CvProjectLoadResult(false, null, null, TranslationKeys.ProjectOpenFailed);
		}

		string json;
		try
		{
			json = File.ReadAllText(filePath);
		}
		catch
		{
			return new CvProjectLoadResult(false, null, null, TranslationKeys.ProjectOpenFailed);
		}

		var import = ReVitaeJsonMapper.Map(json);
		if (!import.Success)
		{
			return new CvProjectLoadResult(false, null, null, import.ErrorMessageKey ?? TranslationKeys.ProjectOpenFailed);
		}

		var settingsResult = TryParseProjectSettings(json);
		return new CvProjectLoadResult(
			true,
			import,
			settingsResult.Settings,
			null,
			settingsResult.PartiallyIgnored,
			settingsResult.Malformed);
	}

	private static object BuildProjectSettingsDto(CvProjectSettings settings) => new
	{
		schemaVersion = settings.SchemaVersion,
		selectedTemplateId = settings.SelectedTemplateId is { } templateId
			? CvExportTemplateIdJson.ToJsonId(templateId)
			: null,
		dismissedQualityHintKeys = settings.DismissedQualityHintKeys.ToArray(),
		sectionExpandState = settings.SectionExpandState,
		savedAtUtc = settings.SavedAtUtc?.UtcDateTime,
		applicationVersion = settings.ApplicationVersion
	};

	private static (CvProjectSettings? Settings, bool PartiallyIgnored, bool Malformed) TryParseProjectSettings(string json)
	{
		try
		{
			using var document = JsonDocument.Parse(json);
			if (!document.RootElement.TryGetProperty(ProjectSettingsProperty, out var settingsElement))
			{
				return (null, false, false);
			}

			return ParseProjectSettingsElement(settingsElement);
		}
		catch
		{
			return (null, false, true);
		}
	}

	private static (CvProjectSettings? Settings, bool PartiallyIgnored, bool Malformed) ParseProjectSettingsElement(
		JsonElement settingsElement)
	{
		if (settingsElement.ValueKind != JsonValueKind.Object)
		{
			return (null, false, true);
		}

		if (!settingsElement.TryGetProperty("schemaVersion", out var schemaElement)
			|| schemaElement.ValueKind != JsonValueKind.Number
			|| !schemaElement.TryGetInt32(out var schemaVersion))
		{
			return (null, false, true);
		}

		var partiallyIgnored = schemaVersion > CvProjectConstants.CurrentProjectSettingsSchemaVersion;

		CvExportTemplateId? templateId = null;
		if (settingsElement.TryGetProperty("selectedTemplateId", out var templateElement)
			&& templateElement.ValueKind == JsonValueKind.String)
		{
			templateId = CvExportTemplateIdJson.ParseOrDefault(templateElement.GetString(), out _);
		}

		var dismissedKeys = new List<string>();
		if (settingsElement.TryGetProperty("dismissedQualityHintKeys", out var dismissedElement)
			&& dismissedElement.ValueKind == JsonValueKind.Array)
		{
			foreach (var item in dismissedElement.EnumerateArray())
			{
				if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
				{
					dismissedKeys.Add(item.GetString()!);
				}
			}
		}

		Dictionary<string, bool>? expandState = null;
		if (settingsElement.TryGetProperty("sectionExpandState", out var expandElement)
			&& expandElement.ValueKind == JsonValueKind.Object)
		{
			expandState = new Dictionary<string, bool>(StringComparer.Ordinal);
			foreach (var property in expandElement.EnumerateObject())
			{
				if (property.Value.ValueKind is JsonValueKind.True or JsonValueKind.False)
				{
					expandState[property.Name] = property.Value.GetBoolean();
				}
			}
		}

		DateTimeOffset? savedAtUtc = null;
		if (settingsElement.TryGetProperty("savedAtUtc", out var savedAtElement)
			&& savedAtElement.ValueKind == JsonValueKind.String
			&& DateTimeOffset.TryParse(savedAtElement.GetString(), out var parsedSavedAt))
		{
			savedAtUtc = parsedSavedAt.ToUniversalTime();
		}

		string? applicationVersion = null;
		if (settingsElement.TryGetProperty("applicationVersion", out var versionElement)
			&& versionElement.ValueKind == JsonValueKind.String)
		{
			applicationVersion = versionElement.GetString();
		}

		return (
			new CvProjectSettings(
				schemaVersion,
				templateId,
				dismissedKeys,
				expandState,
				savedAtUtc,
				applicationVersion),
			partiallyIgnored,
			false);
	}
}
