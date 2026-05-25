using ReVitae.Core.Ai;
using ReVitae.Core.Ai.Download;
using ReVitae.Core.Ai.Ollama;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Ai.Download;

internal sealed class FakeClock(DateTimeOffset start) : IClock
{
	private DateTimeOffset _now = start;

	public DateTimeOffset UtcNow => _now;

	public void Advance(TimeSpan duration) => _now += duration;
}

public sealed class AiDownloadProgressTests
{
	[Theory]
	[InlineData(50, 100, 50)]
	[InlineData(0, 100, 0)]
	[InlineData(100, 100, 100)]
	[InlineData(150, 100, 100)]
	public void TryGetPercent_ComputesAndClamps(long completed, long total, int expected)
	{
		Assert.Equal(expected, AiDownloadProgress.TryGetPercent(completed, total));
	}

	[Fact]
	public void TryGetPercent_ReturnsNullWhenTotalMissing()
	{
		Assert.Null(AiDownloadProgress.TryGetPercent(10, null));
		Assert.Null(AiDownloadProgress.TryGetPercent(null, 100));
	}

	[Fact]
	public void TryGetPercent_ReturnsNullForNonPositiveValues()
	{
		Assert.Null(AiDownloadProgress.TryGetPercent(-1, 100));
		Assert.Null(AiDownloadProgress.TryGetPercent(10, 0));
	}

	[Fact]
	public void TryGetPercent_HandlesLayerReset()
	{
		Assert.Equal(50, AiDownloadProgress.TryGetPercent(50, 100));
		Assert.Equal(10, AiDownloadProgress.TryGetPercent(10, 100));
	}
}

public sealed class AiDownloadUiStateMapperTests
{
	[Theory]
	[InlineData(AiDownloadJobState.Downloading, false, false, true)]
	[InlineData(AiDownloadJobState.Paused, false, false, true)]
	[InlineData(AiDownloadJobState.Completed, false, false, true)]
	[InlineData(AiDownloadJobState.Downloading, true, false, false)]
	[InlineData(AiDownloadJobState.Downloading, false, true, true)]
	[InlineData(AiDownloadJobState.Idle, false, false, false)]
	public void ShouldShowDock_UsesVisibilityRules(
		AiDownloadJobState state,
		bool introVisible,
		bool modalVisible,
		bool expected)
	{
		Assert.Equal(
			expected,
			AiDownloadUiStateMapper.ShouldShowDock(state, introVisible, modalVisible));
	}

	[Theory]
	[InlineData(AiDownloadJobState.Downloading, false, true)]
	[InlineData(AiDownloadJobState.Interrupted, false, true)]
	[InlineData(AiDownloadJobState.Paused, false, false)]
	[InlineData(AiDownloadJobState.Downloading, true, false)]
	public void ShouldShowHeaderBadge_UsesBadgeRules(
		AiDownloadJobState state,
		bool modalVisible,
		bool expected)
	{
		Assert.Equal(
			expected,
			AiDownloadUiStateMapper.ShouldShowHeaderBadge(state, modalVisible));
	}
}

public sealed class AiDownloadJobStorageTests
{
	[Fact]
	public void AiDownloadJobStorage_RoundTrips()
	{
		var path = CreateTempJobPath();
		var clock = new FakeClock(DateTimeOffset.Parse("2026-05-21T10:00:00Z"));
		var storage = new AiDownloadJobStorage(path, clock);
		var snapshot = CreateSampleSnapshot(clock.UtcNow);

		storage.Save(snapshot, forceFlush: true);
		var loaded = storage.TryLoad();

		Assert.NotNull(loaded);
		Assert.Equal(snapshot.JobId, loaded!.JobId);
		Assert.Equal(snapshot.State, loaded.State);
		Assert.Equal(snapshot.CompletedBytes, loaded.CompletedBytes);
		Assert.Equal(snapshot.ErrorMessageKey, loaded.ErrorMessageKey);
	}

