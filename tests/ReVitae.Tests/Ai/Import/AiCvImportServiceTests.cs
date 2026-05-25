using ReVitae.Core.Ai.Cv;
using ReVitae.Core.Ai.Import;
using ReVitae.Core.Ai.Providers;
using ReVitae.Core.Ai.Providers.Chat;
using ReVitae.Core.Import;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Ai.Import;

public sealed class AiCvImportServiceTests
{
	[Fact]
	public async Task ImportAsync_NoBackend_ReturnsNoBackendKey()
	{
		var service = new AiCvImportService(
			AiImportTestHelpers.CreateConfigService(AiSettingsDocument.Empty),
			new AiBackendRuntimeResolver());
		var request = CreateRequest(SampleCvText.JohnDoeMultiSection());
		var outcome = await service.ImportAsync(request);
		Assert.False(outcome.Succeeded);
		Assert.Equal(TranslationKeys.AiCvNoBackendConfigured, outcome.ErrorMessageKey);
	}

	[Fact]
	public async Task ImportAsync_MockRuntime_CompactPlanCompletesWithReview()
	{
		var personal = """{"personalInformation":{"firstName":"John","lastName":"Doe","email":"john@example.com"}}""";
		var work = """{"workExperience":[{"company":"Acme","jobTitle":"Engineer","startYear":2020,"startMonth":1}]}""";
		var runtime = new AiImportTestHelpers.SequenceRuntime([personal, work, "{}", "{}", "{}"]);
		var config = AiImportTestHelpers.CreateConfigService(AiImportTestHelpers.LocalSettings("gemma2-2b"));
		var service = new AiCvImportService(config, new AiImportTestHelpers.FixedRuntimeResolver(runtime));
		var text = SampleCvText.JohnDoeMultiSection();
		var segmentation = CvSectionSegmenter.Segment(CvTextNormalizer.Normalize(text));
		var plan = service.CreatePlan(text, segmentation);
		Assert.True(plan.TotalBatchCount >= 5);

		var request = new AiCvImportRequest(
			text,
			segmentation,
			null,
			plan,
			"en",
			AiCvImportMergeMode.ReplaceAll,
			[],
			null);

		var outcome = await service.ImportAsync(request, runtimeOverride: runtime);
		Assert.True(outcome.Succeeded);
		Assert.NotNull(outcome.Result);
		Assert.NotNull(outcome.ReviewSummary);
	}

	[Fact]
	public async Task ImportAsync_CancelMidWay_ThrowsOperationCanceled()
	{
		var runtime = new SlowImportRuntime();
		var config = AiImportTestHelpers.CreateConfigService(AiImportTestHelpers.LocalSettings("gemma2-2b"));
		var service = new AiCvImportService(config, new AiImportTestHelpers.FixedRuntimeResolver(runtime));
		using var cts = new CancellationTokenSource();
		cts.Cancel();
		var request = CreateRequest(SampleCvText.JohnDoeMultiSection(), cts.Token);
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ImportAsync(request, runtimeOverride: runtime));
	}

	[Fact]
	public async Task ImportAsync_UnreachableBackendBeforeStart_ReturnsError()
	{
		var runtime = new AiImportTestHelpers.FailingRuntime(TranslationKeys.AiSetupProviderRateLimited);
		var config = AiImportTestHelpers.CreateConfigService(AiImportTestHelpers.OnlineSettings("openai", "gpt-4o-mini"));
		var service = new AiCvImportService(config, new AiImportTestHelpers.FixedRuntimeResolver(runtime));
		var outcome = await service.ImportAsync(CreateRequest(SampleCvText.JohnDoeMultiSection()), runtimeOverride: runtime);
		Assert.False(outcome.Succeeded);
	}

	[Fact]
	public void GetBackendStatus_LocalConfigured_IsAvailable()
	{
		var service = new AiCvImportService(
			AiImportTestHelpers.CreateConfigService(AiImportTestHelpers.LocalSettings("gemma2-2b")),
			new AiBackendRuntimeResolver());
		var status = service.GetBackendStatus();
		Assert.Equal(AiBackendKind.Local, status.Kind);
		Assert.True(status.IsAvailable);
	}

	private static AiCvImportRequest CreateRequest(string text, CancellationToken token = default)
	{
		var segmentation = CvSectionSegmenter.Segment(CvTextNormalizer.Normalize(text));
		var plan = AiImportBatchPlanResolver.BuildPlan(
			text,
			segmentation,
			AiImportBatchProfile.Compact);
		return new AiCvImportRequest(
			text,
			segmentation,
			null,
			plan,
			"en",
			AiCvImportMergeMode.ReplaceAll,
			[],
			null,
			token);
	}

	private sealed class SlowImportRuntime : IAiBackendRuntime
	{
		public AiBackendKind Kind => AiBackendKind.Local;

		public string DescribeActiveBackend(ReVitae.Core.Localization.AppLocalizer localizer) => "Slow";

		public async Task<AiChatCompletionResult> CompleteAsync(
			AiCvPromptMessages messages,
			CancellationToken cancellationToken = default)
		{
			await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
			return new AiChatCompletionResult(true, "{}", null);
		}
	}
}
