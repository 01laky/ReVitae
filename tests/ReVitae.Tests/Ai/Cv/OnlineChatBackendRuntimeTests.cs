using System.Net;
using System.Text;
using ReVitae.Core.Ai.Cv;
using ReVitae.Core.Ai.Providers;
using ReVitae.Core.Ai.Providers.Chat;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Ai.Cv;

public sealed class OnlineChatBackendRuntimeTests
{
    [Fact]
    public async Task CompleteAsync_OpenAi_UsesMessagesEndpoint()
    {
        string? capturedBody = null;
        var handler = new StubHandler(async request =>
        {
            capturedBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync().ConfigureAwait(false);
            return Json(HttpStatusCode.OK, """{"choices":[{"message":{"content":"Summary text."}}]}""");
        });

        var provider = AiOnlineProviderCatalog.TryGetById("openai")!;
        var draft = new AiProviderConnectionDraft(
            "openai",
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                [AiProviderFieldIds.ModelId] = "gpt-4o-mini",
                [AiProviderFieldIds.BaseUrl] = "https://api.openai.com/v1",
            },
            "sk-test");

        var runtime = new OnlineChatBackendRuntime(
            provider,
            draft,
            new ChatCompletionClientFactory(new HttpClient(handler)));

        var messages = new AiCvPromptMessages("You are helpful.", "Improve this summary.");
        var result = await runtime.CompleteAsync(messages);

        Assert.True(result.Succeeded);
        Assert.Equal("Summary text.", result.Content);
        Assert.NotNull(capturedBody);
        Assert.Contains("system", capturedBody!, StringComparison.Ordinal);
        Assert.Contains("You are helpful.", capturedBody!, StringComparison.Ordinal);
    }

    [Fact]
    public void DescribeActiveBackend_FormatsOnlineLabel()
    {
        var provider = AiOnlineProviderCatalog.TryGetById("openai")!;
        var draft = new AiProviderConnectionDraft("openai", new Dictionary<string, string?>(), "key");
        var runtime = new OnlineChatBackendRuntime(
            provider,
            draft,
            new ChatCompletionClientFactory(new HttpClient(new StubHandler(_ =>
                Task.FromResult(Json(HttpStatusCode.OK, "{}"))))));
        var localizer = AppLocalizer.FromSystemCulture();

        var label = runtime.DescribeActiveBackend(localizer);

        Assert.Contains("OpenAI", label, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GeminiCombinePrompt_MergesSystemAndUser()
    {
        var combined = GeminiGenerateContentChatClient.CombineGeminiPrompt(
            new AiCvPromptMessages("System rules.", "User task."));

        Assert.Contains("System rules.", combined, StringComparison.Ordinal);
        Assert.Contains("User task.", combined, StringComparison.Ordinal);
    }

    [Fact]
    public void OpenAiBuildMessages_IncludesSystemWhenPresent()
    {
        var payload = OpenAiCompatibleChatClient.BuildOpenAiMessages(
            new AiCvPromptMessages("System", "User"));

        Assert.Equal(2, payload.Length);
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
