using ReVitae.Core.Ai;
using ReVitae.Core.Ai.Download;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Ai;

public sealed class AiDownloadJobStorageEdgeCaseTests : IDisposable
{
	private readonly string _root;

	public AiDownloadJobStorageEdgeCaseTests()
	{
		_root = Path.Combine(Path.GetTempPath(), "revitae-ai-download-job", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_root);
	}

	[Fact]
	public void TryLoad_MissingFile_ReturnsNull()
	{
		var storage = CreateStorage();

		Assert.Null(storage.TryLoad());
	}

	[Fact]
	public void TryLoad_CorruptJson_QuarantinesAndReturnsNull()
	{
		var path = Path.Combine(_root, "job.json");
		File.WriteAllText(path, "{ not-json");
		var storage = new AiDownloadJobStorage(path, new FakeClock(DateTimeOffset.UtcNow));

		Assert.Null(storage.TryLoad());
		Assert.False(File.Exists(path));
		Assert.True(File.Exists(path + ".bak"));
	}

	[Fact]
	public void Save_AndTryLoad_RoundTripsSnapshot()
	{
		var clock = new FakeClock(DateTimeOffset.UtcNow);
		var storage = new AiDownloadJobStorage(Path.Combine(_root, "job.json"), clock);
		var snapshot = CreateSnapshot(clock.UtcNow);

		storage.Save(snapshot, forceFlush: true);

		var loaded = storage.TryLoad();
		Assert.NotNull(loaded);
		Assert.Equal(snapshot.JobId, loaded!.JobId);
		Assert.Equal(snapshot.State, loaded.State);
	}

	[Fact]
	public void Save_ProgressOnlyUpdate_RespectsThrottleUnlessForced()
	{
		var clock = new FakeClock(DateTimeOffset.UtcNow);
		var path = Path.Combine(_root, "progress.json");
		var storage = new AiDownloadJobStorage(path, clock);
		var snapshot = CreateSnapshot(clock.UtcNow) with { CompletedBytes = 10 };

		storage.Save(snapshot, forceFlush: true);
		snapshot = snapshot with { CompletedBytes = 20 };
		storage.Save(snapshot);
		storage.Save(snapshot);

		var loaded = storage.TryLoad();
		Assert.Equal(10, loaded!.CompletedBytes);
		storage.Save(snapshot, forceFlush: true);
		loaded = storage.TryLoad();
		Assert.Equal(20, loaded!.CompletedBytes);
	}

	[Fact]
	public void Delete_RemovesPersistedJob()
	{
		var storage = CreateStorage();
		storage.Save(CreateSnapshot(DateTimeOffset.UtcNow), forceFlush: true);

		storage.Delete();

		Assert.Null(storage.TryLoad());
	}

	[Fact]
	public void ShouldWriteProgressNow_RespectsMaxWritesPerSecond()
	{
		var clock = new FakeClock(DateTimeOffset.UtcNow);
		var storage = new AiDownloadJobStorage(Path.Combine(_root, "throttle.json"), clock);

		Assert.True(storage.ShouldWriteProgressNow());
		storage.Save(CreateSnapshot(clock.UtcNow), forceFlush: true);
		Assert.False(storage.ShouldWriteProgressNow());
		clock.Advance(TimeSpan.FromMilliseconds(600));
		Assert.True(storage.ShouldWriteProgressNow());
	}

	private AiDownloadJobStorage CreateStorage() =>
		new(Path.Combine(_root, Guid.NewGuid() + ".json"), new FakeClock(DateTimeOffset.UtcNow));

	private static AiDownloadJobSnapshot CreateSnapshot(DateTimeOffset now) =>
		new(
			Guid.Parse("11111111-2222-3333-4444-555555555555"),
			"llama31-8b",
			"llama3.1:8b-instruct",
			TranslationKeys.AiModelLlama31_8bName,
			AiDownloadJobState.Downloading,
			false,
			100,
			1000,
			"downloading",
			now,
			now,
			null);

	public void Dispose()
	{
		try
		{
			if (Directory.Exists(_root))
			{
				Directory.Delete(_root, recursive: true);
			}
		}
		catch
		{
		}
	}

	private sealed class FakeClock(DateTimeOffset initial) : IClock
	{
		public DateTimeOffset UtcNow { get; private set; } = initial;

		public void Advance(TimeSpan delta) => UtcNow = UtcNow.Add(delta);
	}
}
