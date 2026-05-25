using ReVitae.Core.Ai.Providers;

namespace ReVitae.Tests.AppPreferences;

public sealed class FirstLaunchAiWizardCuratedProvidersTests
{
	[Fact]
	public void ProviderIds_ContainsFourCuratedEntries()
	{
		Assert.Equal(4, FirstLaunchAiWizardCuratedProviders.ProviderIds.Count);
		Assert.Equal(
			["openai", "anthropic", "google-gemini", "groq"],
			FirstLaunchAiWizardCuratedProviders.ProviderIds);
	}

	[Fact]
	public void GetDefinitions_ResolvesAllCatalogEntries()
	{
		var definitions = FirstLaunchAiWizardCuratedProviders.GetDefinitions();

		Assert.Equal(FirstLaunchAiWizardCuratedProviders.ProviderIds.Count, definitions.Count);
		Assert.All(definitions, definition => Assert.Contains(definition.Id, FirstLaunchAiWizardCuratedProviders.ProviderIds));
		Assert.All(definitions, definition => Assert.False(string.IsNullOrWhiteSpace(definition.DisplayNameKey)));
	}

	[Fact]
	public void GetDefinitions_PreservesCatalogOrder()
	{
		var definitions = FirstLaunchAiWizardCuratedProviders.GetDefinitions();

		Assert.Equal(FirstLaunchAiWizardCuratedProviders.ProviderIds, definitions.Select(definition => definition.Id).ToList());
	}
}
