using ReVitae.Core.Ai.Providers;

namespace ReVitae.Core.Ai;

public sealed class AiSettingsStorage
{
    private readonly AiSettingsRepository _repository;

    public AiSettingsStorage()
        : this(new AiSettingsRepository())
    {
    }

    public AiSettingsStorage(string filePath)
        : this(new AiSettingsRepository(filePath))
    {
    }

    public AiSettingsStorage(AiSettingsRepository repository)
    {
        _repository = repository;
    }

    public AiSettingsRepository Repository => _repository;

    public AiSettingsDocument LoadDocument() => _repository.LoadOrDefault();

    public void SaveDocument(AiSettingsDocument document) => _repository.Save(document);

    public AiSettingsSnapshot? TryLoad()
    {
        var document = _repository.LoadOrDefault();
        if (document.Local?.SelectedModelId is null ||
            document.Local.OllamaModelTag is null ||
            document.Local.DownloadedAtUtc is null)
        {
            return null;
        }

        return new AiSettingsSnapshot(
            document.Local.SelectedModelId,
            document.Local.OllamaModelTag,
            document.Local.DownloadedAtUtc.Value);
    }

    public void Save(AiSettingsSnapshot snapshot)
    {
        var document = _repository.LoadOrDefault();
        document = document with
        {
            Local = new LocalAiSettingsRecord(
                snapshot.SelectedModelId,
                snapshot.OllamaModelTag,
                snapshot.DownloadedAtUtc),
            ActiveBackend = document.ActiveBackend == AiBackendKind.Online
                ? AiBackendKind.Online
                : AiBackendKind.Local,
            ActiveLocalModelId = document.ActiveBackend == AiBackendKind.Online
                ? document.ActiveLocalModelId
                : snapshot.SelectedModelId,
        };
        _repository.Save(document);
    }

    public void ClearLocalIfMatches(string modelId)
    {
        var document = _repository.LoadOrDefault();
        if (!string.Equals(document.Local?.SelectedModelId, modelId, StringComparison.Ordinal))
        {
            return;
        }

        var deactivate = document.ActiveBackend == AiBackendKind.Local &&
                         string.Equals(document.ActiveLocalModelId, modelId, StringComparison.Ordinal);
        document = document with
        {
            Local = null,
            ActiveBackend = deactivate ? AiBackendKind.None : document.ActiveBackend,
            ActiveLocalModelId = deactivate ? null : document.ActiveLocalModelId,
        };
        _repository.Save(document);
    }

    public void Clear()
    {
        _repository.Clear();
    }
}
