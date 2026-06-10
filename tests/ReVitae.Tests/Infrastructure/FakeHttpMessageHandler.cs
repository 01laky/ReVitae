using System.Net;
using System.Text;

namespace ReVitae.Tests.Infrastructure;

/// <summary>
/// Prompt 049 A3 / Part D — the canonical fake HTTP handler for fault injection. Replays a
/// caller-supplied responder so tests can inject timeouts, transport errors, status codes,
/// malformed/partial bodies, and cancellation without touching the network. Reused by the
/// AI provider and Ollama fault suites.
/// </summary>
internal sealed class FakeHttpMessageHandler(
	Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder) : HttpMessageHandler
{
	public int CallCount { get; private set; }

	protected override Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		CallCount++;
		return responder(request, cancellationToken);
	}

	public static FakeHttpMessageHandler Throws(Exception exception) =>
		new((_, _) => throw exception);

	public static FakeHttpMessageHandler Responds(
		HttpStatusCode statusCode,
		string body,
		params (string Name, string Value)[] headers)
	{
		return new FakeHttpMessageHandler((_, _) =>
		{
			var response = new HttpResponseMessage(statusCode)
			{
				Content = new StringContent(body, Encoding.UTF8, "application/json"),
			};

			foreach (var (name, value) in headers)
			{
				response.Headers.TryAddWithoutValidation(name, value);
			}

			return Task.FromResult(response);
		});
	}

	/// <summary>A handler that blocks until the request's cancellation token fires.</summary>
	public static FakeHttpMessageHandler HonorsCancellation() =>
		new(async (_, cancellationToken) =>
		{
			await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
			return new HttpResponseMessage(HttpStatusCode.OK);
		});

	public static HttpClient Client(FakeHttpMessageHandler handler) => new(handler);
}
