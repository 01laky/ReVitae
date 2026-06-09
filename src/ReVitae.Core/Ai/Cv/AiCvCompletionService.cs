using System.Diagnostics;
using ReVitae.Core.Ai.Providers;
using ReVitae.Core.Export;
using ReVitae.Core.Import;
using ReVitae.Core.Localization;
using ReVitae.Core.Quality;

namespace ReVitae.Core.Ai.Cv;

public sealed class AiCvCompletionService
{
	private readonly AiProviderConfigService _configService;
	private readonly IAiBackendRuntimeResolver _runtimeResolver;
	private readonly AiCvAdvisorCache _advisorCache;

	public AiCvCompletionService()
		: this(new AiProviderConfigService(), new AiBackendRuntimeResolver())
	{
	}

	public AiCvCompletionService(
		AiProviderConfigService configService,
		IAiBackendRuntimeResolver runtimeResolver,
		AiCvAdvisorCache? advisorCache = null)
	{
		_configService = configService;
		_runtimeResolver = runtimeResolver;
		_advisorCache = advisorCache ?? new AiCvAdvisorCache();
	}

	public AiSettingsDocument CurrentSettings => _configService.CurrentSettings;

	public IAiSecretStorage SecretStorage => _configService.SecretStorage;

	public AiCvBackendStatus GetBackendStatus()
	{
		var settings = _configService.CurrentSettings;
		var snapshot = AiActiveBackendPresentation.GetSnapshot(settings);
		var resolve = _runtimeResolver.Resolve(settings, _configService.SecretStorage);

		return new AiCvBackendStatus(
			snapshot.Kind,
			resolve.IsAvailable,
			resolve.UnavailableReason,
			resolve.ErrorMessageKey,
			snapshot.Kind == AiBackendKind.None ? null : snapshot);
	}

	public bool IsQualityHintSupported(string qualityHintId) =>
		AiCvTaskRegistry.SupportsQualityHint(qualityHintId);

	/// <summary>True when the hint maps to a single-value task (039 suggestion modal).</summary>
	public bool IsSingleValueQualityHint(string qualityHintId)
	{
		var task = AiCvTaskRegistry.TryGetTaskForQualityHint(qualityHintId);
		return task is not null && !AiCvTaskRegistry.ProducesAdviceList(task.Value);
	}

	/// <summary>True when the hint maps to an advice-list task (advisor modal, 045 A.3).</summary>
	public bool IsAdviceQualityHint(string qualityHintId)
	{
		var task = AiCvTaskRegistry.TryGetTaskForQualityHint(qualityHintId);
		return task is not null && AiCvTaskRegistry.ProducesAdviceList(task.Value);
	}

	public async Task<AiCvCompletionResult> CompleteForQualityHintAsync(
		CvExportSourceData snapshot,
		CvQualityHint hint,
		string uiCulture,
		AiCvTargetContext? targetContext = null,
		CancellationToken cancellationToken = default)
	{
		var task = AiCvTaskRegistry.TryGetTaskForQualityHint(hint.Id);
		if (task is null || AiCvTaskRegistry.ProducesAdviceList(task.Value))
		{
			// Advice-list hints must go through AdviseForQualityHintAsync.
			return Fail(TranslationKeys.AiCvTaskFailed, null);
		}

		var resolve = _runtimeResolver.Resolve(_configService.CurrentSettings, _configService.SecretStorage);
		if (!resolve.IsAvailable || resolve.Runtime is null)
		{
			return Fail(resolve.ErrorMessageKey ?? TranslationKeys.AiCvNoBackendConfigured, null);
		}

		var context = BuildContext(task.Value, snapshot, hint) with { TargetContext = targetContext };
		var culture = ResolveTaskCulture(task.Value, context.CurrentText, uiCulture);
		var messages = AiCvPromptBuilder.Build(task.Value, context, culture);

		try
		{
			var completion = await resolve.Runtime
				.CompleteAsync(messages, cancellationToken)
				.ConfigureAwait(false);

			if (!completion.Succeeded)
			{
				return Fail(completion.ErrorMessage ?? TranslationKeys.AiCvTaskFailed, resolve.Runtime);
			}

			var parsed = AiCvResponseParser.Parse(completion.Content ?? string.Empty, task.Value);
			var guard = AiCvTaskRegistry.IsRewriteTask(task.Value)
				? AiCvEntityGuard.Inspect(context.CurrentText, parsed)
				: null;

			return new AiCvCompletionResult(
				true,
				parsed,
				null,
				CreateDescriptor(resolve.Runtime),
				EntityGuard: guard);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			return new AiCvCompletionResult(false, null, null, null, Cancelled: true);
		}
		catch (AiCvResponseParseException ex)
		{
			return Fail(ex.ErrorMessageKey, resolve.Runtime);
		}
	}

