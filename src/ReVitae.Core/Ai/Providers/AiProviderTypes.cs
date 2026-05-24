namespace ReVitae.Core.Ai.Providers;

public enum AiBackendKind
{
    None = 0,
    Local = 1,
    Online = 2,
}

public enum AiOnlineApiStyle
{
    OpenAiCompatible = 0,
    AnthropicMessages = 1,
    GeminiGenerateContent = 2,
}

public enum AiProviderFieldKind
{
    Password = 0,
    Url = 1,
    Text = 2,
    ModelSelect = 3,
}

public enum AiProviderConnectionErrorKind
{
    None = 0,
    InvalidKey = 1,
    ModelNotFound = 2,
    RateLimited = 3,
    ProviderUnavailable = 4,
    NetworkError = 5,
    InvalidConfiguration = 6,
}

public static class AiProviderFieldIds
{
    public const string ApiKey = "apiKey";
    public const string BaseUrl = "baseUrl";
    public const string ModelId = "modelId";
    public const string DeploymentName = "deploymentName";
    public const string ApiVersion = "apiVersion";
    public const string OrganizationId = "organizationId";
}

public sealed record AiProviderFieldDefinition(
    string Id,
    string LabelKey,
    AiProviderFieldKind Kind,
    bool Required,
    string? PlaceholderKey = null,
    bool Advanced = false);

public sealed record AiProviderModelOption(string ModelId, string? HintKey = null);

public sealed record AiOnlineProviderDefinition(
    string Id,
    string DisplayNameKey,
    string DescriptionKey,
    AiOnlineApiStyle ApiStyle,
    string? DefaultBaseUrl,
    bool HasFreeTierBadge,
    IReadOnlyList<AiProviderFieldDefinition> Fields,
    IReadOnlyList<AiProviderModelOption> SuggestedModels,
    string? ModelUseCaseHintKey = null);

public sealed record AiProviderConnectionConfig(
    string ProviderId,
    string? ModelId,
    string? BaseUrl,
    string? OrganizationId,
    string? DeploymentName,
    string? ApiVersion,
    DateTimeOffset? LastTestedAtUtc,
    bool? LastTestSucceeded);

public sealed record AiProviderConnectionDraft(
    string ProviderId,
    IReadOnlyDictionary<string, string?> Values,
    string? ApiKey);

public sealed record AiProviderTestResult(
    bool Succeeded,
    AiProviderConnectionErrorKind ErrorKind,
    string? ErrorMessageKey,
    string? ErrorDetail);

public sealed record AiChatCompletionResult(bool Succeeded, string? Content, string? ErrorMessage);

public sealed record LocalAiSettingsRecord(
    string? SelectedModelId,
    string? OllamaModelTag,
    DateTimeOffset? DownloadedAtUtc);

public sealed record AiSettingsDocument(
    int SchemaVersion,
    AiBackendKind ActiveBackend,
    string? ActiveLocalModelId,
    string? ActiveOnlineProviderId,
    LocalAiSettingsRecord? Local,
    IReadOnlyDictionary<string, AiProviderConnectionConfig> OnlineProviders)
{
    public const int CurrentSchemaVersion = 2;

    public static AiSettingsDocument Empty { get; } = new(
        CurrentSchemaVersion,
        AiBackendKind.None,
        null,
        null,
        null,
        new Dictionary<string, AiProviderConnectionConfig>(StringComparer.Ordinal));
}

public sealed record ActiveAiBackendSnapshot(
    AiBackendKind Kind,
    string? LocalModelId,
    string? OnlineProviderId,
    string? DisplayNameKey,
    string? ModelLabel);

public enum AiProviderUiAction
{
    Configure,
    Activate,
    Deactivate,
}

public sealed record AiProviderRowPresentation(
    string ProviderId,
    AiProviderUiAction PrimaryAction,
    bool ShowEditLink,
    bool IsConfigured,
    bool IsActive,
    bool ShowFreeTierBadge,
    bool? LastTestSucceeded);
