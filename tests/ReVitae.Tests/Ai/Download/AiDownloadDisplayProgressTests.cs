using ReVitae.Core.Ai;
using ReVitae.Core.Ai.Download;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Ai.Download;

public sealed class AiDownloadDisplayProgressTests
{
	[Fact]
	public void Update_KeepsPercentMonotonicAcrossLayerResets()
	{
		var tracker = new AiDownloadDisplayProgress();
		var jobId = Guid.NewGuid();
		var snapshot = CreateSnapshot(jobId, completed: 50, total: 100, status: "layer 1");

		Assert.Equal(50, tracker.Update(snapshot).Percent);

		snapshot = snapshot with { CompletedBytes = 10, TotalBytes = 200, StatusText = "layer 2" };
		Assert.Equal(50, tracker.Update(snapshot).Percent);

		snapshot = snapshot with { CompletedBytes = 180, TotalBytes = 200 };
		Assert.Equal(90, tracker.Update(snapshot).Percent);
	}

	[Fact]
	public void Update_ResetsHighWaterWhenEnginePhaseEnds()
	{
		var tracker = new AiDownloadDisplayProgress();
		var jobId = Guid.NewGuid();
		var engine = CreateSnapshot(
			jobId,
			completed: 80,
			total: 100,
			status: AiDownloadStatus.FromTranslationKey(TranslationKeys.AiDownloadDownloadingEngine));

		Assert.Equal(80, tracker.Update(engine).Percent);

		var model = engine with
		{
			CompletedBytes = 10,
			TotalBytes = 100,
			StatusText = AiDownloadStatus.FromTranslationKey(TranslationKeys.AiDownloadPullingModel),
		};

		Assert.Equal(10, tracker.Update(model).Percent);
	}

	[Fact]
	public void Update_IgnoresEngineBytesCarriedIntoModelPhaseAtOneHundred()
	{
		var tracker = new AiDownloadDisplayProgress();
		var jobId = Guid.NewGuid();
		const long sharedTotal = 167_608_636L;
		var engine = CreateSnapshot(
			jobId,
			completed: sharedTotal,
			total: sharedTotal,
			status: AiDownloadStatus.FromTranslationKey(TranslationKeys.AiDownloadDownloadingEngine));

		Assert.Equal(100, tracker.Update(engine).Percent);

		var model = engine with
		{
			StatusText = AiDownloadStatus.FromTranslationKey(TranslationKeys.AiDownloadPullingModel),
		};

		Assert.True(tracker.Update(model).IsIndeterminate);
		Assert.Null(tracker.Update(model).Percent);
	}

	[Fact]
	public void Update_CompletedAlwaysShowsOneHundred()
	{
		var tracker = new AiDownloadDisplayProgress();
		var snapshot = CreateSnapshot(Guid.NewGuid(), 10, 100, "pulling") with
		{
			State = AiDownloadJobState.Completed,
		};

		Assert.Equal(100, tracker.Update(snapshot).Percent);
	}

	private static AiDownloadJobSnapshot CreateSnapshot(
		Guid jobId,
		long? completed,
		long? total,
		string status) =>
		new(
			jobId,
			"llama32-3b",
			"llama3.2:3b-instruct",
			TranslationKeys.AiModelLlama32_3bName,
			AiDownloadJobState.Downloading,
			false,
			completed,
			total,
			status,
			DateTimeOffset.UtcNow,
			DateTimeOffset.UtcNow,
			null);
}
