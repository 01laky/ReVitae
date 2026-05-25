using ReVitae.Core.Ai;
using ReVitae.Core.Ai.Ollama;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Ai;

public sealed class OllamaHostTests
{
	[Fact]
	public void BaseUri_UsesDefaultWhenEnvMissing()
	{
		var uri = OllamaHost.BaseUri;

		Assert.Equal("127.0.0.1", uri.Host);
		Assert.Equal(11434, uri.Port);
	}

	[Fact]
	public void TagsUri_PullUri_And_DeleteUri_AreUnderBase()
	{
		Assert.EndsWith("/api/tags", OllamaHost.TagsUri.AbsolutePath, StringComparison.Ordinal);
		Assert.EndsWith("/api/pull", OllamaHost.PullUri.AbsolutePath, StringComparison.Ordinal);
		Assert.EndsWith("/api/delete", OllamaHost.DeleteUri.AbsolutePath, StringComparison.Ordinal);
		Assert.Equal(OllamaHost.BaseUri.Host, OllamaHost.PullUri.Host);
	}
}

public sealed class OllamaStartupHelperTests
{
	[Fact]
	public async Task EnsureReachableAsync_ReturnsErrorKeyWhenUnreachable()
	{
		var probe = new AlwaysUnreachableProbe();
		var installer = new NoOpOllamaInstaller(shouldSucceed: false);
		var result = await OllamaStartupHelper.EnsureReachableAsync(
			probe,
			installer: installer,
			options: OllamaReachabilityOptions.FastForTests,
			allowInstall: true);

		Assert.False(result.Status.IsReachable);
		Assert.Equal(
			OllamaPaths.IsManagedInstallPresent()
				? TranslationKeys.AiSetupOllamaNotRunning
				: TranslationKeys.AiSetupOllamaInstallFailed,
			result.ErrorMessageKey);
	}

	private sealed class NoOpOllamaInstaller(bool shouldSucceed) : IOllamaInstaller
	{
		public Task<OllamaInstallResult> EnsureInstalledAsync(
			IProgress<OllamaInstallProgress>? progress = null,
			CancellationToken cancellationToken = default) =>
			Task.FromResult(new OllamaInstallResult(
				shouldSucceed,
				shouldSucceed ? null : TranslationKeys.AiSetupOllamaInstallFailed));
	}

	private sealed class AlwaysUnreachableProbe : IOllamaRuntimeProbe
	{
		public Task<OllamaRuntimeStatus> ProbeAsync(CancellationToken cancellationToken = default) =>
			Task.FromResult(new OllamaRuntimeStatus(false, []));
	}
}
