using ReVitae.Core.Ai;

namespace ReVitae.Core.Projects;

public sealed record RecentProjectEntry(
	string Path,
	string DisplayName,
	DateTimeOffset LastOpenedUtc);

public sealed class RecentProjectsStore
{
	private readonly string _filePath;

	public RecentProjectsStore()
		: this(Path.Combine(ReVitaeLocalDataPaths.GetReVitaeRootDirectory(), "recent-projects.json"))
	{
	}

	public RecentProjectsStore(string filePath)
	{
		_filePath = filePath;
	}

	public IReadOnlyList<RecentProjectEntry> Load()
	{
		if (!File.Exists(_filePath))
		{
			return [];
		}

		try
		{
			var json = File.ReadAllText(_filePath);
			var document = System.Text.Json.JsonSerializer.Deserialize<RecentProjectsDocument>(
				json,
				JsonOptions);
			if (document?.Entries is null)
			{
				return [];
			}

			return document.Entries
				.Where(entry => !string.IsNullOrWhiteSpace(entry.Path))
				.Select(entry => new RecentProjectEntry(
					entry.Path!,
					string.IsNullOrWhiteSpace(entry.DisplayName)
						? Path.GetFileNameWithoutExtension(entry.Path!)
						: entry.DisplayName!,
					entry.LastOpenedUtc))
				.Where(entry => File.Exists(entry.Path))
				.OrderByDescending(entry => entry.LastOpenedUtc)
				.Take(CvProjectConstants.MaxRecentProjects)
				.ToArray();
		}
		catch
		{
			return [];
		}
	}

	public void Add(string path, string displayName)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		var normalizedPath = Path.GetFullPath(path);
		var existing = Load()
			.Where(entry => !PathsEqual(entry.Path, normalizedPath))
			.ToList();

		existing.Insert(0, new RecentProjectEntry(
			normalizedPath,
			displayName,
			DateTimeOffset.UtcNow));

		var trimmed = existing.Take(CvProjectConstants.MaxRecentProjects).ToArray();
		AtomicJsonFileWriter.WriteObject(
			_filePath,
			new RecentProjectsDocument
			{
				Entries = trimmed
					.Select(entry => new RecentProjectEntryDto
					{
						Path = entry.Path,
						DisplayName = entry.DisplayName,
						LastOpenedUtc = entry.LastOpenedUtc
					})
					.ToArray()
			});
	}

	public void RemoveMissing(string path)
	{
		var normalizedPath = Path.GetFullPath(path);
		var existing = Load()
			.Where(entry => !PathsEqual(entry.Path, normalizedPath))
			.ToArray();

		AtomicJsonFileWriter.WriteObject(
			_filePath,
			new RecentProjectsDocument
			{
				Entries = existing
					.Select(entry => new RecentProjectEntryDto
					{
						Path = entry.Path,
						DisplayName = entry.DisplayName,
						LastOpenedUtc = entry.LastOpenedUtc
					})
					.ToArray()
			});
	}

	public void Clear()
	{
		if (File.Exists(_filePath))
		{
			File.Delete(_filePath);
		}
	}

	private static bool PathsEqual(string left, string right) =>
		OperatingSystem.IsWindows()
			? string.Equals(left, right, StringComparison.OrdinalIgnoreCase)
			: string.Equals(left, right, StringComparison.Ordinal);

	private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
		WriteIndented = true
	};

	private sealed class RecentProjectsDocument
	{
		public RecentProjectEntryDto[]? Entries { get; init; }
	}

	private sealed class RecentProjectEntryDto
	{
		public string? Path { get; init; }

		public string? DisplayName { get; init; }

		public DateTimeOffset LastOpenedUtc { get; init; }
	}
}