	[Fact]
	public void AiDownloadJobStorage_AtomicWrite_LeavesValidFile()
	{
		var path = CreateTempJobPath();
		var clock = new FakeClock(DateTimeOffset.UtcNow);
		var storage = new AiDownloadJobStorage(path, clock);
		storage.Save(CreateSampleSnapshot(clock.UtcNow), forceFlush: true);

		Assert.True(File.Exists(path));
		Assert.False(File.Exists(path + ".tmp"));
	}

	[Fact]
	public void AiDownloadJobStorage_CorruptFile_ReturnsNullAndQuarantines()
	{
		var path = CreateTempJobPath();
		File.WriteAllText(path, "{ not-json");

		var storage = new AiDownloadJobStorage(path, new FakeClock(DateTimeOffset.UtcNow));
		Assert.Null(storage.TryLoad());
		Assert.False(File.Exists(path));
		Assert.True(File.Exists(path + ".bak"));
	}

	[Fact]
	public void AiDownloadJobStorage_ThrottlesProgressWrites()
	{
		var path = CreateTempJobPath();
		var clock = new FakeClock(DateTimeOffset.UtcNow);
		var storage = new AiDownloadJobStorage(path, clock);
		var snapshot = CreateSampleSnapshot(clock.UtcNow) with { State = AiDownloadJobState.Downloading };

		storage.Save(snapshot, forceFlush: true);
		var jsonAfterFirstFlush = File.ReadAllText(path);

		for (var i = 0; i < 10; i++)
		{
			storage.Save(snapshot with { CompletedBytes = i * 100L });
		}

		Assert.Equal(jsonAfterFirstFlush, File.ReadAllText(path));

		clock.Advance(TimeSpan.FromMilliseconds(600));
		storage.Save(snapshot with { CompletedBytes = 9999 });
		Assert.NotEqual(jsonAfterFirstFlush, File.ReadAllText(path));
	}

	[Fact]
	public void AiDownloadJobStorage_FlushesImmediatelyOnStateChange()
	{
		var path = CreateTempJobPath();
		var clock = new FakeClock(DateTimeOffset.UtcNow);
		var storage = new AiDownloadJobStorage(path, clock);
		var downloading = CreateSampleSnapshot(clock.UtcNow) with { State = AiDownloadJobState.Downloading };
		storage.Save(downloading, forceFlush: true);
		var firstWrite = File.GetLastWriteTimeUtc(path);

		storage.Save(downloading with { State = AiDownloadJobState.Paused }, forceFlush: true);
		Assert.True(File.GetLastWriteTimeUtc(path) >= firstWrite);
		Assert.Equal(AiDownloadJobState.Paused, storage.TryLoad()!.State);
	}

	private static string CreateTempJobPath()
	{
		var directory = Path.Combine(Path.GetTempPath(), "revitae-tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		return Path.Combine(directory, "ai-download-job.json");
	}

	private static AiDownloadJobSnapshot CreateSampleSnapshot(DateTimeOffset now) =>
		new(
			Guid.Parse("8f3c2e1a-1111-2222-3333-444455556666"),
			"llama31-8b",
			"llama3.1:8b-instruct",
			TranslationKeys.AiModelLlama31_8bName,
			AiDownloadJobState.Downloading,
			false,
			100,
			200,
			"downloading",
			now,
			now,
			null);
}

public sealed class AiModelDownloadCoordinatorTests
{
	private static async Task WaitForActivePull(FakeOllamaPullClient fakePull)
	{
		await fakePull.PullStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
	}

	[Fact]
	public async Task Coordinator_Pause_DuringEngineSetup_MapsToPaused()
	{
		var harness = CreateHarness();
		harness.OllamaProbe.HangUntilCancelled = true;
		harness.Coordinator.TryStart(GetTestModel(), false);
		await harness.OllamaProbe.WaitUntilHangStartedAsync();

		harness.Coordinator.Pause();
		await WaitForState(harness.Coordinator, AiDownloadJobState.Paused);

		Assert.Equal(AiDownloadJobState.Paused, harness.Coordinator.CurrentSnapshot.State);
	}

