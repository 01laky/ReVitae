using ReVitae.Core.Ai.Providers;

namespace ReVitae.Core.AppPreferences;

public sealed class AppPreferencesService
{
	public const string ResetWizardEnvironmentVariable = "REVITAE_RESET_AI_WIZARD";

	private readonly AppPreferencesRepository _repository;
	private AppPreferencesDocument _current;

	public AppPreferencesService()
		: this(new AppPreferencesRepository())
	{
	}

	public AppPreferencesService(AppPreferencesRepository repository)
	{
		_repository = repository;
		_current = LoadInitial();
	}

	public AppPreferencesDocument Current => _current;

	public bool ShouldShowFirstLaunchAiWizard(AiSettingsDocument aiSettings)
	{
		if (_current.FirstLaunchAiWizardStatus != FirstLaunchAiWizardStatus.NotStarted)
		{
			return false;
		}

		if (aiSettings.ActiveBackend != AiBackendKind.None)
		{
			return false;
		}

		return true;
	}

	public bool ShouldShowAiPromotionsInUi(AiSettingsDocument aiSettings)
	{
		if (aiSettings.ActiveBackend != AiBackendKind.None)
		{
			return true;
		}

		return !_current.HideAiPromotionsInUi;
	}

	public void MarkRemindLater()
	{
		Update(_current with
		{
			FirstLaunchAiWizardStatus = FirstLaunchAiWizardStatus.RemindLater,
			FirstLaunchAiWizardCompletedAtUtc = DateTimeOffset.UtcNow,
			HideAiPromotionsInUi = false,
		});
	}

	public void MarkCompleted(bool clearHideAiPromotions = true)
	{
		Update(_current with
		{
			FirstLaunchAiWizardStatus = FirstLaunchAiWizardStatus.Completed,
			FirstLaunchAiWizardCompletedAtUtc = DateTimeOffset.UtcNow,
			HideAiPromotionsInUi = clearHideAiPromotions ? false : _current.HideAiPromotionsInUi,
		});
	}

	public void MarkDeclinedOffline()
	{
		Update(_current with
		{
			FirstLaunchAiWizardStatus = FirstLaunchAiWizardStatus.DeclinedOffline,
			FirstLaunchAiWizardCompletedAtUtc = DateTimeOffset.UtcNow,
			HideAiPromotionsInUi = true,
		});
	}

	public void ClearHideAiPromotionsOnBackendActivated()
	{
		if (!_current.HideAiPromotionsInUi)
		{
			return;
		}

		Update(_current with { HideAiPromotionsInUi = false });
	}

	public void Reload() => _current = _repository.LoadOrDefault();

	private AppPreferencesDocument LoadInitial()
	{
		if (ShouldResetFromEnvironment())
		{
			var reset = AppPreferencesDocument.Default;
			_repository.Save(reset);
			return reset;
		}

		return _repository.LoadOrDefault();
	}

	private static bool ShouldResetFromEnvironment()
	{
		var value = Environment.GetEnvironmentVariable(ResetWizardEnvironmentVariable);
		return string.Equals(value, "1", StringComparison.Ordinal) ||
			   string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
	}

	private void Update(AppPreferencesDocument document)
	{
		_current = document;
		_repository.Save(document);
	}
}
