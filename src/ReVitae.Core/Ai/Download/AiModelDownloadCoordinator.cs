using ReVitae.Core.Ai.Ollama;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Ai.Download;

public sealed class AiModelDownloadCoordinator
{
	private enum PullCancelIntent
	{
		None,
		Pause,
		Stop,
	}

	private static readonly AiDownloadJobSnapshot IdleSnapshot = new(
		Guid.Empty,
		string.Empty,
		string.Empty,
		string.Empty,
		AiDownloadJobState.Idle,
		false,
		null,
		null,
		null,
		DateTimeOffset.MinValue,
		DateTimeOffset.MinValue,
		null);

	private readonly IOllamaPullClient _pullClient;
	private readonly IOllamaModelDeleteClient _modelDeleteClient;
	private readonly AiDownloadJobStorage _jobStorage;
	private readonly AiSettingsStorage _settingsStorage;
	private readonly IDiskSpaceChecker _diskSpaceChecker;
	private readonly IOllamaRuntimeProbe _ollamaProbe;
	private readonly IOllamaInstaller _ollamaInstaller;
	private readonly IClock _clock;

	private readonly object _snapshotLock = new();
	private CancellationTokenSource? _pullCts;
	private PullCancelIntent _cancelIntent = PullCancelIntent.None;
	private Task? _activePullTask;
	private bool _recoveryAttempted;

	private sealed record PullExecutionContext(
		bool AllowInstall,
		bool SkipReachabilityIfProbeOk,
		OllamaReachabilityOptions ReachabilityOptions);

	private PullExecutionContext CreateStartContext() =>
		new(AllowInstall: true, SkipReachabilityIfProbeOk: false, StartReachabilityOptions);

	private PullExecutionContext CreateResumeContext(AiDownloadJobSnapshot snapshot) =>
		new(
			AllowInstall: !HasPriorModelPullProgress(snapshot) && !OllamaPaths.IsManagedInstallPresent(),
			SkipReachabilityIfProbeOk: true,
			ResumeReachabilityOptions);

	public static bool HasPriorModelPullProgress(AiDownloadJobSnapshot snapshot)
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