	[Fact]
	public async Task Coordinator_Stop_DuringEngineSetup_ClearsJobFile()
	{
		var harness = CreateHarness();
		harness.OllamaProbe.HangUntilCancelled = true;
		harness.Coordinator.TryStart(GetTestModel(), false);
		await harness.OllamaProbe.WaitUntilHangStartedAsync();

		await harness.Coordinator.StopAsync();
		await WaitForState(harness.Coordinator, AiDownloadJobState.Idle);

		Assert.Null(harness.JobStorage.TryLoad());
	}

	[Fact]
	public async Task Coordinator_Pause_MapsCancelledToPaused()
	{
		var harness = CreateHarness(blockPullUntilCancelled: true);
		harness.Coordinator.TryStart(GetTestModel(), false);
		await WaitForActivePull(harness.FakePullClient);

		await harness.Coordinator.PauseAsync();
		await WaitForState(harness.Coordinator, AiDownloadJobState.Paused);

		Assert.Equal(AiDownloadJobState.Paused, harness.Coordinator.CurrentSnapshot.State);
		Assert.Equal(AiDownloadJobState.Paused, harness.JobStorage.TryLoad()!.State);
	}

	[Fact]
	public async Task Coordinator_Pause_LateProgressDoesNotRevertPausedState()
	{
		var harness = CreateHarness(blockPullUntilCancelled: true);
		harness.Coordinator.TryStart(GetTestModel(), false);
		await WaitForActivePull(harness.FakePullClient);

		await harness.Coordinator.PauseAsync();
		await WaitForState(harness.Coordinator, AiDownloadJobState.Paused);

		harness.FakePullClient.ReportProgress(new OllamaPullProgress("pulling layer", 50, 100));
		await Task.Delay(50);

		Assert.Equal(AiDownloadJobState.Paused, harness.Coordinator.CurrentSnapshot.State);
		Assert.Equal(AiDownloadJobState.Paused, harness.JobStorage.TryLoad()!.State);
	}

	[Fact]
	public async Task Coordinator_Stop_ClearsJobFile()
	{
		var harness = CreateHarness(blockPullUntilCancelled: true);
		harness.Coordinator.TryStart(GetTestModel(), false);
		await WaitForActivePull(harness.FakePullClient);

		await harness.Coordinator.StopAsync();
		await WaitForState(harness.Coordinator, AiDownloadJobState.Idle);

		Assert.Null(harness.JobStorage.TryLoad());
	}

	[Fact]
	public void HasPriorModelPullProgress_DetectsPausedModelDownload()
	{
		var snapshot = new AiDownloadJobSnapshot(
			Guid.NewGuid(),
			"gemma2-2b",
			"gemma2:2b",
			TranslationKeys.AiModelGemma2_2bName,
			AiDownloadJobState.Paused,
			false,
			25_000_000,
			167_608_636,
			AiDownloadStatus.FromTranslationKey(TranslationKeys.AiDownloadPullingModel),
			DateTimeOffset.UtcNow,
			DateTimeOffset.UtcNow,
			null);

		Assert.True(AiModelDownloadCoordinator.HasPriorModelPullProgress(snapshot));
	}

	[Fact]
	public void HasPriorModelPullProgress_IgnoresFreshEngineInstall()
	{
		var snapshot = new AiDownloadJobSnapshot(
			Guid.NewGuid(),
			"gemma2-2b",
			"gemma2:2b",
			TranslationKeys.AiModelGemma2_2bName,
			AiDownloadJobState.Downloading,
			false,
			null,
			null,
			AiDownloadStatus.FromTranslationKey(TranslationKeys.AiDownloadDownloadingEngine),
			DateTimeOffset.UtcNow,
			DateTimeOffset.UtcNow,
			null);

		Assert.False(AiModelDownloadCoordinator.HasPriorModelPullProgress(snapshot));
	}