	/// <summary>Advice-list completion for a supported quality hint (045 A.3).</summary>
	public Task<AiCvAdvisorResult> AdviseForQualityHintAsync(
		CvExportSourceData snapshot,
		CvQualityHint hint,
		string uiCulture,
		AiCvTargetContext? targetContext = null,
		CancellationToken cancellationToken = default)
	{
		var task = AiCvTaskRegistry.TryGetTaskForQualityHint(hint.Id);
		var section = hint.Section ?? CvImportSectionId.Skills;
		if (task is null || !AiCvTaskRegistry.ProducesAdviceList(task.Value))
		{
			return Task.FromResult(AiCvAdvisorResult.Fail(TranslationKeys.AiCvTaskFailed, section));
		}

		var context = BuildAdviceContext(task.Value, section, snapshot, targetContext);
		return RunAdviceTaskAsync(task.Value, section, context, uiCulture, useCache: false, cancellationToken);
	}

	/// <summary>Suggest measurable results for a work entry (045 C.2).</summary>
	public Task<AiCvAdvisorResult> SuggestMeasurableResultsAsync(
		CvExportSourceData snapshot,
		string workEntryId,
		string uiCulture,
		AiCvTargetContext? targetContext = null,
		CancellationToken cancellationToken = default)
	{
		var entry = snapshot.WorkExperience.FirstOrDefault(e => string.Equals(e.Id, workEntryId, StringComparison.Ordinal));
		var context = new AiCvCompletionContext(
			AiCvTaskKind.SuggestMeasurableResults,
			entry?.Description ?? string.Empty,
			entry?.JobTitle,
			entry?.Company,
			Section: CvImportSectionId.WorkExperience,
			TargetContext: targetContext);
		return RunAdviceTaskAsync(
			AiCvTaskKind.SuggestMeasurableResults,
			CvImportSectionId.WorkExperience,
			context,
			uiCulture,
			useCache: false,
			cancellationToken);
	}

	/// <summary>
	/// Proactive per-section advisor (045 A.2). Returns 1–4 review-only suggestions for the
	/// section, cached by content (C.8); honors the min-content gate is the caller's job
	/// (the advisor still runs if invoked).
	/// </summary>
	public Task<AiCvAdvisorResult> AdviseSectionAsync(
		CvExportSourceData snapshot,
		CvImportSectionId section,
		string uiCulture,
		AiCvTargetContext? targetContext = null,
		CancellationToken cancellationToken = default)
	{
		var content = AiCvSectionContent.Describe(section, snapshot);
		var isEmpty = AiCvSectionContent.Measure(section, snapshot).IsEmpty;
		var context = new AiCvCompletionContext(
			AiCvTaskKind.SectionAdvisor,
			content,
			Section: section,
			SectionContent: content,
			SectionIsEmpty: isEmpty,
			TargetContext: targetContext);

		return RunAdviceTaskAsync(AiCvTaskKind.SectionAdvisor, section, context, uiCulture, useCache: true, cancellationToken);
	}

	private async Task<AiCvAdvisorResult> RunAdviceTaskAsync(
		AiCvTaskKind task,
		CvImportSectionId section,
		AiCvCompletionContext context,
		string uiCulture,
		bool useCache,
		CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return AiCvAdvisorResult.CancelledResult(section);
		}

		var content = context.SectionContent ?? context.CurrentText;
		var cacheKey = useCache
			? AiCvAdvisorCache.ComputeKey(section, content, context.TargetContext, uiCulture)
			: null;

		if (cacheKey is not null && _advisorCache.TryGet(cacheKey, out var cached))
		{
			AiCvDiagnosticsLogger.LogAdvisor(section, "cache", content.Length, context.TargetContext?.HasValue ?? false, uiCulture, cacheHit: true);
			return cached;
		}

		var resolve = _runtimeResolver.Resolve(_configService.CurrentSettings, _configService.SecretStorage);
		if (!resolve.IsAvailable || resolve.Runtime is null)
		{
			return AiCvAdvisorResult.Fail(resolve.ErrorMessageKey ?? TranslationKeys.AiCvNoBackendConfigured, section);
		}

		var descriptor = CreateDescriptor(resolve.Runtime);
		AiCvDiagnosticsLogger.LogAdvisor(section, descriptor.Label, content.Length, context.TargetContext?.HasValue ?? false, uiCulture, cacheHit: false);

		// Advice text is read by the user → UI culture (045 C.4).
		var messages = AiCvPromptBuilder.Build(task, context, uiCulture);
		var stopwatch = Stopwatch.StartNew();

