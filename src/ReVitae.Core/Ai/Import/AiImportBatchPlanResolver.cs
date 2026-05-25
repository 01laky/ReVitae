using ReVitae.Core.Ai;
using ReVitae.Core.Ai.Providers;
using ReVitae.Core.Import;

namespace ReVitae.Core.Ai.Import;

public static partial class AiImportOnlineModelProfileMap
{
	private static readonly (string Pattern, AiImportBatchProfile Profile)[] Rules =
	[
		("gpt-4o-mini", AiImportBatchProfile.Small),
		("gpt-4.1-mini", AiImportBatchProfile.Small),
		("gpt-4.1-nano", AiImportBatchProfile.Small),
		("claude-3-5-haiku", AiImportBatchProfile.Small),
		("claude-haiku", AiImportBatchProfile.Small),
		("gemini-2.0-flash", AiImportBatchProfile.Small),
		("gemini-flash", AiImportBatchProfile.Small),
		("open-mistral-nemo", AiImportBatchProfile.Small),
		("gpt-4o", AiImportBatchProfile.Large),
		("gpt-4.1", AiImportBatchProfile.Large),
		("claude-sonnet", AiImportBatchProfile.Large),
		("claude-3-5-sonnet", AiImportBatchProfile.Large),
		("gemini-2.5-pro", AiImportBatchProfile.Large),
		("llama-3.3-70b", AiImportBatchProfile.Large),
		("llama3.3-70b", AiImportBatchProfile.Large),
		("mixtral-8x7b", AiImportBatchProfile.Medium),
		("mistral-small", AiImportBatchProfile.Medium),
		("deepseek-chat", AiImportBatchProfile.Medium),
	];

	public static AiImportBatchProfile Resolve(string? modelOrDeploymentId)
	{
		if (string.IsNullOrWhiteSpace(modelOrDeploymentId))
		{
			return WithOnlineId(AiImportBatchProfile.Small);
		}

		var normalized = modelOrDeploymentId.Trim();
		foreach (var (pattern, profile) in Rules)
		{
			if (normalized.Contains(pattern, StringComparison.OrdinalIgnoreCase))
			{
				return WithOnlineId(profile);
			}
		}

		return WithOnlineId(AiImportBatchProfile.Small);
	}

	private static AiImportBatchProfile WithOnlineId(AiImportBatchProfile template) =>
		template with { ProfileId = $"online:{template.ProfileId}" };
}

public static class AiImportBatchPlanResolver
{
	private static readonly IReadOnlyDictionary<string, AiImportBatchProfile> CatalogOverrides =
		new Dictionary<string, AiImportBatchProfile>(StringComparer.Ordinal)
		{
			["gemma2-2b"] = AiImportBatchProfile.Compact with { ProfileId = "gemma2-2b" },
			["phi3-mini"] = AiImportBatchProfile.Small with { ProfileId = "phi3-mini", MaxInputChars = 2000 },
			["llama32-3b"] = AiImportBatchProfile.Small with { ProfileId = "llama32-3b" },
			["qwen25-3b"] = AiImportBatchProfile.Small with { ProfileId = "qwen25-3b" },
			["mistral-7b"] = AiImportBatchProfile.Medium with { ProfileId = "mistral-7b" },
			["llama31-8b"] = AiImportBatchProfile.Medium with { ProfileId = "llama31-8b", MaxInputChars = 5500 },
			["gemma2-9b"] = AiImportBatchProfile.Medium with { ProfileId = "gemma2-9b" },
			["qwen25-7b"] = AiImportBatchProfile.Medium with { ProfileId = "qwen25-7b", MaxInputChars = 5500 },
			["mixtral-8x7b"] = AiImportBatchProfile.Large with { ProfileId = "mixtral-8x7b" },
			["llama31-70b"] = AiImportBatchProfile.ExtraLarge with { ProfileId = "llama31-70b" },
			["llama33-70b"] = AiImportBatchProfile.Large with { ProfileId = "llama33-70b" },
		};

	public static AiImportBatchProfile Resolve(AiSettingsDocument settings)
	{
		if (settings.ActiveBackend == AiBackendKind.Local)
		{
			var modelId = settings.ActiveLocalModelId ?? settings.Local?.SelectedModelId;
			if (!string.IsNullOrWhiteSpace(modelId) &&
				CatalogOverrides.TryGetValue(modelId, out var catalogProfile))
			{
				return catalogProfile;
			}

			var entry = !string.IsNullOrWhiteSpace(modelId)
				? AiModelCatalog.TryGetById(modelId)
				: null;
			if (entry is not null)
			{
				return MapTier(entry.Tier, entry.Id);
			}

			return AiImportBatchProfile.Small with { ProfileId = "local:unknown" };
		}

		if (settings.ActiveBackend == AiBackendKind.Online &&
			!string.IsNullOrWhiteSpace(settings.ActiveOnlineProviderId) &&
			settings.OnlineProviders.TryGetValue(settings.ActiveOnlineProviderId, out var config))
		{
			var modelKey = string.Equals(settings.ActiveOnlineProviderId, "azure-openai", StringComparison.Ordinal)
				? config.DeploymentName
				: config.ModelId;
			return AiImportOnlineModelProfileMap.Resolve(modelKey);
		}

		return AiImportBatchProfile.Small with { ProfileId = "online:unknown" };
	}

	public static AiImportBatchPlan BuildPlan(
		string normalizedText,
		CvSegmentationResult segmentation,
		AiImportBatchProfile profile)
	{
		var chunker = new AiImportSourceChunker(profile);
		var batches = chunker.BuildBatches(normalizedText, segmentation);
		return new AiImportBatchPlan(profile, batches);
	}

	private static AiImportBatchProfile MapTier(AiModelTier tier, string id) =>
		tier switch
		{
			AiModelTier.Compact => AiImportBatchProfile.Compact with { ProfileId = id },
			AiModelTier.Small => AiImportBatchProfile.Small with { ProfileId = id },
			AiModelTier.Medium => AiImportBatchProfile.Medium with { ProfileId = id },
			AiModelTier.Large => AiImportBatchProfile.Large with { ProfileId = id },
			AiModelTier.ExtraLarge => AiImportBatchProfile.ExtraLarge with { ProfileId = id },
			_ => AiImportBatchProfile.Small with { ProfileId = id },
		};
}
