namespace ReVitae.Core.Ai.Providers;

public static class AiProviderRowStateMapper
{
	public static AiProviderRowPresentation Map(
		AiOnlineProviderDefinition provider,
		AiSettingsDocument settings,
		IAiSecretStorage secretStorage)
	{
		settings.OnlineProviders.TryGetValue(provider.Id, out var config);
		var configured = AiProviderConfigValidator.IsConfigured(provider, config, secretStorage);
		var isActive = settings.ActiveBackend == AiBackendKind.Online &&
					   string.Equals(settings.ActiveOnlineProviderId, provider.Id, StringComparison.Ordinal);

		var primary = isActive
			? AiProviderUiAction.Deactivate
			: configured
				? AiProviderUiAction.Activate
				: AiProviderUiAction.Configure;

		return new AiProviderRowPresentation(
			provider.Id,
			primary,
			ShowEditLink: configured || isActive,
			IsConfigured: configured,
			IsActive: isActive,
			ShowFreeTierBadge: provider.HasFreeTierBadge,
			LastTestSucceeded: config?.LastTestSucceeded);
	}
}