	[Fact]
	public async Task Coordinator_Resume_SucceedsAfterTransientProbeFailures()
	{
		var harness = CreateHarness(blockPullUntilCancelled: true);
		harness.Coordinator.TryStart(GetTestModel(), false);
		await WaitForActivePull(harness.FakePullClient);
		harness.Coordinator.Pause();
		await WaitForState(harness.Coordinator, AiDownloadJobState.Paused);

		harness.OllamaProbe.FailUntilProbeCount = harness.OllamaProbe.ProbeCallCount + 5;
		harness.FakePullClient.BlockUntilCancelled = false;

		await harness.Coordinator.ResumeAsync();
		await WaitForPullCompletion(harness);

		Assert.Equal(2, harness.FakePullClient.PullCallCount);
		Assert.Equal(AiDownloadJobState.Idle, harness.Coordinator.CurrentSnapshot.State);
	}

	[Fact]
	public async Task Coordinator_Resume_IssuesSecondPull()
	{
		var harness = CreateHarness(blockPullUntilCancelled: true);
		harness.Coordinator.TryStart(GetTestModel(), false);
		await WaitForActivePull(harness.FakePullClient);
		harness.Coordinator.Pause();
		await WaitForState(harness.Coordinator, AiDownloadJobState.Paused);

		await harness.Coordinator.ResumeAsync();
		await WaitForPullCompletion(harness);

		Assert.Equal(2, harness.FakePullClient.PullCallCount);
	}

	[Fact]
	public async Task Coordinator_Resume_RechecksDiskSpace()
	{
		var harness = CreateHarness(blockPullUntilCancelled: true);
		harness.Coordinator.TryStart(GetTestModel(), false);
		await WaitForActivePull(harness.FakePullClient);
		harness.Coordinator.Pause();
		await WaitForState(harness.Coordinator, AiDownloadJobState.Paused);

		harness.DiskChecker.SetHasSpace(false);
		await harness.Coordinator.ResumeAsync();

		Assert.Equal(AiDownloadJobState.Failed, harness.Coordinator.CurrentSnapshot.State);
		Assert.Equal(
			TranslationKeys.AiDownloadInsufficientDiskSpace,
			harness.Coordinator.CurrentSnapshot.ErrorMessageKey);
	}

	[Fact]
	public async Task Coordinator_Resume_AllowedAfterDiskSpaceRecovered()
	{
		var harness = CreateHarness(blockPullUntilCancelled: true);
		harness.Coordinator.TryStart(GetTestModel(), false);
		await WaitForActivePull(harness.FakePullClient);
		harness.Coordinator.Pause();
		await WaitForState(harness.Coordinator, AiDownloadJobState.Paused);

		harness.DiskChecker.SetHasSpace(false);
		await harness.Coordinator.ResumeAsync();
		harness.DiskChecker.SetHasSpace(true);
		harness.FakePullClient.BlockUntilCancelled = false;

		await harness.Coordinator.ResumeAsync();
		await WaitForPullCompletion(harness);
		Assert.Equal(AiDownloadJobState.Idle, harness.Coordinator.CurrentSnapshot.State);
	}

	[Fact]
	public async Task Coordinator_Failed_KeepsJobForRetry()
	{
		var harness = CreateHarness(failPull: true);
		harness.Coordinator.TryStart(GetTestModel(), false);
		await WaitForState(harness.Coordinator, AiDownloadJobState.Failed);

		Assert.NotNull(harness.JobStorage.TryLoad());
		Assert.Equal(AiDownloadJobState.Failed, harness.JobStorage.TryLoad()!.State);
	}

	[Fact]
	public async Task Coordinator_Completing_WritesAiSettings()
	{
		var harness = CreateHarness();
		harness.Coordinator.CompletedDwellDuration = TimeSpan.FromMilliseconds(10);
		harness.Coordinator.TryStart(GetTestModel(), false);
		await WaitForPullCompletion(harness);
		await Task.Delay(30);

		Assert.NotNull(harness.SettingsStorage.TryLoad());
		Assert.Equal("llama32-3b", harness.SettingsStorage.TryLoad()!.SelectedModelId);
		Assert.Null(harness.JobStorage.TryLoad());
	}

