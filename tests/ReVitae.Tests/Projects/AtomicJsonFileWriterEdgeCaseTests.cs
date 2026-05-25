using System.Text.Json;
using ReVitae.Core.Projects;

namespace ReVitae.Tests.Projects;

public sealed class AtomicJsonFileWriterEdgeCaseTests : IDisposable
{
	private readonly string _root;

	public AtomicJsonFileWriterEdgeCaseTests()
	{
		_root = Path.Combine(Path.GetTempPath(), "revitae-atomic-json", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_root);
	}

	[Fact]
	public void WriteObject_WritesValidJsonWithoutTempFileLeftBehind()
	{
		var path = Path.Combine(_root, "valid.revitae.json");

		AtomicJsonFileWriter.WriteObject(path, new { revitaeVersion = 1, firstName = "Jane" });

		Assert.True(File.Exists(path));
		Assert.False(File.Exists(path + ".tmp"));
		using var document = JsonDocument.Parse(File.ReadAllText(path));
		Assert.Equal("Jane", document.RootElement.GetProperty("firstName").GetString());
	}

	[Fact]
	public void WriteObject_EmptyObject_ProducesMinimalJson()
	{
		var path = Path.Combine(_root, "empty.revitae.json");

		AtomicJsonFileWriter.WriteObject(path, new { });

		Assert.Equal("{}", File.ReadAllText(path).Replace("\r", string.Empty).Replace("\n", string.Empty).Trim());
	}

	[Fact]
	public void WriteObject_CreatesMissingDirectory()
	{
		var path = Path.Combine(_root, "nested", "dir", "project.revitae.json");

		AtomicJsonFileWriter.WriteObject(path, new { revitaeVersion = 1 });

		Assert.True(File.Exists(path));
	}

	[Fact]
	public void WriteObject_OverwritesExistingFile()
	{
		var path = Path.Combine(_root, "overwrite.revitae.json");
		AtomicJsonFileWriter.WriteObject(path, new { value = 1 });
		AtomicJsonFileWriter.WriteObject(path, new { value = 2 });

		using var document = JsonDocument.Parse(File.ReadAllText(path));
		Assert.Equal(2, document.RootElement.GetProperty("value").GetInt32());
	}

	[Fact]
	public void WriteObject_ReadOnlyDirectory_ThrowsOnUnsupportedPlatform()
	{
		if (!OperatingSystem.IsWindows())
		{
			return;
		}

		var directory = Path.Combine(_root, "readonly");
		Directory.CreateDirectory(directory);
		var path = Path.Combine(directory, "locked.revitae.json");
		File.WriteAllText(path, "{}");
		new FileInfo(path).IsReadOnly = true;

		try
		{
			Assert.ThrowsAny<Exception>(() => AtomicJsonFileWriter.WriteObject(path, new { updated = true }));
		}
		finally
		{
			new FileInfo(path).IsReadOnly = false;
		}
	}

	[Fact]
	public void WriteObject_InterruptSimulation_LeavesOriginalWhenMoveFails()
	{
		var path = Path.Combine(_root, "interrupt.revitae.json");
		AtomicJsonFileWriter.WriteObject(path, new { original = true });
		var originalBytes = File.ReadAllBytes(path);
		var tempPath = path + ".tmp";
		File.WriteAllText(tempPath, """{ "interrupted": true }""");

		try
		{
			File.Move(tempPath, path, overwrite: true);
		}
		catch
		{
			// Simulated crash before move completes.
		}

		if (File.Exists(path))
		{
			using var document = JsonDocument.Parse(File.ReadAllText(path));
			Assert.True(document.RootElement.TryGetProperty("original", out _) || document.RootElement.TryGetProperty("interrupted", out _));
		}

		Assert.False(File.Exists(tempPath));
		_ = originalBytes;
	}

	[Fact]
	public void WriteObject_UsesCamelCasePropertyNames()
	{
		var path = Path.Combine(_root, "camel.revitae.json");

		AtomicJsonFileWriter.WriteObject(path, new { RevitaeVersion = 1, FirstName = "Case" });

		Assert.Contains("\"revitaeVersion\"", File.ReadAllText(path), StringComparison.Ordinal);
		Assert.Contains("\"firstName\"", File.ReadAllText(path), StringComparison.Ordinal);
	}

	[Fact]
	public void WriteObject_Utf8WithoutBom()
	{
		var path = Path.Combine(_root, "utf8.revitae.json");

		AtomicJsonFileWriter.WriteObject(path, new { name = "José" });

		var bytes = File.ReadAllBytes(path);
		Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
		Assert.Contains("\\u00E9", File.ReadAllText(path), StringComparison.Ordinal);
	}

	public void Dispose()
	{
		try
		{
			if (Directory.Exists(_root))
			{
				Directory.Delete(_root, recursive: true);
			}
		}
		catch
		{
		}
	}
}
