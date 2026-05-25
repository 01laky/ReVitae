using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ReVitae.Core.Ai.Cv;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Ai.Providers.Chat;

public sealed class ChatCompletionClientFactory
{
	private readonly HttpClient _httpClient;

	public ChatCompletionClientFactory(HttpClient? httpClient = null)
	{
		_httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
	}

	public IChatCompletionClient Create(AiOnlineApiStyle apiStyle) =>
		apiStyle switch
		{
			AiOnlineApiStyle.AnthropicMessages => new AnthropicMessagesChatClient(_httpClient),
			AiOnlineApiStyle.GeminiGenerateContent => new GeminiGenerateContentChatClient(_httpClient),
			_ => new OpenAiCompatibleChatClient(_httpClient),
		};
}

public sealed class OpenAiCompatibleChatClient(HttpClient httpClient) : IChatCompletionClient
{
	public Task<AiChatCompletionResult> CompleteAsync(
		AiOnlineProviderDefinition provider,
		AiProviderConnectionDraft draft,
		string prompt,
		CancellationToken cancellationToken = default) =>
		CompleteWithMessagesAsync(
			provider,
			draft,
			new AiCvPromptMessages(string.Empty, prompt),
			maxTokens: 8,
			cancellationToken);

	public async Task<AiChatCompletionResult> CompleteWithMessagesAsync(
		AiOnlineProviderDefinition provider,
		AiProviderConnectionDraft draft,
		AiCvPromptMessages messages,
		int maxTokens = 512,
		CancellationToken cancellationToken = default)
	{
		var baseUrl = AiProviderConfigValidator.NormalizeBaseUrl(provider, draft);
		if (string.IsNullOrWhiteSpace(baseUrl))
		{
			return Fail(TranslationKeys.AiSetupProviderModelNotFound);
		}

		var modelId = AiProviderConfigValidator.ResolveModelId(provider, draft);
		if (string.IsNullOrWhiteSpace(modelId))
		{
			return Fail(TranslationKeys.AiSetupProviderModelNotFound);
		}

		var url = provider.Id == "azure-openai"
			? $"{baseUrl}/openai/deployments/{modelId}/chat/completions?api-version={GetApiVersion(draft)}"
			: $"{baseUrl}/chat/completions";

		using var request = new HttpRequestMessage(HttpMethod.Post, url);
		request.Headers.Authorization = CreateAuthorization(provider, draft);
		if (provider.Id == "azure-openai")
		{
			request.Headers.Remove("Authorization");
			if (!string.IsNullOrWhiteSpace(draft.ApiKey))
			{
				request.Headers.Add("api-key", draft.ApiKey);
			}
		}

		if (provider.Id == "openai" &&
			draft.Values.TryGetValue(AiProviderFieldIds.OrganizationId, out var org) &&
			!string.IsNullOrWhiteSpace(org))
		{
			request.Headers.Add("OpenAI-Organization", org);
		}

		request.Content = new StringContent(
			JsonSerializer.Serialize(new
			{
				model = provider.Id == "azure-openai" ? (string?)null : modelId,
				messages = BuildOpenAiMessages(messages),
				max_tokens = maxTokens,
			}),
			Encoding.UTF8,
			"application/json");

		return await SendAsync(httpClient, request, cancellationToken).ConfigureAwait(false);
	}

	internal static object[] BuildOpenAiMessages(AiCvPromptMessages messages)
	{
		var result = new List<object>();
		if (!string.IsNullOrWhiteSpace(messages.SystemPrompt))
		{
			result.Add(new { role = "system", content = messages.SystemPrompt });
		}

		result.Add(new { role = "user", content = messages.UserPrompt });
		return result.ToArray();
	}

	private static AuthenticationHeaderValue? CreateAuthorization(
		AiOnlineProviderDefinition provider,
		AiProviderConnectionDraft draft)
	{
		if (provider.Id == "azure-openai" || string.IsNullOrWhiteSpace(draft.ApiKey))
		{
			return null;
		}

		return new AuthenticationHeaderValue("Bearer", draft.ApiKey);
	}

	private static string GetApiVersion(AiProviderConnectionDraft draft) =>
		draft.Values.TryGetValue(AiProviderFieldIds.ApiVersion, out var version) &&
		!string.IsNullOrWhiteSpace(version)
			? version
			: "2024-08-01-preview";

	internal static async Task<AiChatCompletionResult> SendAsync(
		HttpClient httpClient,
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		try
		{
			using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
			var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
			if (!response.IsSuccessStatusCode)
			{
				return MapHttpFailure((int)response.StatusCode, body);
			}

			using var document = JsonDocument.Parse(body);
			var content = document.RootElement
				.GetProperty("choices")[0]
				.GetProperty("message")
				.GetProperty("content")
				.GetString();

			return new AiChatCompletionResult(true, content, null);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception)
		{
			return Fail(TranslationKeys.AiSetupProviderTestFailed);
		}
	}

	internal static AiChatCompletionResult MapHttpFailure(int statusCode, string body)
	{
		var key = statusCode switch
		{
			401 or 403 => TranslationKeys.AiSetupProviderInvalidKey,
			404 => TranslationKeys.AiSetupProviderModelNotFound,
			429 => TranslationKeys.AiSetupProviderRateLimited,
			>= 500 => TranslationKeys.AiSetupProviderUnavailable,
			_ => TranslationKeys.AiSetupProviderTestFailed,
		};

		return Fail(key);
	}