	[Fact]
	public void Coordinator_Start_BlockedWhenJobActive()
	{
		var harness = CreateHarness(blockPullUntilCancelled: true);
		Assert.True(harness.Coordinator.TryStart(GetTestModel(), false));
		Assert.False(harness.Coordinator.TryStart(GetTestModel(), false));
	}

	[Fact]
	public async Task Coordinator_StartupRecover_AutoResumesDownloading()
	{
		var harness = CreateHarness();
		var now = harness.Clock.UtcNow;
		harness.JobStorage.Save(
			new AiDownloadJobSnapshot(
				Guid.NewGuid(),
				"llama32-3b",
				"llama3.2:3b-instruct",
				TranslationKeys.AiModelLlama32_3bName,
				AiDownloadJobState.Downloading,
				false,
				10,
				100,
				"downloading",
				now,
				now,
				null),
			forceFlush: true);

		await harness.Coordinator.TryRecoverOnStartupAsync();
		await WaitForPullCompletion(harness);

		Assert.NotNull(harness.SettingsStorage.TryLoad());
	}

	[Fact]
	public async Task Coordinator_StartupRecover_DoesNotAutoResumePaused()
	{
		var harness = CreateHarness();
		var now = harness.Clock.UtcNow;
		harness.JobStorage.Save(
			new AiDownloadJobSnapshot(
				Guid.NewGuid(),
				"llama32-3b",
				"llama3.2:3b-instruct",
				TranslationKeys.AiModelLlama32_3bName,
				AiDownloadJobState.Paused,
				false,
				10,
				100,
				"paused",
				now,
				now,
				null),
			forceFlush: true);

		await harness.Coordinator.TryRecoverOnStartupAsync();

		Assert.Equal(AiDownloadJobState.Paused, harness.Coordinator.CurrentSnapshot.State);
		Assert.Equal(0, harness.FakePullClient.PullCallCount);
	}

	[Fact]
	public async Task Coordinator_StartupRecover_BackoffRetriesOllama()
	{
		var harness = CreateHarness();
		harness.OllamaProbe.FailuresBeforeSuccess = 2;
		var now = harness.Clock.UtcNow;
		harness.JobStorage.Save(
			new AiDownloadJobSnapshot(
				Guid.NewGuid(),
				"llama32-3b",
				"llama3.2:3b-instruct",
				TranslationKeys.AiModelLlama32_3bName,
				AiDownloadJobState.Downloading,
				false,
				null,
				null,
				null,
				now,
				now,
				null),
			forceFlush: true);

		await harness.Coordinator.TryRecoverOnStartupAsync();
		await WaitForPullCompletion(harness);

		Assert.True(harness.OllamaProbe.ProbeCallCount >= 3);
		Assert.NotNull(harness.SettingsStorage.TryLoad());
	}

	[Fact]
	public async Task Coordinator_StartupRecover_BackoffExhausted_SetsFailed()
	{
		var harness = CreateHarness();
		harness.OllamaProbe.AlwaysUnreachable = true;
		harness.OllamaInstaller.ShouldSucceed = false;
		var now = harness.Clock.UtcNow;
		harness.JobStorage.Save(
			new AiDownloadJobSnapshot(
				Guid.NewGuid(),
				"llama32-3b",
				"llama3.2:3b-instruct",
				TranslationKeys.AiModelLlama32_3bName,
				AiDownloadJobState.Downloading,
				false,
				null,
				null,
				null,
				now,
				now,
				null),
			forceFlush: true);

		await harness.Coordinator.TryRecoverOnStartupAsync();
		await WaitForState(harness.Coordinator, AiDownloadJobState.Failed);

		Assert.Equal(AiDownloadJobState.Failed, harness.Coordinator.CurrentSnapshot.State);
		Assert.Equal(
			ExpectedUnreachableFailureKeyAfterInstallAttempt(harness.OllamaInstaller.InstallCallCount),
			harness.Coordinator.CurrentSnapshot.ErrorMessageKey);
		Assert.InRange(harness.OllamaInstaller.InstallCallCount, 0, 2);
	}

