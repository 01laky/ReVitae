using System.Net;
using ReVitae.Core.Ai.Providers;
using ReVitae.Core.Ai.Providers.Chat;
using ReVitae.Core.Localization;
using ReVitae.Tests.Infrastructure;

namespace ReVitae.Tests.Ai;

/// <summary>
/// Prompt 049 B5 — AI provider network faults. The online chat clients already accept an
/// injectable <c>HttpClient</c>, so faults are driven through <see cref="FakeHttpMessageHandler"/>
/// with no production change. Timeouts and transport errors degrade to a typed failure (never a
/// hang or crash); cancellation propagates promptly; secrets never leak into results.
/// </summary>
public sealed class AiProviderNetworkFaultTests
{
	private const string SecretKey = "sk-secret-LEAK-do-not-surface-12345";

	private static OpenAiCompatibleChatClient OpenAi(FakeHttpMessageHandler handler) =>
		new(FakeHttpMessageHandler.Client(handler));

	private static (AiOnlineProviderDefinition Provider, AiProviderConnectionDraft Draft) OpenAiConfig() =>
		(AiOnlineProviderCatalog.TryGetById("openai")!,
			new AiProviderConnectionDraft(
				"openai",
				new Dictionary<string, string?>(StringComparer.Ordinal)
				{
					[AiProviderFieldIds.ModelId] = "gpt-4o-mini",
					[AiProviderFieldIds.BaseUrl] = "https://api.openai.com/v1",
				},
				SecretKey));

	[Fact]
	public async Task Timeout_DegradesToTypedFailure_NotHangOrThrow()
	{
		var client = OpenAi(FakeHttpMessageHandler.Throws(new TaskCanceledException("timeout")));
		var (provider, draft) = OpenAiConfig();

		var result = await client.CompleteAsync(provider, draft, "ping");

		Assert.False(result.Succeeded);
		Assert.Equal(TranslationKeys.AiSetupProviderTestFailed, result.ErrorMessage);
	}

	[Theory]
	[InlineData("Connection reset by peer")]
	[InlineData("Name or service not known")]
	[InlineData("The SSL connection could not be established")]
	public async Task TransportError_DegradesToTypedFailure(string message)
	{
		var client = OpenAi(FakeHttpMessageHandler.Throws(new HttpRequestException(message)));
		var (provider, draft) = OpenAiConfig();

		var result = await client.CompleteAsync(provider, draft, "ping");

		Assert.False(result.Succeeded);
		Assert.Equal(TranslationKeys.AiSetupProviderTestFailed, result.ErrorMessage);
	}

	[Fact]
	public async Task Cancellation_PropagatesPromptly()
	{
		var client = OpenAi(FakeHttpMessageHandler.HonorsCancellation());
		var (provider, draft) = OpenAiConfig();
		using var cts = new CancellationTokenSource();
		cts.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(
			() => client.CompleteAsync(provider, draft, "ping", cts.Token));
	}

	[Fact]
	public async Task RateLimited_WithRetryAfterHeader_MapsToRateLimited()
	{
		var client = OpenAi(FakeHttpMessageHandler.Responds(
			HttpStatusCode.TooManyRequests, "{}", ("Retry-After", "30")));
		var (provider, draft) = OpenAiConfig();

		var result = await client.CompleteAsync(provider, draft, "ping");

		Assert.False(result.Succeeded);
		Assert.Equal(TranslationKeys.AiSetupProviderRateLimited, result.ErrorMessage);
	}

	[Theory]
	[InlineData(HttpStatusCode.BadGateway)]
	[InlineData(HttpStatusCode.ServiceUnavailable)]
	[InlineData(HttpStatusCode.GatewayTimeout)]
	public async Task ServerErrors_MapToUnavailable(HttpStatusCode statusCode)
	{
		var client = OpenAi(FakeHttpMessageHandler.Responds(statusCode, "upstream down"));
		var (provider, draft) = OpenAiConfig();

		var result = await client.CompleteAsync(provider, draft, "ping");

		Assert.False(result.Succeeded);
		Assert.Equal(TranslationKeys.AiSetupProviderUnavailable, result.ErrorMessage);
	}

	[Theory]
	[InlineData("{\"choices\":[{\"message\":{\"content\":")]
	[InlineData("{ partial stream cut off")]
	[InlineData("")]
	public async Task PartialOrMalformedBody_DegradesToTypedFailure(string body)
	{
		var client = OpenAi(FakeHttpMessageHandler.Responds(HttpStatusCode.OK, body));
		var (provider, draft) = OpenAiConfig();

		var result = await client.CompleteAsync(provider, draft, "ping");

		Assert.False(result.Succeeded);
		Assert.Equal(TranslationKeys.AiSetupProviderTestFailed, result.ErrorMessage);
	}

	[Theory]
	[InlineData(HttpStatusCode.Unauthorized)]
	[InlineData(HttpStatusCode.InternalServerError)]
	[InlineData(HttpStatusCode.TooManyRequests)]
	public async Task SecretApiKey_NeverLeaksIntoResult(HttpStatusCode statusCode)
	{
		var client = OpenAi(FakeHttpMessageHandler.Responds(statusCode, $"error referencing {SecretKey}"));
		var (provider, draft) = OpenAiConfig();

		var result = await client.CompleteAsync(provider, draft, "ping");

		Assert.DoesNotContain(SecretKey, result.ErrorMessage ?? string.Empty, StringComparison.Ordinal);
		Assert.DoesNotContain(SecretKey, result.Content ?? string.Empty, StringComparison.Ordinal);
	}

	[Fact]
	public async Task AnthropicClient_TransportError_DegradesToTypedFailure()
	{
		var client = new AnthropicMessagesChatClient(
			FakeHttpMessageHandler.Client(FakeHttpMessageHandler.Throws(new HttpRequestException("reset"))));
		var provider = AiOnlineProviderCatalog.TryGetById("anthropic")!;
		var draft = new AiProviderConnectionDraft(
			"anthropic",
			new Dictionary<string, string?>(StringComparer.Ordinal)
			{
				[AiProviderFieldIds.ModelId] = "claude-3-5-haiku-20241022",
			},
			SecretKey);

		var result = await client.CompleteAsync(provider, draft, "ping");

		Assert.False(result.Succeeded);
	}
}
