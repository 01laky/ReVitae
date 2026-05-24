using System.Text.Json;

namespace ReVitae.Core.Ai;

public sealed class AiSettingsStorage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly string _filePath;

    public AiSettingsStorage()
        : this(ReVitaeLocalDataPaths.GetAiSettingsFilePath())
    {
    }

    public AiSettingsStorage(string filePath)
    {
        _filePath = filePath;
    }

    public AiSettingsSnapshot? TryLoad()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return null;
            }

            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<AiSettingsSnapshot>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public void Save(AiSettingsSnapshot snapshot)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(snapshot, JsonOptions);
        File.WriteAllText(_filePath, json);
    }

    public void Clear()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }
        }
        catch
        {
        }
    }
}
