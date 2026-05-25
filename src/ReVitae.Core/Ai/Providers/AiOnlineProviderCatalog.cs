using ReVitae.Core.Localization;

namespace ReVitae.Core.Ai.Providers;

public static class AiOnlineProviderCatalog
{
	private static readonly IReadOnlyList<AiOnlineProviderDefinition> All =
	[
		CreateOpenAiCompatible(
			"openai",
			TranslationKeys.AiProviderOpenAiName,
			TranslationKeys.AiProviderOpenAiDescription,
			"https://api.openai.com/v1",
			false,
			[("gpt-4o-mini", TranslationKeys.AiSetupProviderUseCaseQuickEdits), ("gpt-4o", TranslationKeys.AiSetupProviderUseCaseImportAssist)],
			extraFields: [Field(TranslationKeys.AiSetupProviderFieldOrganizationId, AiProviderFieldIds.OrganizationId, AiProviderFieldKind.Text, false, advanced: true)]),
		CreateAnthropic(),
		CreateGemini(),
		CreateOpenAiCompatible(
			"groq",
			TranslationKeys.AiProviderGroqName,
			TranslationKeys.AiProviderGroqDescription,
			"https://api.groq.com/openai/v1",
			true,
			[("llama-3.3-70b-versatile", TranslationKeys.AiSetupProviderUseCaseImportAssist), ("mixtral-8x7b-32768", TranslationKeys.AiSetupProviderUseCaseQuickEdits)]),
		CreateAzure(),
		CreateOpenAiCompatible(
			"mistral",
			TranslationKeys.AiProviderMistralName,
			TranslationKeys.AiProviderMistralDescription,
			"https://api.mistral.ai/v1",
			false,
			[("mistral-small-latest", TranslationKeys.AiSetupProviderUseCaseQuickEdits), ("open-mistral-nemo", TranslationKeys.AiSetupProviderUseCaseQuickEdits)]),
		CreateOpenAiCompatible(
			"deepseek",
			TranslationKeys.AiProviderDeepSeekName,
			TranslationKeys.AiProviderDeepSeekDescription,
			"https://api.deepseek.com/v1",
			false,
			[("deepseek-chat", TranslationKeys.AiSetupProviderUseCaseQuickEdits)]),
		CreateOpenAiCompatible(
			"openrouter",
			TranslationKeys.AiProviderOpenRouterName,
			TranslationKeys.AiProviderOpenRouterDescription,
			"https://openrouter.ai/api/v1",
			true,
			[("openai/gpt-4o-mini", TranslationKeys.AiSetupProviderUseCaseQuickEdits), ("anthropic/claude-3.5-sonnet", TranslationKeys.AiSetupProviderUseCaseImportAssist)]),
		CreateCustom(),
	];

	public static IReadOnlyList<AiOnlineProviderDefinition> GetAll() => All;

	public static AiOnlineProviderDefinition? TryGetById(string providerId) =>
		All.FirstOrDefault(provider => string.Equals(provider.Id, providerId, StringComparison.Ordinal));

	private static AiOnlineProviderDefinition CreateAnthropic() =>
		new(
			"anthropic",
			TranslationKeys.AiProviderAnthropicName,
			TranslationKeys.AiProviderAnthropicDescription,
			AiOnlineApiStyle.AnthropicMessages,
			"https://api.anthropic.com",
			false,
			StandardFields(includeModel: true, includeBaseUrlOverride: true),
			[
				new("claude-sonnet-4-20250514", TranslationKeys.AiSetupProviderUseCaseImportAssist),
				new("claude-3-5-haiku-20241022", TranslationKeys.AiSetupProviderUseCaseQuickEdits),
			],
			TranslationKeys.AiSetupProviderUseCaseImportAssist);

	private static AiOnlineProviderDefinition CreateGemini() =>
		new(
			"google-gemini",
			TranslationKeys.AiProviderGeminiName,
			TranslationKeys.AiProviderGeminiDescription,
			AiOnlineApiStyle.GeminiGenerateContent,
			"https://generativelanguage.googleapis.com/v1beta",
			true,
			StandardFields(includeModel: true, includeBaseUrlOverride: true),
			[
				new("gemini-2.0-flash", TranslationKeys.AiSetupProviderUseCaseQuickEdits),
				new("gemini-2.5-pro-preview-03-25", TranslationKeys.AiSetupProviderUseCaseImportAssist),
			],
			TranslationKeys.AiSetupProviderUseCaseQuickEdits);

