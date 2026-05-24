using ReVitae.Core.Ai;

namespace ReVitae.Tests.Ai;

public sealed class AiSettingsStorageTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _settingsPath;

    public AiSettingsStorageTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "revitae-ai-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        _settingsPath = Path.Combine(_tempDirectory, "ai-settings.json");
    }

    [Fact]
    public void AiSettingsStorage_RoundTrips()
    {
        var storage = new AiSettingsStorage(_settingsPath);
        var snapshot = new AiSettingsSnapshot(
            "medium-instruct",
            "llama3.1:8b-instruct",
            new DateTimeOffset(2026, 5, 21, 12, 0, 0, TimeSpan.Zero));

        storage.Save(snapshot);
        var loaded = storage.TryLoad();

        Assert.NotNull(loaded);
        Assert.Equal(snapshot.SelectedModelId, loaded!.SelectedModelId);
        Assert.Equal(snapshot.OllamaModelTag, loaded.OllamaModelTag);
        Assert.Equal(snapshot.DownloadedAtUtc, loaded.DownloadedAtUtc);
    }

    [Fact]
    public void DiskSpaceChecker_HasSpaceForDownload_WhenEnoughFreeSpace()
    {
        var checker = new FakeDiskSpaceChecker(10L * 1024 * 1024 * 1024);

        Assert.True(checker.HasSpaceForDownload(2L * 1024 * 1024 * 1024));
    }

    [Fact]
    public void DiskSpaceChecker_BlocksWhenInsufficient()
    {
        var checker = new FakeDiskSpaceChecker(1L * 1024 * 1024 * 1024);

        Assert.False(checker.HasSpaceForDownload(2L * 1024 * 1024 * 1024));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private sealed class FakeDiskSpaceChecker(long availableBytes) : IDiskSpaceChecker
    {
        public long? GetAvailableBytesForLocalData() => availableBytes;

        public bool HasSpaceForDownload(long approxDownloadBytes, double bufferFactor = 1.1) =>
            availableBytes >= (long)(approxDownloadBytes * bufferFactor);
    }
}
