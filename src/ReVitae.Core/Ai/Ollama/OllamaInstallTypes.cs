namespace ReVitae.Core.Ai.Ollama;

public enum OllamaInstallPhase
{
	DownloadingEngine,
	InstallingEngine,
	StartingEngine,
}

public sealed record OllamaInstallProgress(
	OllamaInstallPhase Phase,
	long? CompletedBytes,
	long? TotalBytes);

public sealed record OllamaInstallResult(bool Succeeded, string? ErrorMessageKey);

public interface IOllamaInstaller
{
	Task<OllamaInstallResult> EnsureInstalledAsync(
		IProgress<OllamaInstallProgress>? progress = null,
		CancellationToken cancellationToken = default);
}
