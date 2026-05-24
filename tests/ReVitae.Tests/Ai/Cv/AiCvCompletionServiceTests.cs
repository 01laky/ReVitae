using ReVitae.Core.Ai.Cv;
using ReVitae.Core.Ai.Providers;
using ReVitae.Core.Ai.Providers.Chat;
using ReVitae.Core.Export;
using ReVitae.Core.Import;
using ReVitae.Core.Localization;
using ReVitae.Core.Quality;
using CvWorkExperienceEntry = ReVitae.Core.Cv.WorkExperience.WorkExperienceEntry;
using ReVitae.Core.Cv.WorkExperience;

namespace ReVitae.Tests.Ai.Cv;

public sealed class AiCvCompletionServiceTests
{
    [Fact]
    public async Task CompleteForQualityHintAsync_NoBackend_ReturnsNoBackendConfigured()
    {
        var configService = CreateConfigService(AiSettingsDocument.Empty);
        var service = new AiCvCompletionService(configService, new AiBackendRuntimeResolver());

        var hint = CreateWorkHint("entry1");
        var snapshot = CreateSnapshot("entry1", "Generic work.");

        var result = await service.CompleteForQualityHintAsync(snapshot, hint, "en");

        Assert.False(result.Succeeded);
        Assert.Equal(TranslationKeys.AiCvNoBackendConfigured, result.ErrorMessageKey);
    }

    [Fact]
    public async Task CompleteForQualityHintAsync_MockLocalRuntime_Succeeds()
    {
        var configService = CreateConfigService(LocalSettings("gemma2-2b"));
        var runtime = new StubBackendRuntime(AiBackendKind.Local, "Improved work description.");
        var service = new AiCvCompletionService(configService, new FixedRuntimeResolver(runtime));

        var hint = CreateWorkHint("entry1");
        var snapshot = CreateSnapshot("entry1", "Did things at work.");

        var result = await service.CompleteForQualityHintAsync(snapshot, hint, "en");

        Assert.True(result.Succeeded);
        Assert.Equal("Improved work description.", result.SuggestedText);
        Assert.NotNull(result.BackendUsed);
    }

    [Fact]
    public async Task CompleteForQualityHintAsync_Provider401_MapsInvalidKey()
    {
        var configService = CreateConfigService(OnlineSettings("openai"));
        var runtime = new StubBackendRuntime(
            AiBackendKind.Online,
            null,
            new AiChatCompletionResult(false, null, TranslationKeys.AiSetupProviderInvalidKey));
        var service = new AiCvCompletionService(configService, new FixedRuntimeResolver(runtime));

        var hint = CreateWorkHint("entry1");
        var snapshot = CreateSnapshot("entry1", "Did things.");

        var result = await service.CompleteForQualityHintAsync(snapshot, hint, "en");

        Assert.False(result.Succeeded);
        Assert.Equal(TranslationKeys.AiSetupProviderInvalidKey, result.ErrorMessageKey);
    }

    [Fact]
    public async Task CompleteForQualityHintAsync_Cancellation_ReturnsCancelled()
    {
        var configService = CreateConfigService(LocalSettings("gemma2-2b"));
        var service = new AiCvCompletionService(configService, new FixedRuntimeResolver(new SlowBackendRuntime()));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var hint = CreateWorkHint("entry1");
        var snapshot = CreateSnapshot("entry1", "Text");

        var result = await service.CompleteForQualityHintAsync(snapshot, hint, "en", cts.Token);

        Assert.False(result.Succeeded);
        Assert.True(result.Cancelled);
    }

    [Fact]
    public void GetBackendStatus_LocalConfigured_IsAvailable()
    {
        var service = new AiCvCompletionService(
            CreateConfigService(LocalSettings("gemma2-2b")),
            new AiBackendRuntimeResolver());

        var status = service.GetBackendStatus();

        Assert.Equal(AiBackendKind.Local, status.Kind);
        Assert.True(status.IsAvailable);
    }

    [Fact]
    public void IsQualityHintSupported_WorkGeneric_ReturnsTrue()
    {
        var service = new AiCvCompletionService();

        Assert.True(service.IsQualityHintSupported(CvQualityHintIds.WorkGenericDescription));
        Assert.False(service.IsQualityHintSupported(CvQualityHintIds.WorkSectionEmpty));
    }

