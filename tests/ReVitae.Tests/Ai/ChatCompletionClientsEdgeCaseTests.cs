using System.Net;
using System.Text;
using ReVitae.Core.Ai.Providers;
using ReVitae.Core.Ai.Providers.Chat;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Ai;

public sealed class ChatCompletionClientsEdgeCaseTests
{
	[Theory]
	[InlineData(HttpStatusCode.Unauthorized, TranslationKeys.AiSetupProviderInvalidKey)]
	[InlineData(HttpStatusCode.Forbidden, TranslationKeys.AiSetupProviderInvalidKey)]
	[InlineData(HttpStatusCode.NotFound, TranslationKeys.AiSetupProviderModelNotFound)]
	[InlineData(HttpStatusCode.TooManyRequests, TranslationKeys.AiSetupProviderRateLimited)]
	[InlineData(HttpStatusCode.InternalServerError, TranslationKeys.AiSetupProviderUnavailable)]
	public async Task OpenAiClient_MapsHttpFailures(HttpStatusCode statusCode, string expectedKey)
	{
		var client = CreateOpenAiClient(_ => Task.FromResult(Json(statusCode, "{}")));
		var provider = AiOnlineProviderCatalog.TryGetById("openai")!;
		var draft = CreateDraft("gpt-4o-mini", "https://api.openai.com/v1", "sk-test");

		var result = await client.CompleteAsync(provider, draft, "ping");

		Assert.False(result.Succeeded);
		Assert.Equal(expectedKey, result.ErrorMessage);
	}

	[Fact]
	public async Task OpenAiClient_MalformedJson_ReturnsTestFailed()
	{
		var client = CreateOpenAiClient(_ => Task.FromResult(Json(HttpStatusCode.OK, "{ not-json")));
		var provider = AiOnlineProviderCatalog.TryGetById("openai")!;
		var draft = CreateDraft("gpt-4o-mini", "https://api.openai.com/v1", "sk-test");

		var result = await client.CompleteAsync(provider, draft, "ping");

		Assert.False(result.Succeeded);
		Assert.Equal(TranslationKeys.AiSetupProviderTestFailed, result.ErrorMessage);
	}

	[Fact]
	public async Task OpenAiClient_MissingModel_ReturnsModelNotFound()
	{
		var client = CreateOpenAiClient(_ => Task.FromResult(Json(HttpStatusCode.OK, "{}")));
		var provider = AiOnlineProviderCatalog.TryGetById("openai")!;
		var draft = new AiProviderConnectionDraft("openai", new Dictionary<string, string?>(), "sk-test");

		var result = await client.CompleteAsync(provider, draft, "ping");

		Assert.Equal(TranslationKeys.AiSetupProviderModelNotFound, result.ErrorMessage);
	}

	[Fact]
	public async Task OpenAiClient_MissingBaseUrl_ReturnsModelNotFound()
	{
		var client = CreateOpenAiClient(_ => Task.FromResult(Json(HttpStatusCode.OK, "{}")));
		var provider = AiOnlineProviderCatalog.TryGetById("custom-openai")!;
		var draft = new AiProviderConnectionDraft(
			"custom-openai",
			new Dictionary<string, string?>(StringComparer.Ordinal)
			{
				[AiProviderFieldIds.ModelId] = "local-model",
			},
			"sk-test");

		var result = await client.CompleteAsync(provider, draft, "ping");

		Assert.Equal(TranslationKeys.AiSetupProviderModelNotFound, result.ErrorMessage);
	}

	[Fact]
	public async Task OpenAiClient_Success_ParsesContent()
	{
		var client = CreateOpenAiClient(_ =>
			Task.FromResult(Json(HttpStatusCode.OK, """{"choices":[{"message":{"content":"ok"}}]}""")));
		var provider = AiOnlineProviderCatalog.TryGetById("openai")!;
		var draft = CreateDraft("gpt-4o-mini", "https://api.openai.com/v1", "sk-test");

		var result = await client.CompleteAsync(provider, draft, "ping");

		Assert.True(result.Succeeded);
		Assert.Equal("ok", result.Content);
	}

	[Fact]
	public void MapHttpFailure_UnknownStatus_ReturnsTestFailed()
	{
		var mapped = OpenAiCompatibleChatClient.MapHttpFailure(418, string.Empty);

		Assert.False(mapped.Succeeded);
		Assert.Equal(TranslationKeys.AiSetupProviderTestFailed, mapped.ErrorMessage);
	}

	[Fact]
	public async Task AnthropicClient_MissingApiKey_ReturnsInvalidKey()
	{
		var client = new AnthropicMessagesChatClient(new HttpClient(new StubHandler(_ =>
			Task.FromResult(Json(HttpStatusCode.OK, "{}")))));
		var provider = AiOnlineProviderCatalog.TryGetById("anthropic")!;
		var draft = new AiProviderConnectionDraft(
			"anthropic",
			new Dictionary<string, string?>(StringComparer.Ordinal)
			{
				[AiProviderFieldIds.ModelId] = "claude-3-5-haiku-20241022",
			},
			string.Empty);

		var result = await client.CompleteAsync(provider, draft, "ping");

		Assert.Equal(TranslationKeys.AiSetupProviderInvalidKey, result.ErrorMessage);
	}

	[Fact]
	public void Factory_Create_ReturnsExpectedClientType()
	{
		var factory = new ChatCompletionClientFactory(new HttpClient());

		Assert.IsType<AnthropicMessagesChatClient>(factory.Create(AiOnlineApiStyle.AnthropicMessages));
		Assert.IsType<GeminiGenerateContentChatClient>(factory.Create(AiOnlineApiStyle.GeminiGenerateContent));
		Assert.IsType<OpenAiCompatibleChatClient>(factory.Create(AiOnlineApiStyle.OpenAiCompatible));
	}

	private static OpenAiCompatibleChatClient CreateOpenAiClient(
		Func<HttpRequestMessage, Task<HttpResponseMessage>> responder) =>
		new(new HttpClient(new StubHandler(responder)));

	private static AiProviderConnectionDraft CreateDraft(string modelId, string baseUrl, string apiKey) =>
		new(
			"openai",
			new Dictionary<string, string?>(StringComparer.Ordinal)
			{
				[AiProviderFieldIds.ModelId] = modelId,
				[AiProviderFieldIds.BaseUrl] = baseUrl,
			},
			apiKey);

	private static HttpResponseMessage Json(HttpStatusCode statusCode, string body) =>
		new(statusCode)
		{
			Content = new StringContent(body, Encoding.UTF8, "application/json"),
		};

	private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken) =>
			handler(request);
	}
}
