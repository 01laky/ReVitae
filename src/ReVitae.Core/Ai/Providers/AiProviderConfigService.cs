using ReVitae.Core.Ai;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Ai.Providers;

public sealed class AiProviderConfigService
{
    private readonly AiSettingsRepository _settingsRepository;
    private readonly IAiSecretStorage _secretStorage;
    private readonly AiProviderConnectionTester _connectionTester;

    public AiProviderConfigService()
        : this(new AiSettingsRepository(), new FileAiSecretStorage(), new AiProviderConnectionTester())
    {
    }

    public AiProviderConfigService(
        AiSettingsRepository settingsRepository,
        IAiSecretStorage secretStorage,
        AiProviderConnectionTester connectionTester)
    {
        _settingsRepository = settingsRepository;
        _secretStorage = secretStorage;
        _connectionTester = connectionTester;
    }

    public AiSettingsDocument CurrentSettings { get; private set; } = AiSettingsDocument.Empty;

    public IAiSecretStorage SecretStorage => _secretStorage;

    public event Action? SettingsChanged;

    public void Load()
    {
        CurrentSettings = _settingsRepository.LoadOrDefault();
        SettingsChanged?.Invoke();
    }

    public AiProviderConnectionDraft GetDraft(string providerId)
    {
        var provider = RequireProvider(providerId);
        CurrentSettings.OnlineProviders.TryGetValue(providerId, out var config);
        return AiProviderConfigValidator.ToDraft(provider, config, _secretStorage.TryGetApiKey(providerId));
    }

    public void SaveProviderConfig(
        string providerId,
        AiProviderConnectionDraft draft,
        bool? lastTestSucceeded = null,
        DateTimeOffset? lastTestedAtUtc = null)
    {
        var provider = RequireProvider(providerId);
        if (!AiProviderConfigValidator.IsValid(provider, draft))
        {
            throw new InvalidOperationException("Provider configuration is invalid.");
        }

        CurrentSettings.OnlineProviders.TryGetValue(providerId, out var existing);
        var config = AiProviderConfigValidator.ToConfig(provider, draft, existing) with
        {
            LastTestSucceeded = lastTestSucceeded ?? existing?.LastTestSucceeded,
            LastTestedAtUtc = lastTestedAtUtc ?? existing?.LastTestedAtUtc,
        };

        if (!string.IsNullOrWhiteSpace(draft.ApiKey))
        {
            _secretStorage.SaveApiKey(providerId, draft.ApiKey);
        }

        var online = CurrentSettings.OnlineProviders.ToDictionary(
            pair => pair.Key,
            pair => pair.Value,
            StringComparer.Ordinal);
        online[providerId] = config;
        UpdateSettings(CurrentSettings with { OnlineProviders = online });
    }

    public async Task<AiProviderTestResult> TestDraftAsync(
        AiProviderConnectionDraft draft,
        CancellationToken cancellationToken = default)
    {
        var provider = RequireProvider(draft.ProviderId);
        return await _connectionTester.TestAsync(provider, draft, cancellationToken).ConfigureAwait(false);
    }

    public async Task<AiProviderTestResult> TestAndPersistAsync(
        AiProviderConnectionDraft draft,
        CancellationToken cancellationToken = default)
    {
        var result = await TestDraftAsync(draft, cancellationToken).ConfigureAwait(false);
        SaveProviderConfig(
            draft.ProviderId,
            draft,
            result.Succeeded,
            DateTimeOffset.UtcNow);
        return result;
    }

    public void ResetProviderConfig(string providerId)
    {
        var settings = CurrentSettings;
        if (settings.ActiveBackend == AiBackendKind.Online &&
            string.Equals(settings.ActiveOnlineProviderId, providerId, StringComparison.Ordinal))
        {
            settings = settings with
            {
                ActiveBackend = AiBackendKind.None,
                ActiveOnlineProviderId = null,
            };
        }

        var online = settings.OnlineProviders
            .Where(pair => !string.Equals(pair.Key, providerId, StringComparison.Ordinal))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        settings = settings with { OnlineProviders = online };
        _secretStorage.DeleteApiKey(providerId);
        UpdateSettings(settings);
    }

    public void SaveLocalDownloadCompletion(string modelId, string ollamaModelTag, DateTimeOffset downloadedAtUtc)
    {
        var settings = CurrentSettings with
        {
            Local = new LocalAiSettingsRecord(modelId, ollamaModelTag, downloadedAtUtc),
        };

        if (settings.ActiveBackend == AiBackendKind.None)
        {
            settings = settings with
            {
                ActiveBackend = AiBackendKind.Local,
                ActiveLocalModelId = modelId,
            };
        }
        else if (settings.ActiveBackend == AiBackendKind.Local)
        {
            settings = settings with { ActiveLocalModelId = modelId };
        }

        UpdateSettings(settings);
    }

    public void ClearLocalSettingsIfMatches(string modelId)
    {
        if (!string.Equals(CurrentSettings.Local?.SelectedModelId, modelId, StringComparison.Ordinal))
        {
            return;
        }

        var deactivateLocal = CurrentSettings.ActiveBackend == AiBackendKind.Local &&
                              string.Equals(CurrentSettings.ActiveLocalModelId, modelId, StringComparison.Ordinal);
        UpdateSettings(CurrentSettings with
        {
            Local = null,
            ActiveBackend = deactivateLocal ? AiBackendKind.None : CurrentSettings.ActiveBackend,
            ActiveLocalModelId = deactivateLocal ? null : CurrentSettings.ActiveLocalModelId,
        });
    }

