using ReVitae.Core.Projects;

namespace ReVitae.Tests.Projects;

public sealed class RecentProjectsStoreTests : IDisposable
{
	private readonly string _filePath;
	private readonly string _projectA;
	private readonly string _projectB;
	private readonly string _projectC;

	public RecentProjectsStoreTests()
	{
		var root = Path.Combine(Path.GetTempPath(), "revitae-recent-tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(root);
		_filePath = Path.Combine(root, "recent-projects.json");
		_projectA = Path.Combine(root, "a.revitae.json");
		_projectB = Path.Combine(root, "b.revitae.json");
		_projectC = Path.Combine(root, "c.revitae.json");
		File.WriteAllText(_projectA, "{}");
		File.WriteAllText(_projectB, "{}");
		File.WriteAllText(_projectC, "{}");
	}

	[Fact]
	public void Add_InsertsMostRecentFirst()
	{
		var store = new RecentProjectsStore(_filePath);
		store.Add(_projectA, "A");
		store.Add(_projectB, "B");

		var entries = store.Load();

		Assert.Equal(2, entries.Count);
		Assert.Equal(_projectB, entries[0].Path);
		Assert.Equal("B", entries[0].DisplayName);
	}

	[Fact]
	public void Add_DeduplicatesExistingPath()
	{
		var store = new RecentProjectsStore(_filePath);
		store.Add(_projectA, "A");
		store.Add(_projectB, "B");
		store.Add(_projectA, "A again");

		var entries = store.Load();

		Assert.Equal(2, entries.Count);
		Assert.Equal(_projectA, entries[0].Path);
		Assert.Equal("A again", entries[0].DisplayName);
	}

	[Fact]
	public void Add_CapsAtEightEntries()
	{
		var store = new RecentProjectsStore(_filePath);
		var root = Path.GetDirectoryName(_projectA)!;

		for (var i = 0; i < 10; i++)
		{
			var path = Path.Combine(root, $"file-{i}.revitae.json");
			File.WriteAllText(path, "{}");
			store.Add(path, $"File {i}");
		}

		Assert.Equal(8, store.Load().Count);
	}

	[Fact]
	public void Load_PrunesMissingFiles()
	{
		var store = new RecentProjectsStore(_filePath);
		store.Add(_projectA, "A");
		store.Add(_projectB, "B");
		File.Delete(_projectB);

		var entries = store.Load();

		Assert.Single(entries);
		Assert.Equal(_projectA, entries[0].Path);
	}

	[Fact]
	public void RemoveMissing_DropsSingleEntry()
	{
		var store = new RecentProjectsStore(_filePath);
		store.Add(_projectA, "A");
		store.Add(_projectB, "B");
		store.RemoveMissing(_projectA);

		var entries = store.Load();
		Assert.Single(entries);
		Assert.Equal(_projectB, entries[0].Path);
	}

	[Fact]
	public void Clear_RemovesPersistedList()
	{
		var store = new RecentProjectsStore(_filePath);
		store.Add(_projectA, "A");
		store.Clear();

		Assert.Empty(store.Load());
		Assert.False(File.Exists(_filePath));
	}

	public void Dispose()
	{
		try
		{
			var root = Path.GetDirectoryName(_filePath);
			if (!string.IsNullOrWhiteSpace(root) && Directory.Exists(root))
			{
				Directory.Delete(root, recursive: true);
			}
		}
		catch
		{
			// Temp cleanup best effort.
		}
	}
}
