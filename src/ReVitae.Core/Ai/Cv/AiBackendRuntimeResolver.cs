using ReVitae.Core.Ai;
using ReVitae.Core.Ai.Providers;

namespace ReVitae.Core.Ai.Cv;

public interface IAiBackendRuntimeResolver
{
    AiBackendResolveResult Resolve(AiSettingsDocument settings, IAiSecretStorage secretStorage);
}

public sealed class AiBackendRuntimeResolver : IAiBackendRuntimeResolver
{
    public AiBackendResolveResult Resolve(AiSettingsDocument settings, IAiSecretStorage secretStorage)
    {
        return settings.ActiveBackend switch
        {
            AiBackendKind.Local => ResolveLocal(settings),
            AiBackendKind.Online => ResolveOnline(settings, secretStorage),
            _ => AiBackendResolveResult.Unavailable(
                AiBackendUnavailableReason.NoBackendConfigured,
                Localization.TranslationKeys.AiCvNoBackendConfigured),
        };
    }

    private static AiBackendResolveResult ResolveLocal(AiSettingsDocument settings)
    {
        var modelId = settings.ActiveLocalModelId;
        if (string.IsNullOrWhiteSpace(modelId))
        {
            return AiBackendResolveResult.Unavailable(
                AiBackendUnavailableReason.NoBackendConfigured,
                Localization.TranslationKeys.AiCvNoBackendConfigured);
        }

        var modelTag = ResolveLocalModelTag(settings, modelId);
        if (string.IsNullOrWhiteSpace(modelTag))
        {
            return AiBackendResolveResult.Unavailable(
                AiBackendUnavailableReason.LocalModelTagMissing,
                Localization.TranslationKeys.AiCvNoBackendConfigured);
        }

        var model = AiModelCatalog.TryGetById(modelId);
        var displayNameKey = model?.DisplayNameKey ?? Localization.TranslationKeys.AiSetupActiveAiNone;
        return AiBackendResolveResult.Success(new OllamaChatBackendRuntime(modelTag, displayNameKey));
    }

    private static AiBackendResolveResult ResolveOnline(
        AiSettingsDocument settings,
        IAiSecretStorage secretStorage)
    {
        var providerId = settings.ActiveOnlineProviderId;
        if (string.IsNullOrWhiteSpace(providerId))
        {
            return AiBackendResolveResult.Unavailable(
                AiBackendUnavailableReason.NoBackendConfigured,
                Localization.TranslationKeys.AiCvNoBackendConfigured);
        }

        var provider = AiOnlineProviderCatalog.TryGetById(providerId);
        if (provider is null)
        {
            return AiBackendResolveResult.Unavailable(
                AiBackendUnavailableReason.OnlineProviderMisconfigured,
                Localization.TranslationKeys.AiCvNoBackendConfigured);
        }

        settings.OnlineProviders.TryGetValue(providerId, out var config);
        if (!AiProviderConfigValidator.IsConfigured(provider, config, secretStorage))
        {
            return AiBackendResolveResult.Unavailable(
                AiBackendUnavailableReason.OnlineProviderMisconfigured,
                Localization.TranslationKeys.AiCvNoBackendConfigured);
        }

        var draft = AiProviderConfigValidator.ToDraft(
            provider,
            config,
            secretStorage.TryGetApiKey(providerId));

        if (!AiProviderConfigValidator.IsValid(provider, draft))
        {
            return AiBackendResolveResult.Unavailable(
                AiBackendUnavailableReason.OnlineProviderMisconfigured,
                Localization.TranslationKeys.AiCvNoBackendConfigured);
        }

        return AiBackendResolveResult.Success(new OnlineChatBackendRuntime(provider, draft));
    }

    internal static string? ResolveLocalModelTag(AiSettingsDocument settings, string modelId)
    {
        if (settings.Local is not null &&
            string.Equals(settings.Local.SelectedModelId, modelId, StringComparison.Ordinal) &&
            !string.IsNullOrWhiteSpace(settings.Local.OllamaModelTag))
        {
            return settings.Local.OllamaModelTag;
        }

        return AiModelCatalog.TryGetById(modelId)?.OllamaModelTag;
    }
}

public sealed record AiBackendResolveResult(
    IAiBackendRuntime? Runtime,
    AiBackendUnavailableReason UnavailableReason,
    string? ErrorMessageKey)
{
    public bool IsAvailable => Runtime is not null;

    public static AiBackendResolveResult Success(IAiBackendRuntime runtime) =>
        new(runtime, AiBackendUnavailableReason.None, null);

    public static AiBackendResolveResult Unavailable(AiBackendUnavailableReason reason, string errorKey) =>
        new(null, reason, errorKey);
}