    public void UpdateActiveBackend(AiBackendKind kind, string? localModelId, string? onlineProviderId)
    {
        UpdateSettings(CurrentSettings with
        {
            ActiveBackend = kind,
            ActiveLocalModelId = localModelId,
            ActiveOnlineProviderId = onlineProviderId,
        });
    }

    public bool IsProviderConfigured(string providerId)
    {
        var provider = AiOnlineProviderCatalog.TryGetById(providerId);
        if (provider is null)
        {
            return false;
        }

        CurrentSettings.OnlineProviders.TryGetValue(providerId, out var config);
        return AiProviderConfigValidator.IsConfigured(provider, config, _secretStorage);
    }

    private void UpdateSettings(AiSettingsDocument settings)
    {
        CurrentSettings = settings;
        _settingsRepository.Save(settings);
        SettingsChanged?.Invoke();
    }

    private static AiOnlineProviderDefinition RequireProvider(string providerId) =>
        AiOnlineProviderCatalog.TryGetById(providerId)
        ?? throw new InvalidOperationException($"Unknown provider '{providerId}'.");
}

public sealed class AiActiveBackendService
{
    private readonly AiProviderConfigService _configService;

    public AiActiveBackendService(AiProviderConfigService configService)
    {
        _configService = configService;
    }

    public event Action? ActiveBackendChanged;

    public AiSettingsDocument Current => _configService.CurrentSettings;

    public ActiveAiBackendSnapshot GetActiveSnapshot() =>
        AiActiveBackendPresentation.GetSnapshot(Current);

    public bool CanActivateOnline(string providerId) => _configService.IsProviderConfigured(providerId);

    public bool NeedsUntestedActivationWarning(string providerId)
    {
        if (!Current.OnlineProviders.TryGetValue(providerId, out var config))
        {
            return true;
        }

        return config.LastTestSucceeded != true;
    }

    public bool TryActivateLocal(string modelId)
    {
        if (AiModelCatalog.TryGetById(modelId) is null)
        {
            return false;
        }

        _configService.UpdateActiveBackend(AiBackendKind.Local, modelId, null);
        ActiveBackendChanged?.Invoke();
        return true;
    }

    public bool TryActivateOnline(string providerId)
    {
        if (!CanActivateOnline(providerId))
        {
            return false;
        }

        _configService.UpdateActiveBackend(AiBackendKind.Online, null, providerId);
        ActiveBackendChanged?.Invoke();
        return true;
    }

    public void Deactivate()
    {
        _configService.UpdateActiveBackend(AiBackendKind.None, null, null);
        ActiveBackendChanged?.Invoke();
    }

    public bool RequiresSwitchConfirmation(AiBackendKind targetKind, string targetId)
    {
        var active = GetActiveSnapshot();
        if (active.Kind == AiBackendKind.None)
        {
            return false;
        }

        return targetKind switch
        {
            AiBackendKind.Local => active.Kind != AiBackendKind.Local ||
                                   !string.Equals(active.LocalModelId, targetId, StringComparison.Ordinal),
            AiBackendKind.Online => active.Kind != AiBackendKind.Online ||
                                    !string.Equals(active.OnlineProviderId, targetId, StringComparison.Ordinal),
            _ => false,
        };
    }
}

public static class AiActiveBackendPresentation
{
    public static string? GetOnlineModelLabel(AiSettingsDocument settings, string providerId)
    {
        if (!settings.OnlineProviders.TryGetValue(providerId, out var config))
        {
            return null;
        }

        return config.ModelId;
    }

    public static ActiveAiBackendSnapshot GetSnapshot(AiSettingsDocument settings)
    {
        return settings.ActiveBackend switch
        {
            AiBackendKind.Local when !string.IsNullOrWhiteSpace(settings.ActiveLocalModelId) =>
                BuildLocal(settings.ActiveLocalModelId!),
            AiBackendKind.Online when !string.IsNullOrWhiteSpace(settings.ActiveOnlineProviderId) =>
                BuildOnline(settings, settings.ActiveOnlineProviderId!),
            _ => new ActiveAiBackendSnapshot(AiBackendKind.None, null, null, null, null),
        };
    }

    private static ActiveAiBackendSnapshot BuildLocal(string modelId)
    {
        var model = AiModelCatalog.TryGetById(modelId);
        return new ActiveAiBackendSnapshot(
            AiBackendKind.Local,
            modelId,
            null,
            model?.DisplayNameKey,
            model?.OllamaModelTag);
    }

    private static ActiveAiBackendSnapshot BuildOnline(AiSettingsDocument settings, string providerId)
    {
        var provider = AiOnlineProviderCatalog.TryGetById(providerId);
        return new ActiveAiBackendSnapshot(
            AiBackendKind.Online,
            null,
            providerId,
            provider?.DisplayNameKey,
            GetOnlineModelLabel(settings, providerId));
    }
}