	private static AiOnlineProviderDefinition CreateAzure() =>
		new(
			"azure-openai",
			TranslationKeys.AiProviderAzureName,
			TranslationKeys.AiProviderAzureDescription,
			AiOnlineApiStyle.OpenAiCompatible,
			null,
			false,
			[
				Field(TranslationKeys.AiSetupProviderFieldApiKey, AiProviderFieldIds.ApiKey, AiProviderFieldKind.Password, true),
				Field(TranslationKeys.AiSetupProviderFieldBaseUrl, AiProviderFieldIds.BaseUrl, AiProviderFieldKind.Url, true),
				Field(TranslationKeys.AiSetupProviderFieldDeploymentName, AiProviderFieldIds.DeploymentName, AiProviderFieldKind.Text, true),
				Field(TranslationKeys.AiSetupProviderFieldApiVersion, AiProviderFieldIds.ApiVersion, AiProviderFieldKind.Text, false),
			],
			[],
			TranslationKeys.AiSetupProviderUseCaseImportAssist);

	private static AiOnlineProviderDefinition CreateCustom() =>
		new(
			"custom-openai",
			TranslationKeys.AiProviderCustomName,
			TranslationKeys.AiProviderCustomDescription,
			AiOnlineApiStyle.OpenAiCompatible,
			null,
			false,
			[
				Field(TranslationKeys.AiSetupProviderFieldBaseUrl, AiProviderFieldIds.BaseUrl, AiProviderFieldKind.Url, true),
				Field(TranslationKeys.AiSetupProviderFieldModelId, AiProviderFieldIds.ModelId, AiProviderFieldKind.Text, true),
				Field(TranslationKeys.AiSetupProviderFieldApiKey, AiProviderFieldIds.ApiKey, AiProviderFieldKind.Password, false),
			],
			[],
			TranslationKeys.AiSetupProviderUseCaseQuickEdits);

	private static AiOnlineProviderDefinition CreateOpenAiCompatible(
		string id,
		string nameKey,
		string descriptionKey,
		string defaultBaseUrl,
		bool freeTier,
		(string ModelId, string HintKey)[] models,
		AiProviderFieldDefinition[]? extraFields = null)
	{
		var fields = StandardFields(includeModel: true, includeBaseUrlOverride: true).ToList();
		if (extraFields is not null)
		{
			fields.AddRange(extraFields);
		}

		return new AiOnlineProviderDefinition(
			id,
			nameKey,
			descriptionKey,
			AiOnlineApiStyle.OpenAiCompatible,
			defaultBaseUrl,
			freeTier,
			fields,
			models.Select(model => new AiProviderModelOption(model.ModelId, model.HintKey)).ToArray(),
			models.FirstOrDefault().HintKey);
	}

	private static IReadOnlyList<AiProviderFieldDefinition> StandardFields(
		bool includeModel,
		bool includeBaseUrlOverride)
	{
		var fields = new List<AiProviderFieldDefinition>
		{
			Field(TranslationKeys.AiSetupProviderFieldApiKey, AiProviderFieldIds.ApiKey, AiProviderFieldKind.Password, true),
		};

		if (includeModel)
		{
			fields.Add(Field(TranslationKeys.AiSetupProviderFieldModelId, AiProviderFieldIds.ModelId, AiProviderFieldKind.ModelSelect, true));
		}

		if (includeBaseUrlOverride)
		{
			fields.Add(Field(TranslationKeys.AiSetupProviderFieldBaseUrl, AiProviderFieldIds.BaseUrl, AiProviderFieldKind.Url, false, advanced: true));
		}

		return fields;
	}

	private static AiProviderFieldDefinition Field(
		string labelKey,
		string id,
		AiProviderFieldKind kind,
		bool required,
		string? placeholderKey = null,
		bool advanced = false) =>
		new(id, labelKey, kind, required, placeholderKey, advanced);
}