    [Fact]
    public void BuildContext_WorkHint_IncludesEntryMetadata()
    {
        var hint = CreateWorkHint("entry1");
        var snapshot = CreateSnapshot("entry1", "Desc", "Engineer", "Acme");

        var context = AiCvCompletionService.BuildContext(
            AiCvTaskKind.ImproveWorkDescription,
            snapshot,
            hint);

        Assert.Equal("Desc", context.CurrentText);
        Assert.Equal("Engineer", context.JobTitle);
        Assert.Equal("Acme", context.Company);
    }

    private static AiProviderConfigService CreateConfigService(AiSettingsDocument settings)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"revitae-ai-test-{Guid.NewGuid():N}.json");
        var repository = new AiSettingsRepository(tempFile);
        repository.Save(settings);

        var secrets = new TestSecretStorage();
        secrets.SaveApiKey("openai", "sk-test");

        var configService = new AiProviderConfigService(
            repository,
            secrets,
            new AiProviderConnectionTester());
        configService.Load();
        return configService;
    }

    private static AiSettingsDocument LocalSettings(string modelId) =>
        new(
            AiSettingsDocument.CurrentSchemaVersion,
            AiBackendKind.Local,
            modelId,
            null,
            new LocalAiSettingsRecord(modelId, "gemma2:2b", DateTimeOffset.UtcNow),
            new Dictionary<string, AiProviderConnectionConfig>(StringComparer.Ordinal));

    private static AiSettingsDocument OnlineSettings(string providerId) =>
        new(
            AiSettingsDocument.CurrentSchemaVersion,
            AiBackendKind.Online,
            null,
            providerId,
            null,
            new Dictionary<string, AiProviderConnectionConfig>(StringComparer.Ordinal)
            {
                [providerId] = new(
                    providerId,
                    "gpt-4o-mini",
                    "https://api.openai.com/v1",
                    null,
                    null,
                    null,
                    DateTimeOffset.UtcNow,
                    true),
            });

    private static CvQualityHint CreateWorkHint(string entryId) =>
        new(
            CvQualityHintIds.WorkGenericDescription,
            TranslationKeys.QualityHintWorkGenericDescription,
            CvQualityHintSeverity.Suggestion,
            CvImportSectionId.WorkExperience,
            WorkExperienceFieldKeys.Build(entryId, WorkExperienceFieldKeys.Description),
            entryId);

    private static CvExportSourceData CreateSnapshot(
        string entryId,
        string description,
        string jobTitle = "Engineer",
        string company = "Acme") =>
        new(
            new PersonalInformationImport(),
            [new CvWorkExperienceEntry(entryId) { Description = description, JobTitle = jobTitle, Company = company }],
            [],
            [],
            [],
            [],
            [],
            [],
            null);

    private sealed class TestSecretStorage : IAiSecretStorage
    {
        private readonly Dictionary<string, string> _keys = new(StringComparer.Ordinal);

        public void SaveApiKey(string providerId, string apiKey) => _keys[providerId] = apiKey;

        public string? TryGetApiKey(string providerId) =>
            _keys.TryGetValue(providerId, out var key) ? key : null;

        public void DeleteApiKey(string providerId) => _keys.Remove(providerId);

        public void DeleteAll() => _keys.Clear();
    }

    private sealed class FixedRuntimeResolver(IAiBackendRuntime runtime) : IAiBackendRuntimeResolver
    {
        public AiBackendResolveResult Resolve(AiSettingsDocument settings, IAiSecretStorage secretStorage) =>
            AiBackendResolveResult.Success(runtime);
    }

    private sealed class StubBackendRuntime : IAiBackendRuntime
    {
        private readonly AiChatCompletionResult _result;

        public StubBackendRuntime(AiBackendKind kind, string? content, AiChatCompletionResult? result = null)
        {
            Kind = kind;
            _result = result ?? new AiChatCompletionResult(true, content, null);
        }

        public AiBackendKind Kind { get; }

        public string DescribeActiveBackend(AppLocalizer localizer) => "Stub";

        public Task<AiChatCompletionResult> CompleteAsync(
            AiCvPromptMessages messages,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_result);
    }

    private sealed class SlowBackendRuntime : IAiBackendRuntime
    {
        public AiBackendKind Kind => AiBackendKind.Local;

        public string DescribeActiveBackend(AppLocalizer localizer) => "Slow";

        public async Task<AiChatCompletionResult> CompleteAsync(
            AiCvPromptMessages messages,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
            return new AiChatCompletionResult(true, "late", null);
        }
    }
}
