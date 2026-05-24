using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ReVitae.Core.Ai.Providers;

public sealed class InMemoryAiSecretStorage : IAiSecretStorage
{
    private readonly ConcurrentDictionary<string, string> _keys = new(StringComparer.Ordinal);

    public string? TryGetApiKey(string providerId) =>
        _keys.TryGetValue(providerId, out var key) ? key : null;

    public void SaveApiKey(string providerId, string apiKey) =>
        _keys[providerId] = apiKey;

    public void DeleteApiKey(string providerId) =>
        _keys.TryRemove(providerId, out _);

    public void DeleteAll() => _keys.Clear();
}

public sealed class FileAiSecretStorage : IAiSecretStorage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly string _filePath;
    private readonly string _keyPath;
    private readonly object _lock = new();

    public FileAiSecretStorage()
        : this(ReVitaeLocalDataPaths.GetAiSecretsFilePath(), ReVitaeLocalDataPaths.GetAiSecretsKeyFilePath())
    {
    }

    public FileAiSecretStorage(string filePath, string keyPath)
    {
        _filePath = filePath;
        _keyPath = keyPath;
    }

    public string? TryGetApiKey(string providerId)
    {
        lock (_lock)
        {
            var map = LoadMap();
            return map.TryGetValue(providerId, out var key) ? key : null;
        }
    }

    public void SaveApiKey(string providerId, string apiKey)
    {
        lock (_lock)
        {
            var map = LoadMap();
            map[providerId] = apiKey;
            SaveMap(map);
        }
    }

    public void DeleteApiKey(string providerId)
    {
        lock (_lock)
        {
            var map = LoadMap();
            if (map.Remove(providerId))
            {
                SaveMap(map);
            }
        }
    }

    public void DeleteAll()
    {
        lock (_lock)
        {
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }
        }
    }

    private Dictionary<string, string> LoadMap()
    {
        if (!File.Exists(_filePath))
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        try
        {
            var protectedBytes = File.ReadAllBytes(_filePath);
            var plainBytes = AiSecretProtector.Unprotect(protectedBytes, _keyPath);
            var json = Encoding.UTF8.GetString(plainBytes);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions)
                   ?? new Dictionary<string, string>(StringComparer.Ordinal);
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }
    }

    private void SaveMap(Dictionary<string, string> map)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(map, JsonOptions);
        var plainBytes = Encoding.UTF8.GetBytes(json);
        var protectedBytes = AiSecretProtector.Protect(plainBytes, _keyPath);
        var tempPath = _filePath + ".tmp";
        File.WriteAllBytes(tempPath, protectedBytes);
        File.Move(tempPath, _filePath, overwrite: true);
    }
}

internal static class AiSecretProtector
{
    public static byte[] Protect(byte[] plainBytes, string keyPath)
    {
        using var aes = Aes.Create();
        aes.Key = LoadOrCreateKey(keyPath);
        aes.GenerateIV();
        using var encryptor = aes.CreateEncryptor();
        var cipher = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        var payload = new byte[aes.IV.Length + cipher.Length];
        Buffer.BlockCopy(aes.IV, 0, payload, 0, aes.IV.Length);
        Buffer.BlockCopy(cipher, 0, payload, aes.IV.Length, cipher.Length);
        return payload;
    }

    public static byte[] Unprotect(byte[] payload, string keyPath)
    {
        using var aes = Aes.Create();
        aes.Key = LoadOrCreateKey(keyPath);
        var iv = payload.AsSpan(0, 16).ToArray();
        var cipher = payload.AsSpan(16).ToArray();
        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
    }

    private static byte[] LoadOrCreateKey(string keyPath)
    {
        var directory = Path.GetDirectoryName(keyPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (File.Exists(keyPath))
        {
            var existing = File.ReadAllBytes(keyPath);
            if (existing.Length == 32)
            {
                return existing;
            }
        }

        var key = RandomNumberGenerator.GetBytes(32);
        File.WriteAllBytes(keyPath, key);
        return key;
    }
}
