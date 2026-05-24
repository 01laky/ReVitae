using System.Net.Http.Json;
using System.Text.Json;

namespace ReVitae.Core.Ai.Ollama;

public interface IOllamaRuntimeProbe
{
    Task<OllamaRuntimeStatus> ProbeAsync(CancellationToken cancellationToken = default);
}

public sealed class OllamaRuntimeProbe : IOllamaRuntimeProbe
{
    private static readonly Uri TagsUri = new("http://127.0.0.1:11434/api/tags");
    private readonly HttpClient _httpClient;

    public OllamaRuntimeProbe()
        : this(new HttpClient { Timeout = TimeSpan.FromSeconds(1) })
    {
    }

    public OllamaRuntimeProbe(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<OllamaRuntimeStatus> ProbeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync(TagsUri, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return new OllamaRuntimeStatus(false, []);
            }

            var payload = await response.Content.ReadFromJsonAsync<OllamaTagsResponse>(
                cancellationToken: cancellationToken).ConfigureAwait(false);

            var tags = payload?.Models?
                .Select(model => model.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name!)
                .ToArray() ?? [];

            return new OllamaRuntimeStatus(true, tags);
        }
        catch
        {
            return new OllamaRuntimeStatus(false, []);
        }
    }

    private sealed class OllamaTagsResponse
    {
        public List<OllamaTagModel>? Models { get; init; }
    }

    private sealed class OllamaTagModel
    {
        public string? Name { get; init; }
    }
}
