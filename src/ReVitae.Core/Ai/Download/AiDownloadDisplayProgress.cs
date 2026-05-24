using ReVitae.Core.Localization;

namespace ReVitae.Core.Ai.Download;

public sealed record AiDownloadDisplayState(int? Percent, bool IsIndeterminate);

/// <summary>
/// Stabilizes Ollama's per-layer byte progress for UI display.
/// Ollama resets completed/total on each layer; this tracker keeps percent monotonic per phase.
/// </summary>
public sealed class AiDownloadDisplayProgress
{
    private Guid _jobId = Guid.Empty;
    private string? _phaseKey;
    private int _highWaterMarkPercent;
    private long _modelLayerBaselineBytes;

    public AiDownloadDisplayState Update(AiDownloadJobSnapshot snapshot)
    {
        if (snapshot.JobId != _jobId)
        {
            Reset(snapshot.JobId);
        }

        var phaseKey = ResolvePhaseKey(snapshot);
        if (!string.Equals(phaseKey, _phaseKey, StringComparison.Ordinal))
        {
            _phaseKey = phaseKey;
            _highWaterMarkPercent = 0;
            _modelLayerBaselineBytes = 0;
        }

        if (snapshot.State == AiDownloadJobState.Completed)
        {
            _highWaterMarkPercent = 100;
            return new AiDownloadDisplayState(100, false);
        }

        if (phaseKey == "model")
        {
            var modelPercent = TryGetModelPhasePercent(snapshot);
            if (modelPercent is int modelValue)
            {
                _highWaterMarkPercent = Math.Max(_highWaterMarkPercent, modelValue);
                return new AiDownloadDisplayState(_highWaterMarkPercent, false);
            }
        }

        var rawPercent = AiDownloadProgress.TryGetPercent(snapshot.CompletedBytes, snapshot.TotalBytes);
        if (rawPercent is int value && ShouldUseByteProgress(snapshot, phaseKey, value))
        {
            _highWaterMarkPercent = Math.Max(_highWaterMarkPercent, value);
            return new AiDownloadDisplayState(_highWaterMarkPercent, false);
        }

        if (_highWaterMarkPercent > 0)
        {
            return new AiDownloadDisplayState(_highWaterMarkPercent, false);
        }

        return new AiDownloadDisplayState(null, true);
    }

    public void Reset(Guid jobId = default)
    {
        _jobId = jobId;
        _phaseKey = null;
        _highWaterMarkPercent = 0;
        _modelLayerBaselineBytes = 0;
    }

    internal static string ResolvePhaseKey(AiDownloadJobSnapshot snapshot)
    {
        if (AiDownloadStatus.TryGetTranslationKey(snapshot.StatusText, out var statusKey) &&
            IsEnginePhaseKey(statusKey))
        {
            return "engine";
        }

        if (!string.IsNullOrWhiteSpace(snapshot.StatusText))
        {
            return "model";
        }

        return "engine";
    }

    private int? TryGetModelPhasePercent(AiDownloadJobSnapshot snapshot)
    {
        if (snapshot.CompletedBytes is not > 0)
        {
            return null;
        }

        var model = AiModelCatalog.TryGetById(snapshot.SelectedModelId);
        if (model is null)
        {
            return AiDownloadProgress.TryGetPercent(snapshot.CompletedBytes, snapshot.TotalBytes);
        }

        if (snapshot.CompletedBytes < _modelLayerBaselineBytes)
        {
            _modelLayerBaselineBytes = snapshot.CompletedBytes.Value;
        }

        var effectiveCompleted = snapshot.CompletedBytes.Value;
        var approxTotal = model.ApproxDownloadBytes;
        if (approxTotal <= 0)
        {
            return null;
        }

        var layerPercent = AiDownloadProgress.TryGetPercent(snapshot.CompletedBytes, snapshot.TotalBytes);
        if (layerPercent is 100 &&
            snapshot.State != AiDownloadJobState.Completed &&
            snapshot.CompletedBytes == snapshot.TotalBytes)
        {
            return null;
        }

        var overallPercent = (int)Math.Clamp(effectiveCompleted * 100 / approxTotal, 0, 100);
        if (layerPercent is int layerValue)
        {
            return Math.Max(overallPercent, layerValue);
        }

        return overallPercent;
    }

    private static bool ShouldUseByteProgress(AiDownloadJobSnapshot snapshot, string phaseKey, int rawPercent)
    {
        if (phaseKey == "engine")
        {
            return true;
        }

        if (AiDownloadStatus.TryGetTranslationKey(snapshot.StatusText, out var statusKey) &&
            statusKey is TranslationKeys.AiDownloadPreparingEngine
                or TranslationKeys.AiDownloadDownloadingEngine
                or TranslationKeys.AiDownloadStartingEngine)
        {
            return false;
        }

        if (rawPercent >= 100 &&
            AiDownloadStatus.TryGetTranslationKey(snapshot.StatusText, out var pullingKey) &&
            pullingKey == TranslationKeys.AiDownloadPullingModel &&
            snapshot.CompletedBytes == snapshot.TotalBytes)
        {
            return snapshot.State == AiDownloadJobState.Completed;
        }

        return true;
    }

    private static bool IsEnginePhaseKey(string statusKey) =>
        statusKey is TranslationKeys.AiDownloadPreparingEngine
            or TranslationKeys.AiDownloadDownloadingEngine
            or TranslationKeys.AiDownloadStartingEngine;
}