	private static AiChatCompletionResult Fail(string key) =>
		new(false, null, key);
}

public sealed class AnthropicMessagesChatClient(HttpClient httpClient) : IChatCompletionClient
{
	public Task<AiChatCompletionResult> CompleteAsync(
		AiOnlineProviderDefinition provider,
		AiProviderConnectionDraft draft,
		string prompt,
		CancellationToken cancellationToken = default) =>
		CompleteWithMessagesAsync(
			provider,
			draft,
			new AiCvPromptMessages(string.Empty, prompt),
			maxTokens: 8,
			cancellationToken);

	public async Task<AiChatCompletionResult> CompleteWithMessagesAsync(
		AiOnlineProviderDefinition provider,
		AiProviderConnectionDraft draft,
		AiCvPromptMessages messages,
		int maxTokens = 512,
		CancellationToken cancellationToken = default)
	{
		var baseUrl = AiProviderConfigValidator.NormalizeBaseUrl(provider, draft) ?? "https://api.anthropic.com";
		var modelId = AiProviderConfigValidator.ResolveModelId(provider, draft);
		if (string.IsNullOrWhiteSpace(modelId) || string.IsNullOrWhiteSpace(draft.ApiKey))
		{
			return new AiChatCompletionResult(false, null, TranslationKeys.AiSetupProviderInvalidKey);
		}

		using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1/messages");
		request.Headers.Add("x-api-key", draft.ApiKey);
		request.Headers.Add("anthropic-version", "2023-06-01");

		var payload = new Dictionary<string, object>(StringComparer.Ordinal)
		{
			["model"] = modelId,
			["max_tokens"] = maxTokens,
			["messages"] = new[] { new { role = "user", content = messages.UserPrompt } },
		};

		if (!string.IsNullOrWhiteSpace(messages.SystemPrompt))
		{
			payload["system"] = messages.SystemPrompt;
		}

		request.Content = new StringContent(
			JsonSerializer.Serialize(payload),
			Encoding.UTF8,
			"application/json");

		try
		{
			using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
			if (!response.IsSuccessStatusCode)
			{
				var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
				return OpenAiCompatibleChatClient.MapHttpFailure((int)response.StatusCode, body);
			}

			var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
			using var document = JsonDocument.Parse(json);
			var content = document.RootElement.GetProperty("content")[0].GetProperty("text").GetString();
			return new AiChatCompletionResult(true, content, null);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception)
		{
			return new AiChatCompletionResult(false, null, TranslationKeys.AiSetupProviderTestFailed);
		}
	}
}

public sealed class GeminiGenerateContentChatClient(HttpClient httpClient) : IChatCompletionClient
{
	public Task<AiChatCompletionResult> CompleteAsync(
		AiOnlineProviderDefinition provider,
		AiProviderConnectionDraft draft,
		string prompt,
		CancellationToken cancellationToken = default) =>
		CompleteWithMessagesAsync(
			provider,
			draft,
			new AiCvPromptMessages(string.Empty, prompt),
			maxTokens: 8,
			cancellationToken);

	public async Task<AiChatCompletionResult> CompleteWithMessagesAsync(
		AiOnlineProviderDefinition provider,
		AiProviderConnectionDraft draft,
		AiCvPromptMessages messages,
		int maxTokens = 512,
		CancellationToken cancellationToken = default)
	{
		var baseUrl = AiProviderConfigValidator.NormalizeBaseUrl(provider, draft);
		var modelId = AiProviderConfigValidator.ResolveModelId(provider, draft);
		if (string.IsNullOrWhiteSpace(baseUrl) ||
			string.IsNullOrWhiteSpace(modelId) ||
			string.IsNullOrWhiteSpace(draft.ApiKey))
		{
			return new AiChatCompletionResult(false, null, TranslationKeys.AiSetupProviderInvalidKey);
		}

		var prompt = CombineGeminiPrompt(messages);
		var url =
			$"{baseUrl}/models/{modelId}:generateContent?key={Uri.EscapeDataString(draft.ApiKey)}";
		using var request = new HttpRequestMessage(HttpMethod.Post, url);
		request.Content = new StringContent(
			JsonSerializer.Serialize(new
			{
				contents = new[]
				{
					new
					{
						parts = new[] { new { text = prompt } },
					},
				},
				generationConfig = new { maxOutputTokens = maxTokens },
			}),
			Encoding.UTF8,
			"application/json");

		try
		{
			using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
			if (!response.IsSuccessStatusCode)
			{
				var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
				return OpenAiCompatibleChatClient.MapHttpFailure((int)response.StatusCode, body);
			}

			var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
			using var document = JsonDocument.Parse(json);
			var content = document.RootElement
				.GetProperty("candidates")[0]
				.GetProperty("content")
				.GetProperty("parts")[0]
				.GetProperty("text")
				.GetString();
			return new AiChatCompletionResult(true, content, null);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception)
		{
			return new AiChatCompletionResult(false, null, TranslationKeys.AiSetupProviderTestFailed);
		}
	}

	internal static string CombineGeminiPrompt(AiCvPromptMessages messages)
	{
		if (string.IsNullOrWhiteSpace(messages.SystemPrompt))
		{
			return messages.UserPrompt;
		}

		return $"{messages.SystemPrompt}{Environment.NewLine}{Environment.NewLine}{messages.UserPrompt}";
	}
}
