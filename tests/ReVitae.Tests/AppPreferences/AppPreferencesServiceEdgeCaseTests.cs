using ReVitae.Core.AppPreferences;
using ReVitae.Core.Ai.Providers;

namespace ReVitae.Tests.AppPreferences;

public sealed class AppPreferencesServiceEdgeCaseTests : IDisposable
{
	private readonly AppPreferencesTestPaths _paths = new();
	private readonly string? _originalResetVariable;

	public AppPreferencesServiceEdgeCaseTests()
	{
		_originalResetVariable = Environment.GetEnvironmentVariable(AppPreferencesService.ResetWizardEnvironmentVariable);
		Environment.SetEnvironmentVariable(AppPreferencesService.ResetWizardEnvironmentVariable, null);
	}

	public void Dispose()
	{
		Environment.SetEnvironmentVariable(AppPreferencesService.ResetWizardEnvironmentVariable, _originalResetVariable);
		_paths.Dispose();
	}

	[Fact]
	public void ShouldShowFirstLaunchAiWizard_NotStartedWithoutBackend_ReturnsTrue()
	{
		var service = CreateService();

		Assert.True(service.ShouldShowFirstLaunchAiWizard(AiSettingsDocument.Empty));
	}

	[Theory]
	[InlineData(AiBackendKind.Local)]
	[InlineData(AiBackendKind.Online)]
	public void ShouldShowFirstLaunchAiWizard_ActiveBackend_ReturnsFalse(AiBackendKind backend)
	{
		var service = CreateService();
		var settings = AiSettingsDocument.Empty with { ActiveBackend = backend };

		Assert.False(service.ShouldShowFirstLaunchAiWizard(settings));
	}

	[Theory]
	[InlineData(FirstLaunchAiWizardStatus.RemindLater)]
	[InlineData(FirstLaunchAiWizardStatus.Completed)]
	[InlineData(FirstLaunchAiWizardStatus.DeclinedOffline)]
	public void ShouldShowFirstLaunchAiWizard_NonNotStartedStatus_ReturnsFalse(FirstLaunchAiWizardStatus status)
	{
		var repository = _paths.CreateRepository();
		repository.Save(AppPreferencesDocument.Default with { FirstLaunchAiWizardStatus = status });
		var service = new AppPreferencesService(repository);

		Assert.False(service.ShouldShowFirstLaunchAiWizard(AiSettingsDocument.Empty));
	}

	[Fact]
	public void ShouldShowAiPromotionsInUi_ActiveBackend_ReturnsTrueEvenWhenHideFlagSet()
	{
		var repository = _paths.CreateRepository();
		repository.Save(AppPreferencesDocument.Default with
		{
			FirstLaunchAiWizardStatus = FirstLaunchAiWizardStatus.DeclinedOffline,
			HideAiPromotionsInUi = true,
		});
		var service = new AppPreferencesService(repository);

		Assert.True(service.ShouldShowAiPromotionsInUi(AiSettingsDocument.Empty with
		{
			ActiveBackend = AiBackendKind.Local,
		}));
	}

	[Fact]
	public void ShouldShowAiPromotionsInUi_DeclinedOfflineWithoutBackend_ReturnsFalse()
	{
		var service = CreateService();
		service.MarkDeclinedOffline();

		Assert.False(service.ShouldShowAiPromotionsInUi(AiSettingsDocument.Empty));
	}

	[Fact]
	public void ShouldShowAiPromotionsInUi_RemindLaterWithoutBackend_ReturnsTrue()
	{
		var service = CreateService();
		service.MarkRemindLater();

		Assert.True(service.ShouldShowAiPromotionsInUi(AiSettingsDocument.Empty));
	}

	[Fact]
	public void ShouldShowAiPromotionsInUi_CompletedWithoutBackend_ReturnsTrue()
	{
		var service = CreateService();
		service.MarkCompleted();

		Assert.True(service.ShouldShowAiPromotionsInUi(AiSettingsDocument.Empty));
	}