		try
		{
			var completion = await resolve.Runtime
				.CompleteAsync(messages, cancellationToken)
				.ConfigureAwait(false);

			if (!completion.Succeeded)
			{
				return AiCvAdvisorResult.Fail(completion.ErrorMessage ?? TranslationKeys.AiCvTaskFailed, section, descriptor);
			}

			var advice = AiCvResponseParser.ParseAdviceList(completion.Content ?? string.Empty);
			var suggestions = advice
				.Select(a => new AiCvAdvisorSuggestion(a.Advice, ApplyTarget: null, ApplyValue: null, a.Rationale))
				.ToList();

			stopwatch.Stop();
			AiCvDiagnosticsLogger.LogAdvisorResult(section, true, suggestions.Count, 0, stopwatch.ElapsedMilliseconds);

			var result = new AiCvAdvisorResult(true, suggestions, null, descriptor, section);
			if (cacheKey is not null)
			{
				_advisorCache.Set(cacheKey, result);
			}

			return result;
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			return AiCvAdvisorResult.CancelledResult(section);
		}
		catch (AiCvResponseParseException ex)
		{
			AiCvDiagnosticsLogger.LogParseError(AiCvDiagnosticsLogger.AdvisorStep, ex.ErrorMessageKey);
			return AiCvAdvisorResult.Fail(ex.ErrorMessageKey, section, descriptor);
		}
	}

	/// <summary>Effective output culture for a task (045 C.4): content tasks follow CV language.</summary>
	internal static string ResolveTaskCulture(AiCvTaskKind task, string currentText, string uiCulture) =>
		AiCvTaskRegistry.ProducesCvContent(task)
			? AiCvContentLanguageDetector.Detect(currentText, uiCulture)
			: uiCulture;

	internal static AiCvCompletionContext BuildContext(
		AiCvTaskKind task,
		CvExportSourceData snapshot,
		CvQualityHint hint)
	{
		return task switch
		{
			AiCvTaskKind.ImproveWorkDescription or AiCvTaskKind.DraftWorkDescription =>
				BuildWorkContext(task, snapshot, hint),
			AiCvTaskKind.ImproveProfessionalSummary or AiCvTaskKind.DraftProfessionalSummary
				or AiCvTaskKind.ShortenProfessionalSummary =>
				new AiCvCompletionContext(
					task,
					snapshot.Personal.ShortSummary,
					ProfessionalTitle: snapshot.Personal.ProfessionalTitle),
			AiCvTaskKind.ImproveProjectDescription =>
				BuildProjectContext(snapshot, hint),
			_ => throw new ArgumentOutOfRangeException(nameof(task), task, null),
		};
	}

	private AiCvCompletionContext BuildAdviceContext(
		AiCvTaskKind task,
		CvImportSectionId section,
		CvExportSourceData snapshot,
		AiCvTargetContext? targetContext)
	{
		return task switch
		{
			AiCvTaskKind.SuggestSkillGrouping => new AiCvCompletionContext(
				task,
				AiCvSectionContent.Describe(CvImportSectionId.Skills, snapshot),
				Section: CvImportSectionId.Skills,
				SectionContent: AiCvSectionContent.Describe(CvImportSectionId.Skills, snapshot),
				TargetContext: targetContext),
			AiCvTaskKind.DraftSkillsFromContext => new AiCvCompletionContext(
				task,
				AiCvSectionContent.Describe(CvImportSectionId.WorkExperience, snapshot),
				Section: CvImportSectionId.Skills,
				SectionContent: AiCvSectionContent.Describe(CvImportSectionId.WorkExperience, snapshot),
				SectionIsEmpty: true,
				TargetContext: targetContext),
			AiCvTaskKind.AdviseEducationSection => new AiCvCompletionContext(
				task,
				string.Empty,
				Section: CvImportSectionId.Education,
				SectionIsEmpty: true,
				TargetContext: targetContext),
			AiCvTaskKind.AdviseLanguagesSection => new AiCvCompletionContext(
				task,
				string.Empty,
				Section: CvImportSectionId.Languages,
				SectionIsEmpty: true,
				TargetContext: targetContext),
			_ => new AiCvCompletionContext(task, string.Empty, Section: section, TargetContext: targetContext),
		};
	}

	private static AiCvCompletionContext BuildWorkContext(
		AiCvTaskKind task,
		CvExportSourceData snapshot,
		CvQualityHint hint)
	{
		var entry = snapshot.WorkExperience.FirstOrDefault(item =>
			string.Equals(item.Id, hint.EntryId, StringComparison.Ordinal));

		return new AiCvCompletionContext(
			task,
			entry?.Description ?? string.Empty,
			entry?.JobTitle,
			entry?.Company);
	}

	private static AiCvCompletionContext BuildProjectContext(
		CvExportSourceData snapshot,
		CvQualityHint hint)
	{
		var entry = snapshot.Projects.FirstOrDefault(item =>
			string.Equals(item.Id, hint.EntryId, StringComparison.Ordinal));

		return new AiCvCompletionContext(
			AiCvTaskKind.ImproveProjectDescription,
			entry?.Description ?? string.Empty,
			ProjectName: entry?.Name);
	}

	private static AiCvCompletionResult Fail(string? errorKey, IAiBackendRuntime? runtime) =>
		new(
			false,
			null,
			errorKey ?? TranslationKeys.AiCvTaskFailed,
			runtime is null ? null : CreateDescriptor(runtime));

	private static AiCvBackendDescriptor CreateDescriptor(IAiBackendRuntime runtime)
	{
		var localizer = AppLocalizer.FromSystemCulture();
		return new AiCvBackendDescriptor(runtime.Kind, runtime.DescribeActiveBackend(localizer));
	}
}
