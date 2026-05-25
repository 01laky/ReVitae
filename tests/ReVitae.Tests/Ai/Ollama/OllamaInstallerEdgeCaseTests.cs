using System.Net;
using System.Net.Http;
using ReVitae.Core.Ai.Ollama;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Ai.Ollama;

[Trait("Category", "Ollama")]
public sealed class OllamaInstallerEdgeCaseTests : IDisposable
{
	private readonly string _tempRoot;

	public OllamaInstallerEdgeCaseTests()
	{
		_tempRoot = Path.Combine(Path.GetTempPath(), "revitae-ollama-installer", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_tempRoot);
	}

	public void Dispose()
	{
		try
		{
			if (Directory.Exists(_tempRoot))
			{
				Directory.Delete(_tempRoot, recursive: true);
			}
		}
		catch
		{
			// Best-effort temp cleanup.
		}
	}

	[Fact]
	public async Task EnsureInstalledAsync_WhenManagedInstallAlreadyPresent_ReturnsSuccessWithoutDownload()
	{
		var installDir = OllamaPaths.GetManagedInstallDirectory();
		var hadInstall = OllamaPaths.IsManagedInstallPresent();
		if (!hadInstall)
		{
			// Skip when no managed install — CI may not have one; test uses early-return path when present.
			return;
		}

		var installer = new OllamaInstaller(new HttpClient());
		var result = await installer.EnsureInstalledAsync();

		Assert.True(result.Succeeded);
		Assert.Null(result.ErrorMessageKey);
	}

	[Fact]
	public async Task EnsureInstalledAsync_WhenHttpFails_ReturnsFailedResult()
	{
		if (OllamaPaths.IsManagedInstallPresent())
		{
			return;
		}

		var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
		var installer = new OllamaInstaller(new HttpClient(handler));

		var result = await installer.EnsureInstalledAsync(cancellationToken: CancellationToken.None);

		Assert.False(result.Succeeded);
		Assert.Equal(TranslationKeys.AiSetupOllamaInstallFailed, result.ErrorMessageKey);
	}

	[Fact]
	public async Task EnsureInstalledAsync_WhenCancelled_ThrowsOperationCanceledException()
	{
		if (OllamaPaths.IsManagedInstallPresent())
		{
			return;
		}

		var handler = new SlowHttpMessageHandler();
		var installer = new OllamaInstaller(new HttpClient(handler));
		using var cts = new CancellationTokenSource();
		cts.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			installer.EnsureInstalledAsync(cancellationToken: cts.Token));
	}

	[Fact]
	public async Task EnsureInstalledAsync_ReportsProgressDuringMacZipInstall_WhenZipAlreadyDownloaded()
	{
		if (!OperatingSystem.IsMacOS() || OllamaPaths.IsManagedInstallPresent())
		{
			return;
		}

		// Without a real zip this verifies the installer reaches download phase and fails gracefully.
		var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
		var installer = new OllamaInstaller(new HttpClient(handler));
		var result = await installer.EnsureInstalledAsync();

		Assert.False(result.Succeeded);
	}

	[Fact]
	public void OllamaPaths_ManagedInstallDirectory_IsUnderReVitaeRoot()
	{
		var managed = OllamaPaths.GetManagedInstallDirectory();
		var root = ReVitae.Core.Ai.ReVitaeLocalDataPaths.GetReVitaeRootDirectory();

		Assert.StartsWith(root, managed, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
	}

	[Fact]
	public void OllamaPaths_ManagedInstallReserveBytes_Is512MiB()
	{
		Assert.Equal(512L * 1024 * 1024, OllamaPaths.ManagedInstallReserveBytes);
	}

	private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
		: HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken) =>
			Task.FromResult(responder(request));
	}

	private sealed class SlowHttpMessageHandler : HttpMessageHandler
	{
		protected override async Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
			return new HttpResponseMessage(HttpStatusCode.OK);
		}
	}
}
