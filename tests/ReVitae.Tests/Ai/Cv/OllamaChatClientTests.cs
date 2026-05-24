using System.Net;
using System.Text;
using ReVitae.Core.Ai.Cv;
using ReVitae.Core.Ai.Ollama;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Ai.Cv;

public sealed class OllamaChatClientTests
{
    [Fact]
    public async Task ChatAsync_SendsModelTagAndMessages()
    {
        string? capturedBody = null;
        var handler = new StubHandler(async request =>
        {
            capturedBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync().ConfigureAwait(false);
            return Json(HttpStatusCode.OK, """{"message":{"content":"Better description."}}""");
        });
        var client = new OllamaChatClient(new HttpClient(handler) { BaseAddress = OllamaHost.BaseUri });
        var messages = new AiCvPromptMessages("system", "user");

        var result = await client.ChatAsync("gemma2:2b", messages);

        Assert.True(result.Succeeded);
        Assert.Equal("Better description.", result.Content);
        Assert.NotNull(capturedBody);
        Assert.Contains("gemma2:2b", capturedBody!, StringComparison.Ordinal);
        Assert.Contains("system", capturedBody!, StringComparison.Ordinal);
        Assert.Contains("user", capturedBody!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ChatAsync_ConnectionRefused_ReturnsUnavailable()
    {
        var handler = new StubHandler(_ => Task.FromException<HttpResponseMessage>(
            new HttpRequestException("Connection refused")));
        var client = new OllamaChatClient(new HttpClient(handler) { BaseAddress = OllamaHost.BaseUri });

        var result = await client.ChatAsync(
            "gemma2:2b",
            new AiCvPromptMessages("system", "user"));

        Assert.False(result.Succeeded);
        Assert.Equal(TranslationKeys.AiCvOllamaUnavailable, result.ErrorMessage);
    }

    [Fact]
    public async Task ChatAsync_EmptyContent_ReturnsEmptyResponseKey()
    {
        var handler = new StubHandler(_ =>
            Task.FromResult(Json(HttpStatusCode.OK, """{"message":{"content":""}}""")));
        var client = new OllamaChatClient(new HttpClient(handler) { BaseAddress = OllamaHost.BaseUri });

        var result = await client.ChatAsync(
            "gemma2:2b",
            new AiCvPromptMessages("system", "user"));

        Assert.False(result.Succeeded);
        Assert.Equal(TranslationKeys.AiCvEmptyResponse, result.ErrorMessage);
    }

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
