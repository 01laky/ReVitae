using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ReVitae.Core.Ai.Ollama;

public interface IOllamaPullClient
{
    Task<OllamaPullResult> PullAsync(
        string modelTag,
        IProgress<OllamaPullProgress>? progress,
        CancellationToken cancellationToken = default);
}

public sealed class OllamaPullClient : IOllamaPullClient
{
    private readonly HttpClient _httpClient;

    public OllamaPullClient()
        : this(new HttpClient { Timeout = Timeout.InfiniteTimeSpan })
    {
    }

    public OllamaPullClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<OllamaPullResult> PullAsync(
        string modelTag,
        IProgress<OllamaPullProgress>? progress,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, OllamaHost.PullUri);
            request.Content = new StringContent(
                JsonSerializer.Serialize(new { name = modelTag }),
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient
                .SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return new OllamaPullResult(OllamaPullOutcome.Failed, response.ReasonPhrase);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var reader = new StreamReader(stream);

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line is null)
                {
                    break;
                }

                var pullProgress = OllamaPullStreamParser.TryParseLine(line);
                if (pullProgress is not null)
                {
                    progress?.Report(pullProgress);
                }
            }

            return new OllamaPullResult(OllamaPullOutcome.Succeeded, null);
        }
        catch (OperationCanceledException)
        {
            return new OllamaPullResult(OllamaPullOutcome.Cancelled, null);
        }
        catch (Exception ex)
        {
            return new OllamaPullResult(OllamaPullOutcome.Failed, ex.Message);
        }
    }
}

public static class OllamaPullStreamParser
{
    public static OllamaPullProgress? TryParseLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;
            var status = root.TryGetProperty("status", out var statusElement)
                ? statusElement.GetString() ?? string.Empty
                : string.Empty;

            long? completed = root.TryGetProperty("completed", out var completedElement) &&
                              completedElement.TryGetInt64(out var completedValue)
                ? completedValue
                : null;

            long? total = root.TryGetProperty("total", out var totalElement) &&
                          totalElement.TryGetInt64(out var totalValue)
                ? totalValue
                : null;

            return new OllamaPullProgress(status, completed, total);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
