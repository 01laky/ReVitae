namespace ReVitae.Core.Ai.Providers;

public static class FirstLaunchAiWizardCuratedProviders
{
	public static readonly IReadOnlyList<string> ProviderIds =
	[
		"openai",
		"anthropic",
		"google-gemini",
		"groq",
	];

	public static IReadOnlyList<AiOnlineProviderDefinition> GetDefinitions() =>
		ProviderIds
			.Select(AiOnlineProviderCatalog.TryGetById)
			.Where(definition => definition is not null)
			.Cast<AiOnlineProviderDefinition>()
			.ToList();
}
