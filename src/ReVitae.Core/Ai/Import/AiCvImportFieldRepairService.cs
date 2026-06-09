using System.Diagnostics;
using ReVitae.Core.Ai.Cv;
using ReVitae.Core.Ai.Providers;
using ReVitae.Core.Import;
using ReVitae.Core.Localization;

namespace ReVitae.Core.Ai.Import;

/// <summary>
/// Targeted field repair (045 B.2): instead of re-extracting whole sections, sends only the
/// low-confidence fields (capped, lowest-confidence first — 045 C.9) plus the source text and
/// returns corrected values. Adds/removes nothing; a field the model leaves unchanged or empty
/// keeps its current value.
/// </summary>
public sealed class AiCvImportFieldRepairService
{
	private readonly AiProviderConfigService _configService;
	private readonly IAiBackendRuntimeResolver _runtimeResolver;

	public AiCvImportFieldRepairService()
		: this(new AiProviderConfigService(), new AiBackendRuntimeResolver())
	{
	}

	public AiCvImportFieldRepairService(
		AiProviderConfigService configService,
		IAiBackendRuntimeResolver runtimeResolver)
	{
		_configService = configService;
		_runtimeResolver = runtimeResolver;
	}

	public async Task<AiCvImportRepairOutcome> RepairImportFieldsAsync(
		CvTextImportAttempt attempt,
		IReadOnlyList<AiImportFieldRepairTarget> fields,
		string uiCulture,
		CancellationToken cancellationToken = default,
		IAiBackendRuntime? runtimeOverride = null)
	{
		var requested = fields.Count;
		var selected = AiImportFieldRepairPlanner.SelectTargets(fields, out var dropped);
		var sent = selected.Count;

		var resolve = _runtimeResolver.Resolve(_configService.CurrentSettings, _configService.SecretStorage);
		var runtime = runtimeOverride ?? resolve.Runtime;
		if (runtime is null)
		{
			return AiCvImportRepairOutcome.Fail(
				resolve.ErrorMessageKey ?? TranslationKeys.ImportAiNoBackend,
				requested);
		}

		if (sent == 0)
		{
			return AiCvImportRepairOutcome.Fail(TranslationKeys.ImportAiFailed, requested);
		}

		var descriptor = new AiCvBackendDescriptor(runtime.Kind, runtime.DescribeActiveBackend(AppLocalizer.FromSystemCulture()));
		AiCvDiagnosticsLogger.LogRepairStart(requested, sent, dropped, uiCulture);

		var repairs = new List<AiImportFieldRepairResult>();
		var batchesFailed = 0;
		var groups = AiImportFieldRepairPlanner.GroupBySection(selected);
		var stopwatch = Stopwatch.StartNew();

		try
		{
			foreach (var group in groups)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var messages = AiCvImportRepairPromptBuilder.Build(
					group[0].Section,
					group,
					attempt.NormalizedText,
					uiCulture);

				var completion = await runtime.CompleteAsync(messages, cancellationToken).ConfigureAwait(false);
				if (!completion.Succeeded || string.IsNullOrWhiteSpace(completion.Content))
				{
					batchesFailed++;
					AppendUnchanged(repairs, group);
					AiCvDiagnosticsLogger.LogParseError(AiCvDiagnosticsLogger.RepairStep, completion.ErrorMessage ?? "empty");
					continue;
				}

				var map = AiCvImportRepairResponseParser.Parse(completion.Content);
				for (var i = 0; i < group.Count; i++)
				{
					var target = group[i];
					var repaired = map.TryGetValue(i + 1, out var value) && !string.IsNullOrWhiteSpace(value)
						? value.Trim()
						: target.CurrentValue;
					repairs.Add(new AiImportFieldRepairResult(target, repaired));
				}
			}
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			return AiCvImportRepairOutcome.CancelledResult(requested, sent, dropped);
		}

		stopwatch.Stop();
		var succeeded = batchesFailed < groups.Count;
		AiCvDiagnosticsLogger.LogRepairResult(succeeded, repairs.Count(r => r.Changed), batchesFailed, stopwatch.ElapsedMilliseconds);

		if (!succeeded)
		{
			return AiCvImportRepairOutcome.Fail(TranslationKeys.ImportAiFailed, requested) with
			{
				SentFieldCount = sent,
				DroppedFieldCount = dropped,
				BatchesFailed = batchesFailed,
				BackendUsed = descriptor,
			};
		}

		return new AiCvImportRepairOutcome(
			true,
			repairs,
			requested,
			sent,
			dropped,
			batchesFailed,
			null,
			descriptor);
	}

	private static void AppendUnchanged(
		List<AiImportFieldRepairResult> repairs,
		IReadOnlyList<AiImportFieldRepairTarget> group)
	{
		foreach (var target in group)
		{
			repairs.Add(new AiImportFieldRepairResult(target, target.CurrentValue));
		}
	}
}
