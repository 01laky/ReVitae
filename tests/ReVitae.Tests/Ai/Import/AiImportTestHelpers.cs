using System.Text.Json.Nodes;
using ReVitae.Core.Ai.Cv;
using ReVitae.Core.Ai.Import;
using ReVitae.Core.Ai.Providers;
using ReVitae.Core.Ai.Providers.Chat;
using ReVitae.Core.Import;
using ReVitae.Core.Import.Extraction;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Ai.Import;

internal static class AiImportTestHelpers
{
    public static CvSegmentationResult EmptySegmentation() =>
        new()
        {
            HeaderBlock = string.Empty,
            SectionBodies = new Dictionary<CvImportSectionId, string>(),
            Warnings = [],
        };

    public static CvSegmentationResult SegmentationWithWork(string workBody) =>
        new()
        {
            HeaderBlock = "John Doe\njohn@example.com",
            SectionBodies = new Dictionary<CvImportSectionId, string>
            {
                [CvImportSectionId.WorkExperience] = workBody,
            },
            Warnings = [],
        };

    public static CvTextImportAttempt CreateAttempt(
        CvImportResult deterministic,
        string normalizedText,
        CvImportFormat format = CvImportFormat.PlainText)
    {
        var extraction = new CvTextExtractionResult(true, normalizedText, null);
        var segmentation = CvSectionSegmenter.Segment(CvTextNormalizer.Normalize(normalizedText));
        return new CvTextImportAttempt(format, extraction, normalizedText, segmentation, deterministic);
    }

    public static CvImportResult FailedDeterministic(string text) =>
        CreateAttempt(
            CvImportResult.Failed(TranslationKeys.ImportErrorNoStructuredData),
            text).Deterministic;

    public static CvImportResult ThinSuccess(string text, int sectionCount)
    {
        var flags = Enum.GetValues<CvImportSectionId>()
            .Take(sectionCount)
            .ToDictionary(id => id, _ => true);
        foreach (var id in Enum.GetValues<CvImportSectionId>().Skip(sectionCount))
        {
            flags[id] = false;
        }

        return new CvImportResult
        {
            Success = true,
            Personal = new PersonalInformationImport { FirstName = "John" },
            SectionHasData = flags,
        };
    }

    public static AiSettingsDocument LocalSettings(string modelId) =>
        new(
            AiSettingsDocument.CurrentSchemaVersion,
            AiBackendKind.Local,
            modelId,
            null,
            new LocalAiSettingsRecord(modelId, $"{modelId}:tag", DateTimeOffset.UtcNow),
            new Dictionary<string, AiProviderConnectionConfig>(StringComparer.Ordinal));

    public static AiSettingsDocument OnlineSettings(string providerId, string modelId, string? deployment = null) =>
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
                    modelId,
                    "https://api.example.com/v1",
                    null,
                    deployment,
                    null,
                    DateTimeOffset.UtcNow,
                    true),
            });

    public static AiProviderConfigService CreateConfigService(AiSettingsDocument settings)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"revitae-ai-import-{Guid.NewGuid():N}.json");
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

    public static JsonObject PersonalJson(string first = "John", string last = "Doe", string email = "john@example.com") =>
        new()
        {
            ["personalInformation"] = new JsonObject
            {
                ["firstName"] = first,
                ["lastName"] = last,
                ["email"] = email,
            },
        };

    public static JsonObject WorkJson(params (string company, int year)[] entries)
    {
        var array = new JsonArray();
        foreach (var (company, year) in entries)
        {
            array.Add(new JsonObject
            {
                ["company"] = company,
                ["jobTitle"] = "Engineer",
                ["startYear"] = year,
                ["startMonth"] = 1,
            });
        }

        return new JsonObject { ["workExperience"] = array };
    }

    internal sealed class TestSecretStorage : IAiSecretStorage
    {
        private readonly Dictionary<string, string> _keys = new(StringComparer.Ordinal);

        public void SaveApiKey(string providerId, string apiKey) => _keys[providerId] = apiKey;

        public string? TryGetApiKey(string providerId) =>
            _keys.TryGetValue(providerId, out var key) ? key : null;

        public void DeleteApiKey(string providerId) => _keys.Remove(providerId);

        public void DeleteAll() => _keys.Clear();
    }

    internal sealed class FixedRuntimeResolver(IAiBackendRuntime runtime) : IAiBackendRuntimeResolver
    {
        public AiBackendResolveResult Resolve(AiSettingsDocument settings, IAiSecretStorage secretStorage) =>
            AiBackendResolveResult.Success(runtime);
    }

    internal sealed class SequenceRuntime(IReadOnlyList<string?> responses) : IAiBackendRuntime
    {
        private int _index;

        public AiBackendKind Kind { get; init; } = AiBackendKind.Local;

        public string DescribeActiveBackend(AppLocalizer localizer) => "Sequence";

        public Task<AiChatCompletionResult> CompleteAsync(
            AiCvPromptMessages messages,
            CancellationToken cancellationToken = default)
        {
            var content = _index < responses.Count ? responses[_index] : "{}";
            _index++;
            return Task.FromResult(new AiChatCompletionResult(true, content, null));
        }
    }

    internal sealed class FailingRuntime(string errorKey) : IAiBackendRuntime
    {
        public AiBackendKind Kind => AiBackendKind.Online;

        public string DescribeActiveBackend(AppLocalizer localizer) => "Fail";

        public Task<AiChatCompletionResult> CompleteAsync(
            AiCvPromptMessages messages,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new AiChatCompletionResult(false, null, errorKey));
    }
}
