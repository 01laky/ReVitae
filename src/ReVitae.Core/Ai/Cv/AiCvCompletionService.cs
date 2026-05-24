using ReVitae.Core.Ai.Providers;
using ReVitae.Core.Export;
using ReVitae.Core.Localization;
using ReVitae.Core.Quality;

namespace ReVitae.Core.Ai.Cv;

public sealed class AiCvCompletionService
{
    private readonly AiProviderConfigService _configService;
    private readonly IAiBackendRuntimeResolver _runtimeResolver;

    public AiCvCompletionService()
        : this(new AiProviderConfigService(), new AiBackendRuntimeResolver())
    {
    }

    public AiCvCompletionService(
        AiProviderConfigService configService,
        IAiBackendRuntimeResolver runtimeResolver)
    {
        _configService = configService;
        _runtimeResolver = runtimeResolver;
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

    public async Task<AiCvCompletionResult> CompleteForQualityHintAsync(
        CvExportSourceData snapshot,
        CvQualityHint hint,
        string uiCulture,
        CancellationToken cancellationToken = default)
    {
        var task = AiCvTaskRegistry.TryGetTaskForQualityHint(hint.Id);
        if (task is null)
        {
            return Fail(TranslationKeys.AiCvTaskFailed, null);
        }

        var resolve = _runtimeResolver.Resolve(_configService.CurrentSettings, _configService.SecretStorage);
        if (!resolve.IsAvailable || resolve.Runtime is null)
        {
            return Fail(resolve.ErrorMessageKey ?? TranslationKeys.AiCvNoBackendConfigured, null);
        }

        var context = BuildContext(task.Value, snapshot, hint);
        var messages = AiCvPromptBuilder.Build(task.Value, context, uiCulture);

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
            return new AiCvCompletionResult(
                true,
                parsed,
                null,
                CreateDescriptor(resolve.Runtime));
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

    internal static AiCvCompletionContext BuildContext(
        AiCvTaskKind task,
        CvExportSourceData snapshot,
        CvQualityHint hint)
    {
        return task switch
        {
            AiCvTaskKind.ImproveWorkDescription or AiCvTaskKind.DraftWorkDescription =>
                BuildWorkContext(task, snapshot, hint),
            AiCvTaskKind.ImproveProfessionalSummary or AiCvTaskKind.DraftProfessionalSummary =>
                new AiCvCompletionContext(
                    task,
                    snapshot.Personal.ShortSummary,
                    ProfessionalTitle: snapshot.Personal.ProfessionalTitle),
            AiCvTaskKind.ImproveProjectDescription =>
                BuildProjectContext(snapshot, hint),
            _ => throw new ArgumentOutOfRangeException(nameof(task), task, null),
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
