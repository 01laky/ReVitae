using ReVitae.Core.Ai.Download;
using ReVitae.Core.Ai.Ollama;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Ai;

public enum AiModelInstallPresence
{
    NotInstalled,
    Installed,
    ActiveDownload,
    StaleDownload,
}

public sealed record AiModelInstallationStatus(
    AiModelCatalogEntry Model,
    AiModelInstallPresence Presence,
    IReadOnlyList<string> InstalledOllamaTags,
    bool CanUninstall,
    bool CanCleanStaleDownload);

public sealed record AiModelLifecycleResult(bool Succeeded, string? ErrorMessageKey);

public sealed class AiModelLifecycleService
{
    private readonly IOllamaModelDeleteClient _deleteClient;
    private readonly AiDownloadJobStorage _jobStorage;
    private readonly AiSettingsStorage _settingsStorage;

    public AiModelLifecycleService()
        : this(new OllamaModelDeleteClient(), new AiDownloadJobStorage(), new AiSettingsStorage())
    {
    }

    public AiModelLifecycleService(
        IOllamaModelDeleteClient deleteClient,
        AiDownloadJobStorage jobStorage,
        AiSettingsStorage settingsStorage)
    {
        _deleteClient = deleteClient;
        _jobStorage = jobStorage;
        _settingsStorage = settingsStorage;
    }

    public IReadOnlyList<AiModelInstallationStatus> AnalyzeCatalog(
        OllamaRuntimeStatus ollama,
        AiDownloadJobSnapshot currentDownloadSnapshot,
        bool hasActiveDownload)
    {
        var persistedJob = _jobStorage.TryLoad();
        return AiModelCatalog.Default
            .Select(model => AnalyzeModel(model, ollama, currentDownloadSnapshot, persistedJob, hasActiveDownload))
            .ToArray();
    }

    public static AiModelInstallationStatus AnalyzeModel(
        AiModelCatalogEntry model,
        OllamaRuntimeStatus ollama,
        AiDownloadJobSnapshot currentDownloadSnapshot,
        AiDownloadJobSnapshot? persistedJob,
        bool hasActiveDownload)
    {
        var installedTags = AiModelInstallationMatcher.GetMatchingTags(
            ollama.InstalledModelTags,
            model.OllamaModelTag);
        var isInstalled = installedTags.Count > 0;
        var relatedJob = ResolveRelatedJob(model.Id, currentDownloadSnapshot, persistedJob);
        var isActiveDownload = hasActiveDownload &&
                               string.Equals(currentDownloadSnapshot.SelectedModelId, model.Id, StringComparison.Ordinal);
        var hasStaleDownload = HasStaleDownloadArtifact(model, relatedJob, isInstalled, isActiveDownload);

        var presence = isActiveDownload
            ? AiModelInstallPresence.ActiveDownload
            : isInstalled
                ? AiModelInstallPresence.Installed
                : hasStaleDownload
                    ? AiModelInstallPresence.StaleDownload
                    : AiModelInstallPresence.NotInstalled;

        var canUninstall = isInstalled && !isActiveDownload;
        var canCleanStaleDownload = hasStaleDownload && !isActiveDownload;

        return new AiModelInstallationStatus(
            model,
            presence,
            installedTags,
            canUninstall,
            canCleanStaleDownload);
    }

    public async Task<AiModelLifecycleResult> TryUninstallModelAsync(
        AiModelCatalogEntry model,
        OllamaRuntimeStatus ollama,
        bool hasActiveDownload,
        CancellationToken cancellationToken = default)
    {
        if (hasActiveDownload)
        {
            return new AiModelLifecycleResult(false, TranslationKeys.AiSetupModelRemoveBlockedActiveDownload);
        }

        var tags = AiModelInstallationMatcher.GetMatchingTags(ollama.InstalledModelTags, model.OllamaModelTag);
        if (tags.Count == 0)
        {
            ClearLocalArtifacts(model);
            return new AiModelLifecycleResult(true, null);
        }

        if (!ollama.IsReachable)
        {
            return new AiModelLifecycleResult(false, TranslationKeys.AiSetupOllamaNotRunning);
        }

        foreach (var tag in tags)
        {
            if (!await _deleteClient.TryDeleteModelAsync(tag, cancellationToken).ConfigureAwait(false))
            {
                return new AiModelLifecycleResult(false, TranslationKeys.AiSetupModelRemoveFailed);
            }
        }

        ClearLocalArtifacts(model);
        return new AiModelLifecycleResult(true, null);
    }

