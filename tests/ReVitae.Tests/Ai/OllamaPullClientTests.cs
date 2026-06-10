using ReVitae.Core.Ai;
using ReVitae.Core.Ai.Ollama;

namespace ReVitae.Tests.Ai;

public sealed class OllamaPullClientTests
{
	[Fact]
	public void OllamaPullClient_ParsesStreamLines()
	{
		var progress = OllamaPullStreamParser.TryParseLine(
			"""{"status":"downloading","completed":100,"total":200}""");

		Assert.NotNull(progress);
		Assert.Equal("downloading", progress!.Status);
		Assert.Equal(100, progress.Completed);
		Assert.Equal(200, progress.Total);
	}

	[Fact]
	public async Task OllamaPullClient_ReadsStreamingResponse()
	{
		var handler = new FakeOllamaPullHandler(
			"""
            {"status":"pulling manifest"}
            {"status":"downloading","completed":50,"total":100}
            {"status":"success"}
            """);

		var client = new OllamaPullClient(new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:11434/") });

		// Synchronous IProgress, NOT Progress<T>: Progress<T> marshals its callback onto the
		// thread pool when no SynchronizationContext is present, so reports can land after the
		// await and mutate the list while the assertions enumerate it ("Collection was modified").
		var progress = new SynchronousProgress<OllamaPullProgress>();
		var result = await client.PullAsync(
			"llama3.2:3b-instruct",
			progress,
			CancellationToken.None);
		var statuses = progress.Items;

		Assert.Equal(OllamaPullOutcome.Succeeded, result.Outcome);
		Assert.Contains(statuses, value =>
			string.Equals(value.Status, "downloading", StringComparison.Ordinal) &&
			value.Completed == 50 &&
			value.Total == 100);
		Assert.Contains(statuses, value =>
			string.Equals(value.Status, "success", StringComparison.Ordinal));
	}

	private sealed class SynchronousProgress<T> : IProgress<T>
	{
		public List<T> Items { get; } = [];

		public void Report(T value) => Items.Add(value);
	}

	private sealed class FakeOllamaPullHandler(string body) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
			{
				Content = new StringContent(body),
			};

			return Task.FromResult(response);
		}
	}
}
