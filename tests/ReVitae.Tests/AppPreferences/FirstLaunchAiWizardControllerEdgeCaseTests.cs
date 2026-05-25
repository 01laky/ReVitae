using ReVitae.Core.AppPreferences;
using ReVitae.Core.Ai.Providers;

namespace ReVitae.Tests.AppPreferences;

public sealed class FirstLaunchAiWizardControllerEdgeCaseTests
{
	[Fact]
	public void Open_ResetsToWelcomeStep()
	{
		var controller = new FirstLaunchAiWizardController();

		controller.Open(manualRerun: false);

		Assert.Equal(FirstLaunchAiWizardStep.Welcome, controller.State.Step);
		Assert.False(controller.State.ManualRerun);
	}

	[Fact]
	public void Open_ManualRerun_SetsFlag()
	{
		var controller = new FirstLaunchAiWizardController();

		controller.Open(manualRerun: true);

		Assert.True(controller.State.ManualRerun);
	}

	[Fact]
	public void HandleNext_FromWelcome_GoesToChoosePath()
	{
		var controller = new FirstLaunchAiWizardController();
		controller.Open(false);

		controller.HandleNext();

		Assert.Equal(FirstLaunchAiWizardStep.ChoosePath, controller.State.Step);
	}

	[Theory]
	[InlineData(FirstLaunchAiWizardStep.ChoosePath, FirstLaunchAiWizardStep.Welcome)]
	[InlineData(FirstLaunchAiWizardStep.LocalSetup, FirstLaunchAiWizardStep.ChoosePath)]
	[InlineData(FirstLaunchAiWizardStep.OnlineSetup, FirstLaunchAiWizardStep.ChoosePath)]
	[InlineData(FirstLaunchAiWizardStep.Complete, FirstLaunchAiWizardStep.ChoosePath)]
	public void HandleBack_NavigatesToPreviousStep(FirstLaunchAiWizardStep current, FirstLaunchAiWizardStep expected)
	{
		var controller = new FirstLaunchAiWizardController();
		controller.Open(false);
		controller.NavigateTo(current);

		controller.HandleBack();

		Assert.Equal(expected, controller.State.Step);
	}

	[Fact]
	public void HandleEscapeSkip_ShowsRemindLaterComplete()
	{
		var controller = new FirstLaunchAiWizardController();
		controller.Open(false);
		controller.NavigateTo(FirstLaunchAiWizardStep.LocalSetup);

		controller.HandleEscapeSkip();

		Assert.Equal(FirstLaunchAiWizardStep.Complete, controller.State.Step);
		Assert.Equal(FirstLaunchAiWizardCompleteKind.RemindLater, controller.State.CompleteKind);
	}

	[Fact]
	public void HandleEscapeSkip_OnCompleteStep_IsNoOp()
	{
		var controller = new FirstLaunchAiWizardController();
		controller.Open(false);
		controller.ShowComplete(FirstLaunchAiWizardCompleteKind.ActiveLocal);

		var state = controller.HandleEscapeSkip();

		Assert.Equal(FirstLaunchAiWizardCompleteKind.ActiveLocal, state.CompleteKind);
	}

	[Fact]
	public void SuspendAndResume_SubModalFlow()
	{
		var controller = new FirstLaunchAiWizardController();
		controller.Open(false);
		controller.NavigateTo(FirstLaunchAiWizardStep.LocalSetup);

		controller.SuspendForSubModal(returnToWelcomeAfterSetup: true);
		Assert.True(controller.State.IsSuspendedForSubModal);
		Assert.True(controller.State.ReturnToWelcomeAfterSetup);

		controller.ResumeAfterSubModal(FirstLaunchAiWizardStep.LocalSetup);
		Assert.False(controller.State.IsSuspendedForSubModal);
		Assert.Equal(FirstLaunchAiWizardStep.LocalSetup, controller.State.Step);
	}

	[Fact]
	public void Close_ClearsManualRerunWhenNotSuspended()
	{
		var controller = new FirstLaunchAiWizardController();
		controller.Open(manualRerun: true);

		controller.Close();

		Assert.False(controller.State.ManualRerun);
	}

	[Theory]
	[InlineData(FirstLaunchAiWizardStep.Welcome, 1)]
	[InlineData(FirstLaunchAiWizardStep.ChoosePath, 2)]
	[InlineData(FirstLaunchAiWizardStep.LocalSetup, 3)]
	[InlineData(FirstLaunchAiWizardStep.Complete, 4)]
	public void GetStepNumber_MapsSteps(FirstLaunchAiWizardStep step, int expected)
	{
		var controller = new FirstLaunchAiWizardController();
		controller.Open(false);
		controller.NavigateTo(step);

		Assert.Equal(expected, controller.GetStepNumber());
	}

	[Fact]
	public void ShouldShowOnStartup_BlocksWhenRecoveryExists()
	{
		var controller = new FirstLaunchAiWizardController();
		controller.Open(false);

		Assert.False(controller.ShouldShowOnStartup(recoveryExists: true, shouldShowWizardPreference: true));
	}

	[Fact]
	public void ShouldShowOnStartup_AllowsWhenPreferenceTrueAndNoRecovery()
	{
		var controller = new FirstLaunchAiWizardController();
		controller.Open(false);

		Assert.True(controller.ShouldShowOnStartup(recoveryExists: false, shouldShowWizardPreference: true));
	}

	[Fact]
	public void ShowComplete_SetsCompleteKindAndStep()
	{
		var controller = new FirstLaunchAiWizardController();
		controller.Open(false);

		controller.ShowComplete(FirstLaunchAiWizardCompleteKind.DeclinedOffline);

		Assert.Equal(FirstLaunchAiWizardStep.Complete, controller.State.Step);
		Assert.Equal(FirstLaunchAiWizardCompleteKind.DeclinedOffline, controller.State.CompleteKind);
	}
}
