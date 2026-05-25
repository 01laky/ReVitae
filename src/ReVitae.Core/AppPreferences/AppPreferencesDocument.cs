namespace ReVitae.Core.AppPreferences;

public sealed record AppPreferencesDocument(
	int SchemaVersion,
	FirstLaunchAiWizardStatus FirstLaunchAiWizardStatus,
	DateTimeOffset? FirstLaunchAiWizardCompletedAtUtc,
	bool HideAiPromotionsInUi)
{
	public const int CurrentSchemaVersion = 2;

	public static AppPreferencesDocument Default { get; } = new(
		CurrentSchemaVersion,
		FirstLaunchAiWizardStatus.NotStarted,
		null,
		HideAiPromotionsInUi: false);
}