	[Fact]
	public void MarkRemindLater_PersistsStatusAndTimestamp()
	{
		var service = CreateService();

		service.MarkRemindLater();

		Assert.Equal(FirstLaunchAiWizardStatus.RemindLater, service.Current.FirstLaunchAiWizardStatus);
		Assert.NotNull(service.Current.FirstLaunchAiWizardCompletedAtUtc);
		Assert.False(service.Current.HideAiPromotionsInUi);
		Assert.Equal(
			FirstLaunchAiWizardStatus.RemindLater,
			_paths.CreateRepository().LoadOrDefault().FirstLaunchAiWizardStatus);
	}

	[Fact]
	public void MarkCompleted_ClearsHidePromotionsByDefault()
	{
		var repository = _paths.CreateRepository();
		repository.Save(AppPreferencesDocument.Default with
		{
			HideAiPromotionsInUi = true,
		});
		var service = new AppPreferencesService(repository);

		service.MarkCompleted();

		Assert.Equal(FirstLaunchAiWizardStatus.Completed, service.Current.FirstLaunchAiWizardStatus);
		Assert.False(service.Current.HideAiPromotionsInUi);
	}

	[Fact]
	public void MarkCompleted_PreservesHidePromotionsWhenRequested()
	{
		var repository = _paths.CreateRepository();
		repository.Save(AppPreferencesDocument.Default with
		{
			HideAiPromotionsInUi = true,
		});
		var service = new AppPreferencesService(repository);

		service.MarkCompleted(clearHideAiPromotions: false);

		Assert.True(service.Current.HideAiPromotionsInUi);
	}

	[Fact]
	public void MarkDeclinedOffline_SetsHidePromotionsAndPersists()
	{
		var service = CreateService();

		service.MarkDeclinedOffline();

		Assert.Equal(FirstLaunchAiWizardStatus.DeclinedOffline, service.Current.FirstLaunchAiWizardStatus);
		Assert.True(service.Current.HideAiPromotionsInUi);
		Assert.NotNull(service.Current.FirstLaunchAiWizardCompletedAtUtc);
	}

	[Fact]
	public void ClearHideAiPromotionsOnBackendActivated_ClearsFlag()
	{
		var service = CreateService();
		service.MarkDeclinedOffline();

		service.ClearHideAiPromotionsOnBackendActivated();

		Assert.False(service.Current.HideAiPromotionsInUi);
		Assert.Equal(
			FirstLaunchAiWizardStatus.DeclinedOffline,
			service.Current.FirstLaunchAiWizardStatus);
	}

	[Fact]
	public void ClearHideAiPromotionsOnBackendActivated_NoOpWhenAlreadyFalse()
	{
		var service = CreateService();
		service.MarkRemindLater();

		service.ClearHideAiPromotionsOnBackendActivated();

		Assert.False(service.Current.HideAiPromotionsInUi);
	}

	[Fact]
	public void Reload_RefreshesFromDisk()
	{
		var repository = _paths.CreateRepository();
		var service = new AppPreferencesService(repository);
		service.MarkRemindLater();

		repository.Save(AppPreferencesDocument.Default with
		{
			FirstLaunchAiWizardStatus = FirstLaunchAiWizardStatus.Completed,
		});
		service.Reload();

		Assert.Equal(FirstLaunchAiWizardStatus.Completed, service.Current.FirstLaunchAiWizardStatus);
	}

	[Theory]
	[InlineData("1")]
	[InlineData("true")]
	[InlineData("TRUE")]
	public void Constructor_ResetEnvironmentVariable_ResetsWizard(string resetValue)
	{
		var repository = _paths.CreateRepository();
		repository.Save(AppPreferencesDocument.Default with
		{
			FirstLaunchAiWizardStatus = FirstLaunchAiWizardStatus.RemindLater,
			HideAiPromotionsInUi = true,
		});

		Environment.SetEnvironmentVariable(AppPreferencesService.ResetWizardEnvironmentVariable, resetValue);
		var service = new AppPreferencesService(repository);

		Assert.Equal(FirstLaunchAiWizardStatus.NotStarted, service.Current.FirstLaunchAiWizardStatus);
		Assert.False(service.Current.HideAiPromotionsInUi);
		Assert.Equal(
			FirstLaunchAiWizardStatus.NotStarted,
			repository.LoadOrDefault().FirstLaunchAiWizardStatus);
	}

	private AppPreferencesService CreateService() => new(_paths.CreateRepository());
}
