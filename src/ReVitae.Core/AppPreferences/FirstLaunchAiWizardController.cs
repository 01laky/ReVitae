namespace ReVitae.Core.AppPreferences;

public enum FirstLaunchAiWizardStep
{
	Welcome,
	ChoosePath,
	LocalSetup,
	OnlineSetup,
	Complete,
}

public enum FirstLaunchAiWizardCompleteKind
{
	RemindLater,
	DeclinedOffline,
	ActiveLocal,
	ActiveOnline,
	DownloadInProgress,
}

public sealed record FirstLaunchAiWizardState
{
	public FirstLaunchAiWizardStep Step { get; init; } = FirstLaunchAiWizardStep.Welcome;

	public FirstLaunchAiWizardCompleteKind CompleteKind { get; init; } = FirstLaunchAiWizardCompleteKind.RemindLater;

	public bool IsSuspendedForSubModal { get; init; }

	public bool ReturnToWelcomeAfterSetup { get; init; }

	public bool ManualRerun { get; init; }
}

public sealed class FirstLaunchAiWizardController
{
	public FirstLaunchAiWizardState State { get; private set; } = new();

	public void Open(bool manualRerun)
	{
		State = new FirstLaunchAiWizardState
		{
			Step = FirstLaunchAiWizardStep.Welcome,
			ManualRerun = manualRerun,
		};
	}

	public void Close()
	{
		if (!State.IsSuspendedForSubModal)
		{
			State = State with { ManualRerun = false };
		}
	}

	public FirstLaunchAiWizardState NavigateTo(FirstLaunchAiWizardStep step) =>
		State = State with { Step = step };

	public FirstLaunchAiWizardState ShowComplete(FirstLaunchAiWizardCompleteKind kind) =>
		State = State with
		{
			CompleteKind = kind,
			Step = FirstLaunchAiWizardStep.Complete,
		};

	public FirstLaunchAiWizardState SuspendForSubModal(bool returnToWelcomeAfterSetup = false) =>
		State = State with
		{
			IsSuspendedForSubModal = true,
			ReturnToWelcomeAfterSetup = returnToWelcomeAfterSetup,
		};

	public FirstLaunchAiWizardState ResumeAfterSubModal(FirstLaunchAiWizardStep step) =>
		State = State with
		{
			IsSuspendedForSubModal = false,
			ReturnToWelcomeAfterSetup = false,
			Step = step,
		};

	public FirstLaunchAiWizardState HandleBack()
	{
		var next = State.Step switch
		{
			FirstLaunchAiWizardStep.ChoosePath => FirstLaunchAiWizardStep.Welcome,
			FirstLaunchAiWizardStep.LocalSetup or FirstLaunchAiWizardStep.OnlineSetup => FirstLaunchAiWizardStep.ChoosePath,
			FirstLaunchAiWizardStep.Complete => FirstLaunchAiWizardStep.ChoosePath,
			_ => State.Step,
		};

		return State = State with { Step = next };
	}

	public FirstLaunchAiWizardState HandleNext() =>
		State.Step == FirstLaunchAiWizardStep.Welcome
			? State = State with { Step = FirstLaunchAiWizardStep.ChoosePath }
			: State;

	public FirstLaunchAiWizardState HandleEscapeSkip() =>
		State.Step == FirstLaunchAiWizardStep.Complete
			? State
			: ShowComplete(FirstLaunchAiWizardCompleteKind.RemindLater);

	public int GetStepNumber() =>
		State.Step switch
		{
			FirstLaunchAiWizardStep.Welcome => 1,
			FirstLaunchAiWizardStep.ChoosePath => 2,
			FirstLaunchAiWizardStep.LocalSetup or FirstLaunchAiWizardStep.OnlineSetup => 3,
			FirstLaunchAiWizardStep.Complete => 4,
			_ => 1,
		};

	public bool ShouldShowOnStartup(bool recoveryExists, bool shouldShowWizardPreference) =>
		!State.ManualRerun && !recoveryExists && shouldShowWizardPreference;
}
