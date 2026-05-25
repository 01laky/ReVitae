namespace ReVitae.Core.Projects;

using ReVitae.Core.Localization;

public enum AutosaveWriteStatus
{
	SkippedNotDirty,
	SkippedNoFormData,
	SkippedDebounce,
	Written,
	Failed
}

public sealed record AutosaveWriteResult(
	AutosaveWriteStatus Status,
	Exception? Error = null);

public enum RecoveryRestoreRecommendation
{
	NoneAvailable,
	RestoreRecovery,
	PreferPrimaryProject,
	DiscardCorruptRecovery
}

public sealed record RecoveryEvaluationResult(
	RecoveryRestoreRecommendation Recommendation,
	CvProjectLoadResult? RecoveryLoadResult = null);

public sealed class CvProjectLifecycleService
{
	private readonly IClock _clock;
	private readonly IProjectAutosaveStore _autosaveStore;
	private bool _isDirty;
	private DateTimeOffset _lastAutosaveUtc = DateTimeOffset.MinValue;

	public CvProjectLifecycleService(
		IClock clock,
		IProjectAutosaveStore autosaveStore,
		int autosaveIntervalSeconds = CvProjectConstants.AutosaveIntervalSeconds,
		TimeSpan? debounceInterval = null)
	{
		_clock = clock;
		_autosaveStore = autosaveStore;
		AutosaveIntervalSeconds = autosaveIntervalSeconds;
		DebounceInterval = debounceInterval ?? TimeSpan.FromSeconds(Math.Max(0, autosaveIntervalSeconds - 1));
	}

	public int AutosaveIntervalSeconds { get; }

	public TimeSpan DebounceInterval { get; }

	public bool IsDirty => _isDirty;

	public DateTimeOffset LastAutosaveUtc => _lastAutosaveUtc;

	public void MarkDirty()
	{
		_isDirty = true;
	}

	public void ClearDirty()
	{
		_isDirty = false;
	}

	public void SetDirty(bool isDirty) =>
		_isDirty = isDirty;

	public bool HasUnsavedChanges() =>
		_isDirty;

	public bool ShouldWriteAutosave(bool hasFormData)
	{
		if (!_isDirty || !hasFormData)
		{
			return false;
		}

		var elapsed = _clock.UtcNow - _lastAutosaveUtc;
		return elapsed >= DebounceInterval;
	}

	public AutosaveWriteResult TryWriteAutosaveRecovery(CvProjectSaveRequest request, bool hasFormData)
	{
		if (!_isDirty)
		{
			return new AutosaveWriteResult(AutosaveWriteStatus.SkippedNotDirty);
		}

		if (!hasFormData)
		{
			return new AutosaveWriteResult(AutosaveWriteStatus.SkippedNoFormData);
		}

		if (!ShouldWriteAutosave(hasFormData))
		{
			return new AutosaveWriteResult(AutosaveWriteStatus.SkippedDebounce);
		}

		try
		{
			_autosaveStore.WriteRecovery(request);
			_lastAutosaveUtc = _clock.UtcNow;
			return new AutosaveWriteResult(AutosaveWriteStatus.Written);
		}
		catch (Exception ex)
		{
			return new AutosaveWriteResult(AutosaveWriteStatus.Failed, ex);
		}
	}

	public void OnManualSaveSucceeded()
	{
		ClearDirty();
		_autosaveStore.DeleteRecovery();
	}

	public void OnProjectLoaded(bool fromRecovery)
	{
		ClearDirty();
		_autosaveStore.DeleteRecovery();
		if (!fromRecovery)
		{
			_lastAutosaveUtc = DateTimeOffset.MinValue;
		}
	}

	public void DiscardRecovery() =>
		_autosaveStore.DeleteRecovery();

	public bool RecoveryExists() =>
		_autosaveStore.RecoveryExists();

	public RecoveryEvaluationResult EvaluateRecovery(DateTimeOffset? primaryProjectLastWriteUtc)
	{
		if (!_autosaveStore.RecoveryExists())
		{
			return new RecoveryEvaluationResult(RecoveryRestoreRecommendation.NoneAvailable);
		}

		var recoveryWriteUtc = _autosaveStore.GetRecoveryLastWriteUtc();
		var loadResult = _autosaveStore.LoadRecovery();
		if (!loadResult.Success || loadResult.Import is null)
		{
			return new RecoveryEvaluationResult(RecoveryRestoreRecommendation.DiscardCorruptRecovery, loadResult);
		}

		if (primaryProjectLastWriteUtc is { } primaryUtc
			&& recoveryWriteUtc is { } recoveryUtc
			&& primaryUtc >= recoveryUtc)
		{
			return new RecoveryEvaluationResult(RecoveryRestoreRecommendation.PreferPrimaryProject, loadResult);
		}

		return new RecoveryEvaluationResult(RecoveryRestoreRecommendation.RestoreRecovery, loadResult);
	}

	public CvProjectLoadResult LoadValidatedProject(string filePath)
	{
		var validation = CvProjectPathValidator.ValidateOpenPath(filePath);
		if (!validation.IsValid || validation.NormalizedPath is null)
		{
			return new CvProjectLoadResult(false, null, null, TranslationKeys.ProjectOpenFailed);
		}

		if (!File.Exists(validation.NormalizedPath))
		{
			return new CvProjectLoadResult(false, null, null, TranslationKeys.ProjectRecentMissing);
		}

		return CvProjectService.Load(validation.NormalizedPath);
	}

	public void SaveValidatedProject(string filePath, CvProjectSaveRequest request)
	{
		var validation = CvProjectPathValidator.ValidateSavePath(filePath);
		if (!validation.IsValid || validation.NormalizedPath is null)
		{
			throw new InvalidOperationException($"Invalid project save path: {validation.Failure}");
		}

		var directory = Path.GetDirectoryName(validation.NormalizedPath);
		if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
		{
			Directory.CreateDirectory(directory);
		}

		CvProjectService.Save(validation.NormalizedPath, request);
	}
}
