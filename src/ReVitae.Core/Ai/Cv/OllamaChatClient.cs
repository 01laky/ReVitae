using System.Net.Http;
using System.Text;
using System.Text.Json;
using ReVitae.Core.Ai.Ollama;
using ReVitae.Core.Ai.Providers;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Ai.Cv;

public interface IOllamaChatClient
{
    Task<AiChatCompletionResult> ChatAsync(
        string modelTag,
        AiCvPromptMessages messages,
        CancellationToken cancellationToken = default);
}

public sealed class OllamaChatClient : IOllamaChatClient
{
    private readonly HttpClient _httpClient;

    public OllamaChatClient(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
    }

    public async Task<AiChatCompletionResult> ChatAsync(
        string modelTag,
        AiCvPromptMessages messages,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelTag))
        {
            return Fail(TranslationKeys.AiSetupProviderModelNotFound);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, OllamaHost.ChatUri);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                model = modelTag,
                messages = new object[]
                {
                    new { role = "system", content = messages.SystemPrompt },
                    new { role = "user", content = messages.UserPrompt },
                },
                stream = false,
                options = new { num_predict = 512, temperature = 0.4 },
            }),
            Encoding.UTF8,
            "application/json");

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return Fail(TranslationKeys.AiCvOllamaUnavailable);
            }

            using var document = JsonDocument.Parse(body);
            var content = document.RootElement
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return string.IsNullOrWhiteSpace(content)
                ? Fail(TranslationKeys.AiCvEmptyResponse)
                : new AiChatCompletionResult(true, content, null);
        }
        catch (HttpRequestException)
        {
            return Fail(TranslationKeys.AiCvOllamaUnavailable);
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

    private static AiChatCompletionResult Fail(string key) => new(false, null, key);
}
