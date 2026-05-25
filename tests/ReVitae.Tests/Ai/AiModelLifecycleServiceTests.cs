using ReVitae.Core.Ai;
using ReVitae.Core.Ai.Download;
using ReVitae.Core.Ai.Ollama;
using ReVitae.Core.Localization;
using ReVitae.Tests.Ai.Download;

namespace ReVitae.Tests.Ai;

public sealed class AiModelInstallationMatcherTests
{
	[Theory]
	[InlineData("llama3.2:3b-instruct", "llama3.2:3b-instruct", true)]
	[InlineData("llama3.2:3b-instruct:abc123", "llama3.2:3b-instruct", true)]
	[InlineData("gemma2:2b", "llama3.2:3b-instruct", false)]
	public void IsTagForModel_MatchesExpected(string installedTag, string catalogTag, bool expected) =>
		Assert.Equal(expected, AiModelInstallationMatcher.IsTagForModel(installedTag, catalogTag));
}

public sealed class AiModelLifecycleServiceTests
{
	[Fact]
	public void AnalyzeModel_DetectsInstalledModel()
	{
		var model = AiModelCatalog.TryGetById("gemma2-2b")!;
		var status = AiModelLifecycleService.AnalyzeModel(
			model,
			new OllamaRuntimeStatus(true, ["gemma2:2b"]),
			IdleSnapshot,
			null,
			hasActiveDownload: false);

		Assert.Equal(AiModelInstallPresence.Installed, status.Presence);
		Assert.True(status.CanUninstall);
		Assert.False(status.CanCleanStaleDownload);
	}

	[Fact]
	public void AnalyzeModel_DetectsStaleFailedJob()
	{
		var model = AiModelCatalog.TryGetById("gemma2-2b")!;
		var failedJob = CreateJob(model, AiDownloadJobState.Failed, 10, 100);
		var status = AiModelLifecycleService.AnalyzeModel(
			model,
			new OllamaRuntimeStatus(false, []),
			IdleSnapshot,
			failedJob,
			hasActiveDownload: false);

		Assert.Equal(AiModelInstallPresence.StaleDownload, status.Presence);
		Assert.True(status.CanCleanStaleDownload);
		Assert.False(status.CanUninstall);
	}

	[Fact]
	public void AnalyzeModel_DetectsActiveDownload()
	{
		var model = AiModelCatalog.TryGetById("gemma2-2b")!;
		var activeJob = CreateJob(model, AiDownloadJobState.Downloading, 10, 100);
		var status = AiModelLifecycleService.AnalyzeModel(
			model,
			new OllamaRuntimeStatus(true, []),
			activeJob,
			null,
			hasActiveDownload: true);

		Assert.Equal(AiModelInstallPresence.ActiveDownload, status.Presence);
		Assert.False(status.CanUninstall);
		Assert.False(status.CanCleanStaleDownload);
	}

	[Fact]
	public async Task TryUninstallModelAsync_DeletesMatchingTagsAndClearsSettings()
	{
		var directory = Path.Combine(Path.GetTempPath(), "revitae-tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		var settingsPath = Path.Combine(directory, "ai-settings.json");
		var jobPath = Path.Combine(directory, "ai-download-job.json");

		var settingsStorage = new AiSettingsStorage(settingsPath);
		settingsStorage.Save(new AiSettingsSnapshot("gemma2-2b", "gemma2:2b", DateTimeOffset.UtcNow));

		var deleteClient = new RecordingDeleteClient();
		var clock = new FakeClock(DateTimeOffset.UtcNow);
		var service = new AiModelLifecycleService(
			deleteClient,
			new AiDownloadJobStorage(jobPath, clock),
			settingsStorage);

		var model = AiModelCatalog.TryGetById("gemma2-2b")!;
		var result = await service.TryUninstallModelAsync(
			model,
			new OllamaRuntimeStatus(true, ["gemma2:2b", "gemma2:2b:deadbeef"]),
			hasActiveDownload: false);

		Assert.True(result.Succeeded);
		Assert.Equal(2, deleteClient.DeletedTags.Count);
		Assert.Null(settingsStorage.TryLoad());
	}

	[Fact]
	public async Task TryCleanStaleDownloadAsync_ClearsPersistedJob()
	{
		var directory = Path.Combine(Path.GetTempPath(), "revitae-tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		var jobPath = Path.Combine(directory, "ai-download-job.json");
		var clock = new FakeClock(DateTimeOffset.UtcNow);
		var jobStorage = new AiDownloadJobStorage(jobPath, clock);
		var model = AiModelCatalog.TryGetById("gemma2-2b")!;
		jobStorage.Save(CreateJob(model, AiDownloadJobState.Failed, 10, 100), forceFlush: true);

		var service = new AiModelLifecycleService(
			new RecordingDeleteClient(),
			jobStorage,
			new AiSettingsStorage(Path.Combine(directory, "ai-settings.json")));

		var result = await service.TryCleanStaleDownloadAsync(
			model,
			new OllamaRuntimeStatus(false, []),
			hasActiveDownload: false);

		Assert.True(result.Succeeded);
		Assert.Null(jobStorage.TryLoad());
	}

	private static readonly AiDownloadJobSnapshot IdleSnapshot = new(
		Guid.Empty,
		string.Empty,
		string.Empty,
		string.Empty,
		AiDownloadJobState.Idle,
		false,
		null,
		null,
		null,
		DateTimeOffset.MinValue,
		DateTimeOffset.MinValue,
		null);

	private static AiDownloadJobSnapshot CreateJob(
		AiModelCatalogEntry model,
		AiDownloadJobState state,
		long completed,
		long total) =>
		new(
			Guid.NewGuid(),
			model.Id,
			model.OllamaModelTag,
			model.DisplayNameKey,
			state,
			false,
			completed,
			total,
			AiDownloadStatus.FromTranslationKey(TranslationKeys.AiDownloadPullingModel),
			DateTimeOffset.UtcNow,
			DateTimeOffset.UtcNow,
			null);

	private sealed class RecordingDeleteClient : IOllamaModelDeleteClient
	{
		public List<string> DeletedTags { get; } = [];

		public Task<bool> TryDeleteModelAsync(string modelTag, CancellationToken cancellationToken = default)
		{
			DeletedTags.Add(modelTag);
			return Task.FromResult(true);
		}
	}
}