	[Fact]
	public async Task Coordinator_PullHttpError_SetsFailed()
	{
		var harness = CreateHarness(failPull: true);
		harness.Coordinator.TryStart(GetTestModel(), false);
		await WaitForState(harness.Coordinator, AiDownloadJobState.Failed);
		Assert.Equal(AiDownloadJobState.Failed, harness.Coordinator.CurrentSnapshot.State);
	}

	[Fact]
	public async Task Coordinator_TryStart_OllamaUnreachable_SetsFailedWithReachabilityKey()
	{
		var harness = CreateHarness();
		harness.OllamaProbe.AlwaysUnreachable = true;
		harness.OllamaInstaller.ShouldSucceed = false;
		harness.Coordinator.TryStart(GetTestModel(), false);
		await WaitForState(harness.Coordinator, AiDownloadJobState.Failed);

		Assert.Equal(AiDownloadJobState.Failed, harness.Coordinator.CurrentSnapshot.State);
		Assert.Equal(
			ExpectedUnreachableFailureKeyAfterInstallAttempt(harness.OllamaInstaller.InstallCallCount),
			harness.Coordinator.CurrentSnapshot.ErrorMessageKey);
		Assert.InRange(harness.OllamaInstaller.InstallCallCount, 0, 1);
	}

	[Fact]
	public async Task Coordinator_DiskSpaceFailureMidPull_SetsInsufficientDiskKey()
	{
		var harness = CreateHarness(blockPullUntilCancelled: true);
		harness.Coordinator.TryStart(GetTestModel(), false);
		await WaitForActivePull(harness.FakePullClient);

		harness.DiskChecker.SetHasSpace(false);
		harness.FakePullClient.ReportProgress(new OllamaPullProgress("downloading", 50, 100));
		await WaitForState(harness.Coordinator, AiDownloadJobState.Failed);

		Assert.Equal(
			TranslationKeys.AiDownloadInsufficientDiskSpace,
			harness.Coordinator.CurrentSnapshot.ErrorMessageKey);
	}

	[Fact]
	public async Task Coordinator_ProgressUpdates_RaiseSnapshotChanged()
	{
		var harness = CreateHarness(blockPullUntilCancelled: true);
		var changes = new List<AiDownloadJobState>();
		harness.Coordinator.SnapshotChanged += snapshot => changes.Add(snapshot.State);

		harness.Coordinator.TryStart(GetTestModel(), false);
		await WaitForActivePull(harness.FakePullClient);
		harness.FakePullClient.ReportProgress(new OllamaPullProgress("downloading", 25, 100));

		for (var i = 0; i < 50; i++)
		{
			if (changes.Contains(AiDownloadJobState.Downloading))
			{
				return;
			}

			await Task.Delay(10);
		}

		Assert.Contains(AiDownloadJobState.Downloading, changes);
	}

	private static async Task WaitForState(
		AiModelDownloadCoordinator coordinator,
		AiDownloadJobState expected)
	{
		for (var i = 0; i < 200; i++)
		{
			if (coordinator.CurrentSnapshot.State == expected)
			{
				return;
			}

			await Task.Delay(10);
		}

		Assert.Equal(expected, coordinator.CurrentSnapshot.State);
	}

	private static async Task WaitForPullCompletion(TestHarness harness)
	{
		for (var i = 0; i < 200; i++)
		{
			var state = harness.Coordinator.CurrentSnapshot.State;
			if (harness.FakePullClient.PullCallCount > 0 &&
				state is AiDownloadJobState.Idle or AiDownloadJobState.Failed)
			{
				return;
			}

			if (state == AiDownloadJobState.Completed)
			{
				await WaitForState(harness.Coordinator, AiDownloadJobState.Idle);
				return;
			}

			await Task.Delay(10);
		}
	}

	private static AiModelCatalogEntry GetTestModel() =>
		AiModelCatalog.TryGetById("llama32-3b")!;

	private static string ExpectedUnreachableFailureKeyAfterInstallAttempt(int installCallCount) =>
		installCallCount > 0
			? TranslationKeys.AiSetupOllamaInstallFailed
			: TranslationKeys.AiSetupOllamaNotRunning;

