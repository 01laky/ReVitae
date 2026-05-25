using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace ReVitae.Core.Ai.Ollama;

public interface IOllamaModelDeleteClient
{
	Task<bool> TryDeleteModelAsync(string modelTag, CancellationToken cancellationToken = default);
}

public sealed class OllamaModelDeleteClient : IOllamaModelDeleteClient
{
	private readonly HttpClient _httpClient;

	public OllamaModelDeleteClient()
		: this(new HttpClient { Timeout = TimeSpan.FromSeconds(30) })
	{
	}

	public OllamaModelDeleteClient(HttpClient httpClient)
	{
		_httpClient = httpClient;
	}

	public async Task<bool> TryDeleteModelAsync(string modelTag, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(modelTag))
		{
			return false;
		}

		try
		{
			using var request = new HttpRequestMessage(HttpMethod.Delete, OllamaHost.DeleteUri);
			request.Content = new StringContent(
				JsonSerializer.Serialize(new { name = modelTag }),
				Encoding.UTF8,
				"application/json");

			using var response = await _httpClient
				.SendAsync(request, cancellationToken)
				.ConfigureAwait(false);

			return response.IsSuccessStatusCode;
		}
		catch
		{
			return false;
		}
	}
}
