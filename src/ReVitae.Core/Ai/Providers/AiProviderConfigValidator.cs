namespace ReVitae.Core.Ai.Providers;

public static class AiProviderConfigValidator
{
	public static bool IsConfigured(
		AiOnlineProviderDefinition provider,
		AiProviderConnectionConfig? savedConfig,
		IAiSecretStorage secretStorage)
	{
		var draft = ToDraft(provider, savedConfig, secretStorage.TryGetApiKey(provider.Id));
		return IsValid(provider, draft, requireApiKey: true);
	}

	public static bool IsValid(
		AiOnlineProviderDefinition provider,
		AiProviderConnectionDraft draft,
		bool requireApiKey = false)
	{
		foreach (var field in provider.Fields.Where(field => field.Required))
		{
			if (field.Id == AiProviderFieldIds.ApiKey)
			{
				if (requireApiKey && string.IsNullOrWhiteSpace(draft.ApiKey))
				{
					return false;
				}

				continue;
			}

			if (!TryGetValue(draft, field.Id, out var value) || string.IsNullOrWhiteSpace(value))
			{
				return false;
			}
		}

		if (provider.Id == "custom-openai")
		{
			return TryGetValue(draft, AiProviderFieldIds.BaseUrl, out var baseUrl) &&
				   !string.IsNullOrWhiteSpace(baseUrl) &&
				   TryGetValue(draft, AiProviderFieldIds.ModelId, out var modelId) &&
				   !string.IsNullOrWhiteSpace(modelId);
		}

		if (provider.Id == "azure-openai")
		{
			return TryGetValue(draft, AiProviderFieldIds.BaseUrl, out var baseUrl) &&
				   !string.IsNullOrWhiteSpace(baseUrl) &&
				   TryGetValue(draft, AiProviderFieldIds.DeploymentName, out var deployment) &&
				   !string.IsNullOrWhiteSpace(deployment) &&
				   !string.IsNullOrWhiteSpace(draft.ApiKey);
		}

		if (provider.ApiStyle != AiOnlineApiStyle.OpenAiCompatible || provider.Fields.Any(field => field.Id == AiProviderFieldIds.ModelId))
		{
			return TryGetValue(draft, AiProviderFieldIds.ModelId, out var modelId) &&
				   !string.IsNullOrWhiteSpace(modelId) &&
				   (!requireApiKey || !string.IsNullOrWhiteSpace(draft.ApiKey));
		}

		return !requireApiKey || !string.IsNullOrWhiteSpace(draft.ApiKey);
	}

	public static AiProviderConnectionDraft ToDraft(
		AiOnlineProviderDefinition provider,
		AiProviderConnectionConfig? config,
		string? apiKey)
	{
		var values = new Dictionary<string, string?>(StringComparer.Ordinal);
		foreach (var field in provider.Fields)
		{
			values[field.Id] = field.Id switch
			{
				AiProviderFieldIds.ModelId => config?.ModelId,
				AiProviderFieldIds.BaseUrl => config?.BaseUrl ?? provider.DefaultBaseUrl,
				AiProviderFieldIds.DeploymentName => config?.DeploymentName,
				AiProviderFieldIds.ApiVersion => config?.ApiVersion ?? "2024-08-01-preview",
				AiProviderFieldIds.OrganizationId => config?.OrganizationId,
				_ => null,
			};
		}

		if (provider.Id == "azure-openai" && string.IsNullOrWhiteSpace(values[AiProviderFieldIds.ApiVersion]))
		{
			values[AiProviderFieldIds.ApiVersion] = "2024-08-01-preview";
		}

		return new AiProviderConnectionDraft(provider.Id, values, apiKey);
	}

	public static AiProviderConnectionConfig ToConfig(
		AiOnlineProviderDefinition provider,
		AiProviderConnectionDraft draft,
		AiProviderConnectionConfig? existing)
	{
		return new AiProviderConnectionConfig(
			provider.Id,
			TryGetDraftValue(draft, AiProviderFieldIds.ModelId) ??
			TryGetDraftValue(draft, AiProviderFieldIds.DeploymentName),
			NormalizeBaseUrl(provider, draft),
			TryGetDraftValue(draft, AiProviderFieldIds.OrganizationId),
			TryGetDraftValue(draft, AiProviderFieldIds.DeploymentName),
			TryGetDraftValue(draft, AiProviderFieldIds.ApiVersion) ?? "2024-08-01-preview",
			existing?.LastTestedAtUtc,
			existing?.LastTestSucceeded);
	}

	public static string? ResolveModelId(AiOnlineProviderDefinition provider, AiProviderConnectionDraft draft) =>
		provider.Id == "azure-openai"
			? TryGetDraftValue(draft, AiProviderFieldIds.DeploymentName)
			: TryGetDraftValue(draft, AiProviderFieldIds.ModelId);

	public static string? NormalizeBaseUrl(AiOnlineProviderDefinition provider, AiProviderConnectionDraft draft)
	{
		var baseUrl = TryGetDraftValue(draft, AiProviderFieldIds.BaseUrl) ?? provider.DefaultBaseUrl;
		return string.IsNullOrWhiteSpace(baseUrl) ? null : baseUrl.Trim().TrimEnd('/');
	}

	private static string? TryGetDraftValue(AiProviderConnectionDraft draft, string fieldId) =>
		TryGetValue(draft, fieldId, out var value) ? value : null;

	private static bool TryGetValue(AiProviderConnectionDraft draft, string fieldId, out string? value)
	{
		if (draft.Values.TryGetValue(fieldId, out value))
		{
			return true;
		}

		value = null;
		return false;
	}
}
