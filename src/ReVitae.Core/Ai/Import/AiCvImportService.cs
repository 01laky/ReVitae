using System.Diagnostics;
using System.Text.Json.Nodes;
using ReVitae.Core.Ai.Cv;
using ReVitae.Core.Ai.Providers;
using ReVitae.Core.Import;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Ai.Import;

public sealed class AiCvImportService
{
	private readonly AiProviderConfigService _configService;
	private readonly IAiBackendRuntimeResolver _runtimeResolver;

	public AiCvImportService()
		: this(new AiProviderConfigService(), new AiBackendRuntimeResolver())
	{
	}

	public AiCvImportService(
		AiProviderConfigService configService,
		IAiBackendRuntimeResolver runtimeResolver)
	{
		_configService = configService;
		_runtimeResolver = runtimeResolver;
	}

	public AiImportBatchPlan CreatePlan(string normalizedText, CvSegmentationResult segmentation)
	{
		var profile = AiImportBatchPlanResolver.Resolve(_configService.CurrentSettings);
		return AiImportBatchPlanResolver.BuildPlan(normalizedText, segmentation, profile);
	}

	public AiCvBackendStatus GetBackendStatus()
	{
		var settings = _configService.CurrentSettings;
		var resolve = _runtimeResolver.Resolve(settings, _configService.SecretStorage);
		var snapshot = AiActiveBackendPresentation.GetSnapshot(settings);
		return new AiCvBackendStatus(
			snapshot.Kind,
			resolve.IsAvailable,
			resolve.UnavailableReason,
			resolve.ErrorMessageKey,
			snapshot.Kind == AiBackendKind.None ? null : snapshot);
	}

	public async Task<AiCvImportOutcome> ImportAsync(
		AiCvImportRequest request,
		IProgress<AiCvImportProgress>? progress = null,
		IAiBackendRuntime? runtimeOverride = null)
	{
		var resolve = _runtimeResolver.Resolve(_configService.CurrentSettings, _configService.SecretStorage);
		var runtime = runtimeOverride ?? resolve.Runtime;
		if (runtime is null)
		{
			return new AiCvImportOutcome(
				false,
				null,
				null,
				0,
				0,
				resolve.ErrorMessageKey ?? TranslationKeys.ImportAiNoBackend,
				null,
				false);
		}

		var accumulated = AiCvImportResultMerger.CreateEmptyDocument();
		var completed = 0;
		var failed = 0;
		string? lastParseError = null;

		AiImportDiagnosticsLogger.LogSessionStart(
			request.Plan.Profile.ProfileId,
			request.Plan.TotalBatchCount,
			request.UiCulture);

		foreach (var batch in request.Plan.Batches)
		{
			request.CancellationToken.ThrowIfCancellationRequested();

			progress?.Report(new AiCvImportProgress(
				completed,
				request.Plan.TotalBatchCount,
				batch.Phase,
				batch.BatchIndex,
				batch.BatchCountInPhase,
				GetPhaseLabelKey(batch.Phase)));

			var carryForward = AiImportCarryForwardSummaryBuilder.Build(
				accumulated,
				request.Plan.Profile.MaxCarryForwardSummaryChars);
			var messages = AiCvImportPromptBuilder.Build(
				batch.Phase,
				batch.SliceText,
				carryForward,
				request.UiCulture);

			var stopwatch = Stopwatch.StartNew();
			var raw = await CompleteBatchAsync(runtime, messages, request.CancellationToken);
			stopwatch.Stop();

			var parsed = AiCvImportResponseParser.TryParse(raw ?? string.Empty, batch.Phase);
			if (!parsed.Success && parsed.ShouldRetry)
			{
				var retryMessages = new AiCvPromptMessages(
					messages.SystemPrompt,
					messages.UserPrompt + "\nReturn valid JSON only.");
				raw = await CompleteBatchAsync(runtime, retryMessages, request.CancellationToken);
				parsed = AiCvImportResponseParser.TryParse(raw ?? string.Empty, batch.Phase);
			}

			if (parsed.Success && parsed.Fragment is not null)
			{
				AiCvImportResultMerger.MergeFragment(accumulated, parsed.Fragment);
				completed++;
			}
			else
			{
				failed++;
				lastParseError = parsed.SanitizedError;
				AiImportDiagnosticsLogger.LogParseError(parsed.SanitizedError ?? "unknown");
			}

			AiImportDiagnosticsLogger.LogBatch(
				batch.Phase,
				batch.BatchIndex,
				batch.BatchCountInPhase,
				request.Plan.Profile.ProfileId,
				batch.SliceText.Length,
				carryForward.Length,
				raw?.Length ?? 0,
				parsed.Success,
				parsed.ShouldRetry,
				stopwatch.ElapsedMilliseconds);
		}

		var finalResult = AiCvImportResultMerger.BuildFinalResult(
			accumulated,
			null,
			AiCvImportMergeMode.ReplaceAll,
			request.AcquisitionWarnings,
			failed,
			request.ExistingProfilePhotoPath);

		AiImportDiagnosticsLogger.LogSessionEnd(finalResult.Success, completed, failed);

		if (!finalResult.Success)
		{
			return new AiCvImportOutcome(
				false,
				null,
				null,
				completed,
				failed,
				TranslationKeys.ImportAiFailed,
				lastParseError,
				false);
		}

		var review = AiCvImportReviewSummaryBuilder.Build(request.DeterministicBaseline, finalResult);
		return new AiCvImportOutcome(
			true,
			finalResult,
			review,
			completed,
			failed,
			null,
			lastParseError,
			false);
	}

	private static async Task<string?> CompleteBatchAsync(
		IAiBackendRuntime runtime,
		AiCvPromptMessages messages,
		CancellationToken cancellationToken)
	{
		var result = await runtime.CompleteAsync(messages, cancellationToken);
		return result.Succeeded ? result.Content : null;
	}

	private static string GetPhaseLabelKey(AiImportPhase phase) =>
		phase switch
		{
			AiImportPhase.Personal => TranslationKeys.ImportAiPhasePersonal,
			AiImportPhase.Work => TranslationKeys.ImportAiPhaseWork,
			AiImportPhase.Education => TranslationKeys.ImportAiPhaseEducation,
			AiImportPhase.Skills or AiImportPhase.SkillsAndLanguages => TranslationKeys.ImportAiPhaseSkills,
			AiImportPhase.Languages => TranslationKeys.ImportAiPhaseLanguages,
			AiImportPhase.Certificates => TranslationKeys.ImportAiPhaseCertificates,
			AiImportPhase.Projects => TranslationKeys.ImportAiPhaseProjects,
			AiImportPhase.Links => TranslationKeys.ImportAiPhaseLinks,
			AiImportPhase.Additional => TranslationKeys.ImportAiPhaseAdditional,
			_ => TranslationKeys.ImportAiProgressTitle,
		};
}
