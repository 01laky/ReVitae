using System.Net;
using System.Text;
using ReVitae.Core.Ai.Providers;
using ReVitae.Core.Ai.Providers.Chat;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Ai.Providers;

public sealed class AiProviderConnectionTesterTests
{
	[Fact]
	public async Task TestAsync_OpenAiSuccess_ReturnsSucceeded()
	{
		var handler = new StubHttpMessageHandler(request =>
		{
			Assert.Contains("/chat/completions", request.RequestUri!.AbsoluteUri, StringComparison.Ordinal);
			return JsonResponse(HttpStatusCode.OK, OpenAiSuccessBody);
		});
		var tester = CreateTester(handler);
		var provider = AiOnlineProviderCatalog.TryGetById("openai")!;
		var draft = Draft("openai", "gpt-4o-mini", "https://api.openai.com/v1", "sk-test");

		var result = await tester.TestAsync(provider, draft);

		Assert.True(result.Succeeded);
		Assert.Equal(AiProviderConnectionErrorKind.None, result.ErrorKind);
	}

	[Fact]
	public async Task TestAsync_OpenAi401_ReturnsInvalidKey()
	{
		var handler = new StubHttpMessageHandler(_ => JsonResponse(HttpStatusCode.Unauthorized, "{}"));
		var tester = CreateTester(handler);
		var provider = AiOnlineProviderCatalog.TryGetById("openai")!;
		var draft = Draft("openai", "gpt-4o-mini", "https://api.openai.com/v1", "bad-key");

		var result = await tester.TestAsync(provider, draft);

		Assert.False(result.Succeeded);
		Assert.Equal(AiProviderConnectionErrorKind.InvalidKey, result.ErrorKind);
		Assert.Equal(TranslationKeys.AiSetupProviderInvalidKey, result.ErrorMessageKey);
	}

	[Fact]
	public async Task TestAsync_AnthropicSuccess_UsesMessagesEndpoint()
	{
		var handler = new StubHttpMessageHandler(request =>
		{
			Assert.Contains("/v1/messages", request.RequestUri!.AbsoluteUri, StringComparison.Ordinal);
			Assert.True(request.Headers.Contains("x-api-key"));
			return JsonResponse(HttpStatusCode.OK, AnthropicSuccessBody);
		});
		var tester = CreateTester(handler);
		var provider = AiOnlineProviderCatalog.TryGetById("anthropic")!;
		var draft = Draft("anthropic", "claude-3-5-haiku-20241022", "https://api.anthropic.com", "sk-ant");

		var result = await tester.TestAsync(provider, draft);

		Assert.True(result.Succeeded);
	}

	[Fact]
	public async Task TestAsync_GeminiSuccess_UsesGenerateContentEndpoint()
	{
		var handler = new StubHttpMessageHandler(request =>
		{
			Assert.Contains(":generateContent", request.RequestUri!.AbsoluteUri, StringComparison.Ordinal);
			Assert.Contains("key=gemini-key", request.RequestUri.Query, StringComparison.Ordinal);
			return JsonResponse(HttpStatusCode.OK, GeminiSuccessBody);
		});
		var tester = CreateTester(handler);
		var provider = AiOnlineProviderCatalog.TryGetById("google-gemini")!;
		var draft = Draft("google-gemini", "gemini-2.0-flash", "https://generativelanguage.googleapis.com/v1beta", "gemini-key");

		var result = await tester.TestAsync(provider, draft);

		Assert.True(result.Succeeded);
	}

	[Fact]
	public async Task TestAsync_AzureUsesDeploymentUrlShape()
	{
		HttpRequestMessage? captured = null;
		var handler = new StubHttpMessageHandler(request =>
		{
			captured = request;
			return JsonResponse(HttpStatusCode.OK, OpenAiSuccessBody);
		});
		var tester = CreateTester(handler);
		var provider = AiOnlineProviderCatalog.TryGetById("azure-openai")!;
		var draft = new AiProviderConnectionDraft(
			"azure-openai",
			new Dictionary<string, string?>(StringComparer.Ordinal)
			{
				[AiProviderFieldIds.BaseUrl] = "https://example.openai.azure.com",
				[AiProviderFieldIds.DeploymentName] = "gpt-4o",
				[AiProviderFieldIds.ApiVersion] = "2024-08-01-preview",
			},
			"azure-key");

		var result = await tester.TestAsync(provider, draft);

		Assert.True(result.Succeeded);
		Assert.NotNull(captured);
		Assert.Contains("/openai/deployments/gpt-4o/chat/completions", captured!.RequestUri!.AbsoluteUri, StringComparison.Ordinal);
		Assert.Contains("api-version=2024-08-01-preview", captured.RequestUri.Query, StringComparison.Ordinal);
		Assert.True(captured.Headers.Contains("api-key"));
	}

	[Fact]
	public async Task TestAsync_InvalidConfiguration_ReturnsWithoutHttpCall()
	{
		var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("Should not call HTTP"));
		var tester = CreateTester(handler);
		var provider = AiOnlineProviderCatalog.TryGetById("openai")!;
		var draft = Draft("openai", modelId: null, baseUrl: "https://api.openai.com/v1", apiKey: "sk-test");

		var result = await tester.TestAsync(provider, draft);

		Assert.False(result.Succeeded);
		Assert.Equal(AiProviderConnectionErrorKind.InvalidConfiguration, result.ErrorKind);
	}

	[Fact]
	public async Task TestAsync_RateLimited_MapsErrorKind()
	{
		var handler = new StubHttpMessageHandler(_ => JsonResponse(HttpStatusCode.TooManyRequests, "{}"));
		var tester = CreateTester(handler);
		var provider = AiOnlineProviderCatalog.TryGetById("openai")!;
		var draft = Draft("openai", "gpt-4o-mini", "https://api.openai.com/v1", "sk-test");

		var result = await tester.TestAsync(provider, draft);

		Assert.False(result.Succeeded);
		Assert.Equal(AiProviderConnectionErrorKind.RateLimited, result.ErrorKind);
		Assert.Equal(TranslationKeys.AiSetupProviderRateLimited, result.ErrorMessageKey);
	}

	private static AiProviderConnectionTester CreateTester(HttpMessageHandler handler)
	{
		var httpClient = new HttpClient(handler);
		var factory = new ChatCompletionClientFactory(httpClient);
		return new AiProviderConnectionTester(factory);
	}

	private static AiProviderConnectionDraft Draft(
		string providerId,
		string? modelId,
		string baseUrl,
		string? apiKey) =>
		new(
			providerId,
			new Dictionary<string, string?>(StringComparer.Ordinal)
			{
				[AiProviderFieldIds.ModelId] = modelId,
				[AiProviderFieldIds.BaseUrl] = baseUrl,
			},
			apiKey);

	private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string body) =>
		new(statusCode)
		{
			Content = new StringContent(body, Encoding.UTF8, "application/json"),
		};

	private const string OpenAiSuccessBody =
		"""
        {"choices":[{"message":{"content":"OK"}}]}
        """;

	private const string AnthropicSuccessBody =
		"""
        {"content":[{"text":"OK"}]}
        """;

	private const string GeminiSuccessBody =
		"""
        {"candidates":[{"content":{"parts":[{"text":"OK"}]}}]}
        """;

	private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
		: HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken) =>
			Task.FromResult(handler(request));
	}
}