    public async Task<AiModelLifecycleResult> TryCleanStaleDownloadAsync(
        AiModelCatalogEntry model,
        OllamaRuntimeStatus ollama,
        bool hasActiveDownload,
        CancellationToken cancellationToken = default)
    {
        if (hasActiveDownload)
        {
            return new AiModelLifecycleResult(false, TranslationKeys.AiSetupModelRemoveBlockedActiveDownload);
        }

        var tags = AiModelInstallationMatcher.GetMatchingTags(ollama.InstalledModelTags, model.OllamaModelTag);
        if (tags.Count > 0)
        {
            if (!ollama.IsReachable)
            {
                return new AiModelLifecycleResult(false, TranslationKeys.AiSetupOllamaNotRunning);
            }

            foreach (var tag in tags)
            {
                await _deleteClient.TryDeleteModelAsync(tag, cancellationToken).ConfigureAwait(false);
            }
        }

        ClearLocalArtifacts(model);
        return new AiModelLifecycleResult(true, null);
    }

    private void ClearLocalArtifacts(AiModelCatalogEntry model)
    {
        var job = _jobStorage.TryLoad();
        if (job is not null &&
            string.Equals(job.SelectedModelId, model.Id, StringComparison.Ordinal))
        {
            _jobStorage.Delete();
        }

        var settings = _settingsStorage.TryLoad();
        var document = _settingsStorage.LoadDocument();
        if (document.Local is not null &&
            (string.Equals(document.Local.SelectedModelId, model.Id, StringComparison.Ordinal) ||
             string.Equals(document.Local.OllamaModelTag, model.OllamaModelTag, StringComparison.OrdinalIgnoreCase)))
        {
            _settingsStorage.ClearLocalIfMatches(model.Id);
        }
        else if (settings is not null &&
                 (string.Equals(settings.SelectedModelId, model.Id, StringComparison.Ordinal) ||
                  string.Equals(settings.OllamaModelTag, model.OllamaModelTag, StringComparison.OrdinalIgnoreCase)))
        {
            _settingsStorage.ClearLocalIfMatches(model.Id);
        }
    }

    private static AiDownloadJobSnapshot? ResolveRelatedJob(
        string modelId,
        AiDownloadJobSnapshot currentDownloadSnapshot,
        AiDownloadJobSnapshot? persistedJob)
    {
        if (currentDownloadSnapshot.State != AiDownloadJobState.Idle &&
            string.Equals(currentDownloadSnapshot.SelectedModelId, modelId, StringComparison.Ordinal))
        {
            return currentDownloadSnapshot;
        }

        if (persistedJob is not null &&
            string.Equals(persistedJob.SelectedModelId, modelId, StringComparison.Ordinal))
        {
            return persistedJob;
        }

        return null;
    }

    private static bool HasStaleDownloadArtifact(
        AiModelCatalogEntry model,
        AiDownloadJobSnapshot? relatedJob,
        bool isInstalled,
        bool isActiveDownload)
    {
        if (isActiveDownload || relatedJob is null)
        {
            return false;
        }

        if (relatedJob.State is AiDownloadJobState.Failed
            or AiDownloadJobState.Paused
            or AiDownloadJobState.Interrupted
            or AiDownloadJobState.Stopped)
        {
            return true;
        }

        if (relatedJob.State == AiDownloadJobState.Downloading)
        {
            return true;
        }

        return !isInstalled &&
               relatedJob.CompletedBytes is > 0 &&
               HasPriorModelPullProgress(relatedJob);
    }

    private static bool HasPriorModelPullProgress(AiDownloadJobSnapshot snapshot)
    {
        if (snapshot.CompletedBytes is not > 0 || snapshot.TotalBytes is not > 0)
        {
            return false;
        }

        if (AiDownloadStatus.TryGetTranslationKey(snapshot.StatusText, out var statusKey))
        {
            return statusKey is TranslationKeys.AiDownloadPullingModel
                or TranslationKeys.AiDownloadRestartingAfterRecovery;
        }

        return true;
    }
}