		return snapshot.State is AiDownloadJobState.Paused
			or AiDownloadJobState.Interrupted
			or AiDownloadJobState.Failed;
	}

	private PullExecutionContext CreateRecoveryContext() =>
		new(AllowInstall: true, SkipReachabilityIfProbeOk: false, ResumeReachabilityOptions);

	public AiModelDownloadCoordinator()
		: this(
			new OllamaPullClient(),
			new OllamaModelDeleteClient(),
			new AiDownloadJobStorage(),
			new AiSettingsStorage(),
			new DiskSpaceChecker(),
			new OllamaRuntimeProbe(),
			new OllamaInstaller(),
			new SystemClock())
	{
	}

	public AiModelDownloadCoordinator(
		IOllamaPullClient pullClient,
		IOllamaModelDeleteClient modelDeleteClient,
		AiDownloadJobStorage jobStorage,
		AiSettingsStorage settingsStorage,
		IDiskSpaceChecker diskSpaceChecker,
		IOllamaRuntimeProbe ollamaProbe,
		IOllamaInstaller ollamaInstaller,
		IClock clock)
	{
		_pullClient = pullClient;
		_modelDeleteClient = modelDeleteClient;
		_jobStorage = jobStorage;
		_settingsStorage = settingsStorage;
		_diskSpaceChecker = diskSpaceChecker;
		_ollamaProbe = ollamaProbe;
		_ollamaInstaller = ollamaInstaller;
		_clock = clock;
		CurrentSnapshot = IdleSnapshot;
	}

	public event Action<AiDownloadJobSnapshot>? SnapshotChanged;

	public AiDownloadJobSnapshot CurrentSnapshot { get; private set; }

	public TimeSpan CompletedDwellDuration { get; set; } = TimeSpan.FromSeconds(4);

	public OllamaReachabilityOptions StartReachabilityOptions { get; set; } = OllamaReachabilityOptions.Default;

	public OllamaReachabilityOptions ResumeReachabilityOptions { get; set; } = OllamaReachabilityOptions.ForResume;

	public OllamaReachabilityOptions ReachabilityOptions
	{
		get => StartReachabilityOptions;
		set
		{
			StartReachabilityOptions = value;
			ResumeReachabilityOptions = value;
		}
	}

	public bool HasActiveJob => AiDownloadUiStateMapper.HasActiveJob(CurrentSnapshot.State);

	public void ResetIfJobMatches(string selectedModelId)
	{
		if (!string.Equals(CurrentSnapshot.SelectedModelId, selectedModelId, StringComparison.Ordinal))
		{
			return;
		}

		if (CurrentSnapshot.State is AiDownloadJobState.Downloading or AiDownloadJobState.Interrupted)
		{
			return;
		}

		_jobStorage.Delete();
		SetIdle();
	}

	public bool TryStart(AiModelCatalogEntry model, bool requiresOversizedWarning)
	{
		if (HasActiveJob)
		{
			return false;
		}

		if (!_diskSpaceChecker.HasSpaceForDownload(model.ApproxDownloadBytes))
		{
			return false;
		}

		var now = _clock.UtcNow;
		var snapshot = new AiDownloadJobSnapshot(
			Guid.NewGuid(),
			model.Id,
			model.OllamaModelTag,
			model.DisplayNameKey,
			AiDownloadJobState.Downloading,
			requiresOversizedWarning,
			null,
			null,
			AiDownloadStatus.FromTranslationKey(TranslationKeys.AiDownloadPreparingEngine),
			now,
			now,
			null);

		Publish(snapshot, forceFlush: true);
		BeginPull(snapshot, CreateStartContext());
		return true;
	}

	public void Pause() => _ = PauseAsync();

	public async Task PauseAsync(CancellationToken cancellationToken = default)
	{
		Task? activeTask;
		lock (_snapshotLock)
		{
			if (CurrentSnapshot.State is not AiDownloadJobState.Downloading
				and not AiDownloadJobState.Interrupted)
			{
				return;
			}

			_cancelIntent = PullCancelIntent.Pause;
			_pullCts?.Cancel();
			activeTask = _activePullTask;
		}

		if (activeTask is not null)
		{
			try
			{
				await activeTask.WaitAsync(cancellationToken).ConfigureAwait(false);
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				throw;
			}
			catch
			{
			}
		}

		if (CurrentSnapshot.State is AiDownloadJobState.Downloading or AiDownloadJobState.Interrupted)
		{
			_cancelIntent = PullCancelIntent.Pause;
			ApplyPullCancelIntent();
		}
	}

	public async Task ResumeAsync(CancellationToken cancellationToken = default)
	{
		if (CurrentSnapshot.State is not AiDownloadJobState.Paused
			and not AiDownloadJobState.Interrupted
			and not AiDownloadJobState.Failed)
		{
			return;
		}

		var model = AiModelCatalog.TryGetById(CurrentSnapshot.SelectedModelId);
		if (model is null)
		{
			SetFailed(TranslationKeys.AiDownloadFailed);
			return;
		}

		if (!_diskSpaceChecker.HasSpaceForDownload(model.ApproxDownloadBytes))
		{
			SetFailed(TranslationKeys.AiDownloadInsufficientDiskSpace);
			return;
		}

		var resumed = CurrentSnapshot with
		{
			State = AiDownloadJobState.Downloading,
			ErrorMessageKey = null,
			StatusText = HasPriorModelPullProgress(CurrentSnapshot)
				? AiDownloadStatus.FromTranslationKey(TranslationKeys.AiDownloadPullingModel)
				: CurrentSnapshot.StatusText,
			LastUpdatedAtUtc = _clock.UtcNow,
		};

		Publish(resumed, forceFlush: true);
		BeginPull(resumed, CreateResumeContext(resumed));
	}

	public async Task StopAsync()
	{
		if (CurrentSnapshot.State == AiDownloadJobState.Idle)
		{
			return;
		}

		if (CurrentSnapshot.State is AiDownloadJobState.Downloading
			or AiDownloadJobState.Interrupted)
		{
			_cancelIntent = PullCancelIntent.Stop;
			_pullCts?.Cancel();
			if (_activePullTask is not null)
			{
				try
				{
					await _activePullTask.ConfigureAwait(false);
				}
				catch
				{
				}
			}

			if (HasActiveJob)
			{
				_jobStorage.Delete();
				SetIdle();
			}

			_cancelIntent = PullCancelIntent.None;
			return;
		}

		_activePullTask = null;
		_jobStorage.Delete();
		SetIdle();
		_cancelIntent = PullCancelIntent.None;
	}

	public async Task TryRecoverOnStartupAsync(CancellationToken cancellationToken = default)
	{
		if (_recoveryAttempted)
		{
			return;
		}

		_recoveryAttempted = true;

		var loaded = _jobStorage.TryLoad();
		if (loaded is null)
		{
			return;
		}

		if (loaded.State is AiDownloadJobState.Completed or AiDownloadJobState.Stopped)
		{
			_jobStorage.Delete();
			return;
		}

		Publish(loaded, forceFlush: true);

		if (loaded.State == AiDownloadJobState.Downloading)
		{
			var interrupted = loaded with
			{
				State = AiDownloadJobState.Interrupted,
				LastUpdatedAtUtc = _clock.UtcNow,
			};

			Publish(interrupted, forceFlush: true);
			await ResumeAsync(cancellationToken).ConfigureAwait(false);
			return;
		}

		if (loaded.State is AiDownloadJobState.Paused or AiDownloadJobState.Failed)
		{
			RaiseSnapshotChanged();
		}
	}

	private void BeginPull(AiDownloadJobSnapshot snapshot, PullExecutionContext context)
	{
		_cancelIntent = PullCancelIntent.None;
		_pullCts?.Cancel();
		_pullCts?.Dispose();
		_pullCts = new CancellationTokenSource();

		var token = _pullCts.Token;
		var previousTask = _activePullTask;
		_activePullTask = Task.Run(async () =>
		{
			if (previousTask is not null)
			{
				try
				{
					await previousTask.ConfigureAwait(false);
				}
				catch
				{
				}
			}

			await RunPullAsync(snapshot, context, token).ConfigureAwait(false);
		}, token);
	}

	private async Task RunPullAsync(
		AiDownloadJobSnapshot snapshot,
		PullExecutionContext context,
		CancellationToken cancellationToken)
	{
		try
		{
			await RunPullCoreAsync(snapshot, context, cancellationToken).ConfigureAwait(false);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			ApplyPullCancelIntent();
		}
	}

	private async Task RunPullCoreAsync(
		AiDownloadJobSnapshot snapshot,
		PullExecutionContext context,
		CancellationToken cancellationToken)
	{
		var model = AiModelCatalog.TryGetById(snapshot.SelectedModelId);
		if (model is null)
		{
			SetFailed(TranslationKeys.AiDownloadFailed);
			return;
		}

		if (context.AllowInstall &&
			!OllamaPaths.IsManagedInstallPresent() &&
			!_diskSpaceChecker.HasSpaceForDownload(OllamaPaths.ManagedInstallReserveBytes))
		{
			SetFailed(TranslationKeys.AiDownloadInsufficientDiskSpace);
			return;
		}

		var pullContext = context;
		var reachability = await EnsureOllamaReachableAsync(pullContext, cancellationToken).ConfigureAwait(false);
		if (!reachability.Status.IsReachable)
		{
			if (pullContext.SkipReachabilityIfProbeOk &&
				HasPriorModelPullProgress(CurrentSnapshot) &&
				!OllamaPaths.IsManagedInstallPresent() &&
				await TryEnsureEngineInstalledAsync(cancellationToken).ConfigureAwait(false))
			{
				reachability = await EnsureOllamaReachableAsync(
						pullContext with { AllowInstall = false },
						cancellationToken)
					.ConfigureAwait(false);
			}

			if (!reachability.Status.IsReachable &&
				pullContext.SkipReachabilityIfProbeOk &&
				await TryPrepareFreshRestartAsync(model, cancellationToken).ConfigureAwait(false))
			{
				pullContext = CreateStartContext();
				reachability = await EnsureOllamaReachableAsync(pullContext, cancellationToken).ConfigureAwait(false);
			}

			if (!reachability.Status.IsReachable)
			{
				SetFailed(reachability.ErrorMessageKey ?? TranslationKeys.AiDownloadOllamaRequired);
				return;
			}
		}

		var preserveModelProgress = pullContext.SkipReachabilityIfProbeOk &&
									HasPriorModelPullProgress(CurrentSnapshot);

		if (!preserveModelProgress)
		{
			Publish(CurrentSnapshot with
			{
				CompletedBytes = null,
				TotalBytes = null,
				StatusText = AiDownloadStatus.FromTranslationKey(TranslationKeys.AiDownloadPullingModel),
				LastUpdatedAtUtc = _clock.UtcNow,
			}, forceFlush: true);
		}

		var progress = new Progress<OllamaPullProgress>(pullProgress =>
		{
			if (!_diskSpaceChecker.HasSpaceForDownload(model.ApproxDownloadBytes))
			{
				_cancelIntent = PullCancelIntent.None;
				_pullCts?.Cancel();
				SetFailed(TranslationKeys.AiDownloadInsufficientDiskSpace);
				return;
			}

			TryPublishActiveDownloadProgress(current => current with
			{
				CompletedBytes = pullProgress.Completed ?? current.CompletedBytes,
				TotalBytes = pullProgress.Total ?? current.TotalBytes,
				StatusText = string.IsNullOrWhiteSpace(pullProgress.Status)
					? current.StatusText
					: pullProgress.Status,
				LastUpdatedAtUtc = _clock.UtcNow,
			});
		});

		var result = await _pullClient
			.PullAsync(snapshot.OllamaModelTag, progress, cancellationToken)
			.ConfigureAwait(false);

		if (result.Outcome == OllamaPullOutcome.Failed &&
			context.SkipReachabilityIfProbeOk &&
			await TryPrepareFreshRestartAsync(model, cancellationToken).ConfigureAwait(false))
		{
			result = await _pullClient
				.PullAsync(snapshot.OllamaModelTag, progress, cancellationToken)
				.ConfigureAwait(false);
		}

		if (_cancelIntent != PullCancelIntent.None || result.Outcome == OllamaPullOutcome.Cancelled)
		{
			ApplyPullCancelIntent();
			return;
		}

		if (result.Outcome == OllamaPullOutcome.Succeeded)
		{
			_settingsStorage.Save(new AiSettingsSnapshot(
				snapshot.SelectedModelId,
				snapshot.OllamaModelTag,
				_clock.UtcNow));

			var completed = CurrentSnapshot with
			{
				State = AiDownloadJobState.Completed,
				CompletedBytes = CurrentSnapshot.TotalBytes ?? CurrentSnapshot.CompletedBytes,
				LastUpdatedAtUtc = _clock.UtcNow,
				ErrorMessageKey = null,
			};

			Publish(completed, forceFlush: true);
			await RunCompletedDwellAsync().ConfigureAwait(false);
			_jobStorage.Delete();
			SetIdle();
			return;
		}

		var failureKey = !_diskSpaceChecker.HasSpaceForDownload(model.ApproxDownloadBytes)
			? TranslationKeys.AiDownloadInsufficientDiskSpace
			: TranslationKeys.AiDownloadFailed;
		SetFailed(failureKey);
	}

	private void ApplyPullCancelIntent()
	{
		if (_cancelIntent == PullCancelIntent.Pause)
		{
			var paused = CurrentSnapshot with
			{
				State = AiDownloadJobState.Paused,
				LastUpdatedAtUtc = _clock.UtcNow,
			};
			Publish(paused, forceFlush: true);
		}
		else if (_cancelIntent == PullCancelIntent.Stop)
		{
			_jobStorage.Delete();
			SetIdle();
		}

		_cancelIntent = PullCancelIntent.None;
	}

	private async Task RunCompletedDwellAsync()
	{
		try
		{
			await Task.Delay(CompletedDwellDuration).ConfigureAwait(false);
		}
		catch
		{
		}
	}

	private async Task<OllamaReachabilityResult> EnsureOllamaReachableAsync(
		PullExecutionContext context,
		CancellationToken cancellationToken)
	{
		if (context.SkipReachabilityIfProbeOk)
		{
			var status = await OllamaStartupHelper.ProbeWithRetriesAsync(
					_ollamaProbe,
					context.ReachabilityOptions.InitialProbeAttempts,
					context.ReachabilityOptions.InitialProbeDelay,
					cancellationToken)
				.ConfigureAwait(false);
			if (status.IsReachable)
			{
				return new OllamaReachabilityResult(status, null);
			}

			status = await OllamaStartupHelper.ProbeWithRetriesAsync(
					_ollamaProbe,
					context.ReachabilityOptions.LaunchProbeAttempts,
					context.ReachabilityOptions.LaunchProbeDelay,
					cancellationToken)
				.ConfigureAwait(false);
			if (status.IsReachable)
			{
				return new OllamaReachabilityResult(status, null);
			}
		}

		return await OllamaStartupHelper.EnsureReachableAsync(
			_ollamaProbe,
			CreateInstallProgressReporter(),
			_ollamaInstaller,
			context.ReachabilityOptions,
			cancellationToken,
			allowInstall: context.AllowInstall).ConfigureAwait(false);
	}

	private IProgress<OllamaInstallProgress> CreateInstallProgressReporter() =>
		new Progress<OllamaInstallProgress>(ReportInstallProgress);

	private void ReportInstallProgress(OllamaInstallProgress installProgress)
	{
		var statusKey = installProgress.Phase switch
		{
			OllamaInstallPhase.DownloadingEngine => TranslationKeys.AiDownloadDownloadingEngine,
			OllamaInstallPhase.InstallingEngine => TranslationKeys.AiDownloadPreparingEngine,
			OllamaInstallPhase.StartingEngine => TranslationKeys.AiDownloadStartingEngine,
			_ => TranslationKeys.AiDownloadPreparingEngine,
		};

		AiDownloadJobSnapshot current;
		lock (_snapshotLock)
		{
			current = CurrentSnapshot;
		}

		var enteringEnginePhase = AiDownloadStatus.TryGetTranslationKey(current.StatusText, out var currentKey)
			? !IsEnginePhaseKey(currentKey)
			: HasPriorModelPullProgress(current);

		TryPublishActiveDownloadProgress(snapshot => snapshot with
		{
			StatusText = AiDownloadStatus.FromTranslationKey(statusKey),
			CompletedBytes = installProgress.CompletedBytes,
			TotalBytes = installProgress.TotalBytes,
			LastUpdatedAtUtc = _clock.UtcNow,
		}, forceFlush: enteringEnginePhase);
	}

	private static bool IsEnginePhaseKey(string statusKey) =>
		statusKey is TranslationKeys.AiDownloadPreparingEngine
			or TranslationKeys.AiDownloadDownloadingEngine
			or TranslationKeys.AiDownloadStartingEngine;

	private async Task<bool> TryEnsureEngineInstalledAsync(CancellationToken cancellationToken)
	{
		Publish(
			CurrentSnapshot with
			{
				StatusText = AiDownloadStatus.FromTranslationKey(TranslationKeys.AiDownloadPreparingEngine),
				LastUpdatedAtUtc = _clock.UtcNow,
			},
			forceFlush: true);

		var result = await _ollamaInstaller
			.EnsureInstalledAsync(CreateInstallProgressReporter(), cancellationToken)
			.ConfigureAwait(false);

		if (!result.Succeeded)
		{
			return false;
		}

		if (HasPriorModelPullProgress(CurrentSnapshot))
		{
			Publish(
				CurrentSnapshot with
				{
					StatusText = AiDownloadStatus.FromTranslationKey(TranslationKeys.AiDownloadPullingModel),
					LastUpdatedAtUtc = _clock.UtcNow,
				},
				forceFlush: true);
		}

		return true;
	}

	private async Task<bool> TryPrepareFreshRestartAsync(
		AiModelCatalogEntry model,
		CancellationToken cancellationToken)
	{
		Publish(CurrentSnapshot with
		{
			StatusText = AiDownloadStatus.FromTranslationKey(TranslationKeys.AiDownloadRestartingAfterRecovery),
			LastUpdatedAtUtc = _clock.UtcNow,
		});

		var reachability = await EnsureOllamaReachableAsync(
				CreateRecoveryContext(),
				cancellationToken)
			.ConfigureAwait(false);

		if (!reachability.Status.IsReachable)
		{
			return false;
		}

		await _modelDeleteClient
			.TryDeleteModelAsync(CurrentSnapshot.OllamaModelTag, cancellationToken)
			.ConfigureAwait(false);

		Publish(CurrentSnapshot with
		{
			CompletedBytes = null,
			TotalBytes = null,
			ErrorMessageKey = null,
			StatusText = AiDownloadStatus.FromTranslationKey(TranslationKeys.AiDownloadPullingModel),
			LastUpdatedAtUtc = _clock.UtcNow,
		});

		return true;
	}

	private void SetFailed(string errorMessageKey)
	{
		if (CurrentSnapshot.State == AiDownloadJobState.Idle)
		{
			return;
		}

		var failed = CurrentSnapshot with
		{
			State = AiDownloadJobState.Failed,
			ErrorMessageKey = errorMessageKey,
			LastUpdatedAtUtc = _clock.UtcNow,
		};

		Publish(failed, forceFlush: true);
	}

	private void SetIdle()
	{
		lock (_snapshotLock)
		{
			CurrentSnapshot = IdleSnapshot;
		}

		RaiseSnapshotChanged();
	}

	private void TryPublishActiveDownloadProgress(
		Func<AiDownloadJobSnapshot, AiDownloadJobSnapshot> transform,
		bool forceFlush = false)
	{
		AiDownloadJobSnapshot updated;
		lock (_snapshotLock)
		{
			if (CurrentSnapshot.State is not AiDownloadJobState.Downloading
				and not AiDownloadJobState.Interrupted)
			{
				return;
			}

			updated = transform(CurrentSnapshot);
			CurrentSnapshot = updated;
		}

		_jobStorage.Save(updated, forceFlush);
		RaiseSnapshotChanged();
	}

	private void Publish(AiDownloadJobSnapshot snapshot, bool forceFlush = false)
	{
		lock (_snapshotLock)
		{
			CurrentSnapshot = snapshot;
		}

		_jobStorage.Save(snapshot, forceFlush);
		RaiseSnapshotChanged();
	}

	private void RaiseSnapshotChanged()
	{
		AiDownloadJobSnapshot snapshot;
		lock (_snapshotLock)
		{
			snapshot = CurrentSnapshot;
		}

		SnapshotChanged?.Invoke(snapshot);
	}
}
