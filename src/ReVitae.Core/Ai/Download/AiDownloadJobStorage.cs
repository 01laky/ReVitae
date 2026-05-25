using System.Text.Json;

namespace ReVitae.Core.Ai.Download;

internal sealed record AiDownloadJobDocument(
	Guid JobId,
	string SelectedModelId,
	string OllamaModelTag,
	string DisplayNameKey,
	AiDownloadJobState State,
	bool RequiresOversizedWarning,
	long? CompletedBytes,
	long? TotalBytes,
	string? StatusText,
	string? ErrorMessageKey,
	DateTimeOffset StartedAtUtc,
	DateTimeOffset LastUpdatedAtUtc);

public sealed class AiDownloadJobStorage
{
	public const int MaxProgressWritesPerSecond = 2;

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = true,
	};

	private readonly string _filePath;
	private readonly IClock _clock;
	private readonly object _writeLock = new();
	private DateTimeOffset _lastProgressWriteUtc = DateTimeOffset.MinValue;

	public AiDownloadJobStorage()
		: this(ReVitaeLocalDataPaths.GetAiDownloadJobFilePath(), new SystemClock())
	{
	}

	public AiDownloadJobStorage(string filePath, IClock clock)
	{
		_filePath = filePath;
		_clock = clock;
	}

	public AiDownloadJobSnapshot? TryLoad()
	{
		try
		{
			if (!File.Exists(_filePath))
			{
				return null;
			}

			var json = File.ReadAllText(_filePath);
			var document = JsonSerializer.Deserialize<AiDownloadJobDocument>(json, JsonOptions);
			return document is null ? null : ToSnapshot(document);
		}
		catch
		{
			QuarantineCorruptFile();
			return null;
		}
	}

	public void Save(AiDownloadJobSnapshot snapshot, bool forceFlush = false)
	{
		lock (_writeLock)
		{
			if (!forceFlush && IsProgressOnlyUpdate(snapshot) && !ShouldWriteProgressNow())
			{
				return;
			}

			if (IsProgressOnlyUpdate(snapshot))
			{
				_lastProgressWriteUtc = _clock.UtcNow;
			}

			var directory = Path.GetDirectoryName(_filePath);
			if (!string.IsNullOrWhiteSpace(directory))
			{
				Directory.CreateDirectory(directory);
			}

			var document = new AiDownloadJobDocument(
				snapshot.JobId,
				snapshot.SelectedModelId,
				snapshot.OllamaModelTag,
				snapshot.DisplayNameKey,
				snapshot.State,
				snapshot.RequiresOversizedWarning,
				snapshot.CompletedBytes,
				snapshot.TotalBytes,
				snapshot.StatusText,
				snapshot.ErrorMessageKey,
				snapshot.StartedAtUtc,
				snapshot.LastUpdatedAtUtc);

			var json = JsonSerializer.Serialize(document, JsonOptions);
			var tempPath = _filePath + ".tmp";
			File.WriteAllText(tempPath, json);
			File.Move(tempPath, _filePath, overwrite: true);
		}
	}

	public void Delete()
	{
		lock (_writeLock)
		{
			if (File.Exists(_filePath))
			{
				File.Delete(_filePath);
			}
		}
	}

	public bool ShouldWriteProgressNow()
	{
		var elapsed = _clock.UtcNow - _lastProgressWriteUtc;
		return elapsed.TotalMilliseconds >= 1000d / MaxProgressWritesPerSecond;
	}

	private static bool IsProgressOnlyUpdate(AiDownloadJobSnapshot snapshot) =>
		snapshot.State is AiDownloadJobState.Downloading or AiDownloadJobState.Interrupted;

	private void QuarantineCorruptFile()
	{
		try
		{
			if (!File.Exists(_filePath))
			{
				return;
			}

			var backupPath = _filePath + ".bak";
			if (File.Exists(backupPath))
			{
				File.Delete(backupPath);
			}

			File.Move(_filePath, backupPath);
		}
		catch
		{
		}
	}

	private static AiDownloadJobSnapshot ToSnapshot(AiDownloadJobDocument document) =>
		new(
			document.JobId,
			document.SelectedModelId,
			document.OllamaModelTag,
			document.DisplayNameKey,
			document.State,
			document.RequiresOversizedWarning,
			document.CompletedBytes,
			document.TotalBytes,
			document.StatusText,
			document.StartedAtUtc,
			document.LastUpdatedAtUtc,
			document.ErrorMessageKey);
}
