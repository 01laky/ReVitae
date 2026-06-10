using System.Net;
using ReVitae.Core.Ai;
using ReVitae.Core.Ai.Ollama;
using ReVitae.Tests.Infrastructure;

namespace ReVitae.Tests.Ai;

/// <summary>
/// Prompt 049 B6 — Ollama pull faults. <see cref="OllamaPullClient"/> accepts an injectable
/// <c>HttpClient</c>, so faults are driven through <see cref="FakeHttpMessageHandler"/> with no
/// production change: HTTP errors and transport failures map to Failed, cancellation/timeout map
/// to Cancelled, malformed progress lines are skipped, and a clean stream Succeeds with progress.
/// </summary>
[Trait("Category", "Ollama")]
public sealed class OllamaPullClientFaultTests
{
	private sealed class CollectingProgress : IProgress<OllamaPullProgress>
	{
		public List<OllamaPullProgress> Reports { get; } = [];

		public void Report(OllamaPullProgress value) => Reports.Add(value);
	}

	private static OllamaPullClient Client(FakeHttpMessageHandler handler) =>
		new(FakeHttpMessageHandler.Client(handler));

	[Theory]
	[InlineData(HttpStatusCode.InternalServerError)]
	[InlineData(HttpStatusCode.ServiceUnavailable)]
	[InlineData(HttpStatusCode.NotFound)]
	public async Task HttpError_MapsToFailed(HttpStatusCode statusCode)
	{
		var client = Client(FakeHttpMessageHandler.Responds(statusCode, "nope"));

		var result = await client.PullAsync("llama3.2:3b", null);

		Assert.Equal(OllamaPullOutcome.Failed, result.Outcome);
	}

	[Fact]
	public async Task TransportError_MapsToFailed()
	{
		var client = Client(FakeHttpMessageHandler.Throws(new HttpRequestException("connection refused")));

		var result = await client.PullAsync("llama3.2:3b", null);

		Assert.Equal(OllamaPullOutcome.Failed, result.Outcome);
		Assert.NotNull(result.ErrorMessage);
	}

	[Fact]
	public async Task Timeout_MapsToCancelled()
	{
		var client = Client(FakeHttpMessageHandler.Throws(new TaskCanceledException("timeout")));

		var result = await client.PullAsync("llama3.2:3b", null);

		Assert.Equal(OllamaPullOutcome.Cancelled, result.Outcome);
	}

	[Fact]
	public async Task PreCancelledToken_MapsToCancelled()
	{
		var client = Client(FakeHttpMessageHandler.HonorsCancellation());
		using var cts = new CancellationTokenSource();
		cts.Cancel();

		var result = await client.PullAsync("llama3.2:3b", null, cts.Token);

		Assert.Equal(OllamaPullOutcome.Cancelled, result.Outcome);
	}

	[Fact]
	public async Task CleanStream_SucceedsAndReportsProgress()
	{
		var ndjson = string.Join('\n',
			"{\"status\":\"pulling manifest\"}",
			"{\"status\":\"downloading\",\"completed\":10,\"total\":100}",
			"{\"status\":\"downloading\",\"completed\":60,\"total\":100}",
			"{\"status\":\"downloading\",\"completed\":5,\"total\":50}",
			"{\"status\":\"success\"}");
		var client = Client(FakeHttpMessageHandler.Responds(HttpStatusCode.OK, ndjson));
		var progress = new CollectingProgress();

		var result = await client.PullAsync("llama3.2:3b", progress);

		Assert.Equal(OllamaPullOutcome.Succeeded, result.Outcome);
		Assert.Contains(progress.Reports, report => report.Status == "success");
		Assert.Contains(progress.Reports, report => report.Completed == 60 && report.Total == 100);
	}

	[Fact]
	public async Task MalformedLinesInStream_AreSkipped_StillSucceeds()
	{
		var ndjson = string.Join('\n',
			"{ not json at all",
			"",
			"plain text line",
			"{\"status\":\"downloading\",\"completed\":1,\"total\":2}",
			"{\"status\":\"success\"}");
		var client = Client(FakeHttpMessageHandler.Responds(HttpStatusCode.OK, ndjson));
		var progress = new CollectingProgress();

		var result = await client.PullAsync("llama3.2:3b", progress);

		Assert.Equal(OllamaPullOutcome.Succeeded, result.Outcome);
		Assert.Contains(progress.Reports, report => report.Status == "success");
		Assert.DoesNotContain(progress.Reports, report => report.Status == "plain text line");
	}

	[Fact]
	public async Task EmptyStream_Succeeds()
	{
		var client = Client(FakeHttpMessageHandler.Responds(HttpStatusCode.OK, string.Empty));
		var progress = new CollectingProgress();

		var result = await client.PullAsync("llama3.2:3b", progress);

		Assert.Equal(OllamaPullOutcome.Succeeded, result.Outcome);
		Assert.Empty(progress.Reports);
	}
}
