using ReVitae.Core.Ai;
using ReVitae.Core.Ai.Download;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Ai.Download;

public sealed class AiDownloadDisplayProgressLiveSnapshotTests
{
	[Fact]
	public void Update_LiveGemmaPullSnapshot_ShowsLayerPercent()
	{
		var tracker = new AiDownloadDisplayProgress();
		var snapshot = new AiDownloadJobSnapshot(
			Guid.Parse("e2cf6056-4202-4337-a9bd-1deb6f359099"),
			"gemma2-2b",
			"gemma2:2b",
			TranslationKeys.AiModelGemma2_2bName,
			AiDownloadJobState.Downloading,
			false,
			75_826_064,
			1_629_509_152,
			"pulling 7462734796d6",
			DateTimeOffset.UtcNow,
			DateTimeOffset.UtcNow,
			null);

		var display = tracker.Update(snapshot);

		Assert.False(display.IsIndeterminate);
		Assert.Equal(4, display.Percent);
	}
}