	private static TestHarness CreateHarness(
		bool blockPullUntilCancelled = false,
		bool failPull = false)
	{
		var directory = Path.Combine(Path.GetTempPath(), "revitae-tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		var jobPath = Path.Combine(directory, "ai-download-job.json");
		var settingsPath = Path.Combine(directory, "ai-settings.json");
		var clock = new FakeClock(DateTimeOffset.UtcNow);
		var jobStorage = new AiDownloadJobStorage(jobPath, clock);
		var settingsStorage = new AiSettingsStorage(settingsPath);
		var diskChecker = new FakeDiskSpaceChecker(true);
		var ollamaProbe = new FakeOllamaRuntimeProbe();
		var ollamaInstaller = new FakeOllamaInstaller(ollamaProbe);
		var fakePull = new FakeOllamaPullClient(blockPullUntilCancelled, failPull);
		var modelDeleteClient = new FakeOllamaModelDeleteClient();

		var coordinator = new AiModelDownloadCoordinator(
			fakePull,
			modelDeleteClient,
			jobStorage,
			settingsStorage,
			diskChecker,
			ollamaProbe,
			ollamaInstaller,
			clock)
		{
			CompletedDwellDuration = TimeSpan.FromMilliseconds(10),
			ReachabilityOptions = OllamaReachabilityOptions.FastForTests,
		};

		return new TestHarness(
			coordinator,
			jobStorage,
			settingsStorage,
			diskChecker,
			ollamaProbe,
			ollamaInstaller,
			fakePull,
			modelDeleteClient,
			clock);
	}

	private sealed class TestHarness(
		AiModelDownloadCoordinator coordinator,
		AiDownloadJobStorage jobStorage,
		AiSettingsStorage settingsStorage,
		FakeDiskSpaceChecker diskChecker,
		FakeOllamaRuntimeProbe ollamaProbe,
		FakeOllamaInstaller ollamaInstaller,
		FakeOllamaPullClient fakePullClient,
		FakeOllamaModelDeleteClient modelDeleteClient,
		FakeClock clock)
	{
		public AiModelDownloadCoordinator Coordinator { get; } = coordinator;
		public AiDownloadJobStorage JobStorage { get; } = jobStorage;
		public AiSettingsStorage SettingsStorage { get; } = settingsStorage;
		public FakeDiskSpaceChecker DiskChecker { get; } = diskChecker;
		public FakeOllamaRuntimeProbe OllamaProbe { get; } = ollamaProbe;
		public FakeOllamaInstaller OllamaInstaller { get; } = ollamaInstaller;
		public FakeOllamaPullClient FakePullClient { get; } = fakePullClient;
		public FakeOllamaModelDeleteClient ModelDeleteClient { get; } = modelDeleteClient;
		public FakeClock Clock { get; } = clock;
	}

	private sealed class FakeDiskSpaceChecker(bool hasSpace) : IDiskSpaceChecker
	{
		private bool _hasSpace = hasSpace;

		public void SetHasSpace(bool hasSpace) => _hasSpace = hasSpace;

		public long? GetAvailableBytesForLocalData() => _hasSpace ? 100L * 1024 * 1024 * 1024 : 0;

		public bool HasSpaceForDownload(long approxDownloadBytes, double bufferFactor = 1.1) => _hasSpace;
	}

	private sealed class FakeOllamaInstaller : IOllamaInstaller
	{
		private readonly FakeOllamaRuntimeProbe? _linkedProbe;

		public FakeOllamaInstaller(FakeOllamaRuntimeProbe? linkedProbe = null)
		{
			_linkedProbe = linkedProbe;
		}

		public bool ShouldSucceed { get; set; } = true;
		public int InstallCallCount { get; private set; }

		public Task<OllamaInstallResult> EnsureInstalledAsync(
			IProgress<OllamaInstallProgress>? progress = null,
			CancellationToken cancellationToken = default)
		{
			InstallCallCount++;
			if (ShouldSucceed)
			{
				_linkedProbe?.MarkReachableAfterInstall();
				progress?.Report(new OllamaInstallProgress(OllamaInstallPhase.StartingEngine, null, null));
				return Task.FromResult(new OllamaInstallResult(true, null));
			}

			return Task.FromResult(new OllamaInstallResult(false, TranslationKeys.AiSetupOllamaInstallFailed));
		}
	}

	private sealed class FakeOllamaRuntimeProbe : IOllamaRuntimeProbe
	{
		public int FailuresBeforeSuccess { get; set; }
		public int? FailUntilProbeCount { get; set; }
		public bool AlwaysUnreachable { get; set; }
		public bool HangUntilCancelled { get; set; }
		public int ProbeCallCount { get; private set; }
		private int _hangStarted;

		public async Task WaitUntilHangStartedAsync()
		{
			for (var i = 0; i < 200; i++)
			{
				if (Volatile.Read(ref _hangStarted) > 0)
				{
					return;
				}

				await Task.Delay(10);
			}

			throw new InvalidOperationException("Probe did not enter hanging state.");
		}

		public void MarkReachableAfterInstall()
		{
			AlwaysUnreachable = false;
			FailuresBeforeSuccess = 0;
		}

		public async Task<OllamaRuntimeStatus> ProbeAsync(CancellationToken cancellationToken = default)
		{
			ProbeCallCount++;
			if (HangUntilCancelled)
			{
				Interlocked.Increment(ref _hangStarted);
				await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
			}

			if (AlwaysUnreachable)
			{
				return new OllamaRuntimeStatus(false, []);
			}

			if (FailUntilProbeCount is int failUntil && ProbeCallCount <= failUntil)
			{
				return new OllamaRuntimeStatus(false, []);
			}

			if (ProbeCallCount <= FailuresBeforeSuccess)
			{
				return new OllamaRuntimeStatus(false, []);
			}

			return new OllamaRuntimeStatus(true, []);
		}
	}

	private sealed class FakeOllamaPullClient : IOllamaPullClient
	{
		private readonly bool _failPull;
		private TaskCompletionSource? _blocker;
		private bool _blockUntilCancelled;

		public FakeOllamaPullClient(bool blockUntilCancelled, bool failPull)
		{
			_blockUntilCancelled = blockUntilCancelled;
			_failPull = failPull;
		}

		public bool BlockUntilCancelled
		{
			get => _blockUntilCancelled;
			set => _blockUntilCancelled = value;
		}

		public int PullCallCount { get; private set; }

		public TaskCompletionSource PullStarted { get; private set; } =
			new(TaskCreationOptions.RunContinuationsAsynchronously);

		public IProgress<OllamaPullProgress>? LastProgress { get; private set; }

		public void Reset()
		{
			PullCallCount = 0;
			PullStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		}

		public void ReportProgress(OllamaPullProgress progress) =>
			LastProgress?.Report(progress);

		public async Task<OllamaPullResult> PullAsync(
			string modelTag,
			IProgress<OllamaPullProgress>? progress,
			CancellationToken cancellationToken = default)
		{
			PullCallCount++;
			LastProgress = progress;
			PullStarted.TrySetResult();

			if (_failPull)
			{
				return new OllamaPullResult(OllamaPullOutcome.Failed, "HTTP 500");
			}

			progress?.Report(new OllamaPullProgress("downloading", 10, 100));

			if (_blockUntilCancelled)
			{
				_blocker = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
				using var registration = cancellationToken.Register(() => _blocker.TrySetResult());
				await _blocker.Task.ConfigureAwait(false);
				return new OllamaPullResult(OllamaPullOutcome.Cancelled, null);
			}

			progress?.Report(new OllamaPullProgress("success", 100, 100));
			return new OllamaPullResult(OllamaPullOutcome.Succeeded, null);
		}
	}

	private sealed class FakeOllamaModelDeleteClient : IOllamaModelDeleteClient
	{
		public int DeleteCallCount { get; private set; }

		public Task<bool> TryDeleteModelAsync(string modelTag, CancellationToken cancellationToken = default)
		{
			DeleteCallCount++;
			return Task.FromResult(true);
		}
	}
}
