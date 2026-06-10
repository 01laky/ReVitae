using System.Text.Json;
using ReVitae.Core.Export;
using ReVitae.Core.Projects;
using ReVitae.Tests.Export;

namespace ReVitae.Tests.Projects;

/// <summary>
/// Prompt 049 B8 / C3 — project persistence faults against the real filesystem. Atomic writes
/// must fail cleanly on impossible targets and stay atomic under concurrency (the committed file
/// is always one complete valid document, never an interleaved half-write); loads of missing,
/// empty, corrupt, or truncated files degrade to a typed failure.
/// </summary>
[Trait("Category", "Projects")]
public sealed class ProjectPersistenceFaultTests : IDisposable
{
	private readonly string _directory;

	public ProjectPersistenceFaultTests()
	{
		_directory = Path.Combine(Path.GetTempPath(), "revitae-persist-fault", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_directory);
	}

	public void Dispose()
	{
		try
		{
			Directory.Delete(_directory, recursive: true);
		}
		catch
		{
			// Best-effort cleanup.
		}
	}

	private string At(string name) => System.IO.Path.Combine(_directory, name);

	[Fact]
	public void WriteObject_TargetIsADirectory_Throws()
	{
		var target = At("target-as-dir");
		Directory.CreateDirectory(target);

		Assert.ThrowsAny<Exception>(() => AtomicJsonFileWriter.WriteObject(target, new { name = "x" }));
	}

	[Fact]
	public void WriteObject_ParentPathIsAFile_Throws()
	{
		var parent = At("parent-is-file");
		File.WriteAllText(parent, "i am a file");
		var nested = System.IO.Path.Combine(parent, "child.json");

		Assert.ThrowsAny<Exception>(() => AtomicJsonFileWriter.WriteObject(nested, new { name = "x" }));
	}

	[Fact]
	public async Task WriteObject_ConcurrentWriters_LeaveOneCompleteValidFile()
	{
		var target = At("concurrent.json");

		var tasks = Enumerable.Range(0, 16).Select(i => Task.Run(() =>
		{
			try
			{
				AtomicJsonFileWriter.WriteObject(target, new { writer = i, payload = new string('x', 200) });
			}
			catch
			{
				// Concurrent writers may collide on the temp file; atomicity only guarantees the
				// committed target is never a partial write — exceptions here are acceptable.
			}
		}));

		await Task.WhenAll(tasks);

		Assert.True(File.Exists(target));
		var exception = Record.Exception(() => JsonDocument.Parse(File.ReadAllText(target)));
		Assert.Null(exception);
		Assert.False(File.Exists(target + ".tmp"), "An atomic temp file was left behind.");
	}

	[Fact]
	public void Load_MissingFile_ReturnsTypedFailure()
	{
		var result = CvProjectSerializer.Load(At("nope.revitae.json"));

		Assert.False(result.Success);
		Assert.NotNull(result.ErrorMessageKey);
	}

	[Theory]
	[InlineData("")]
	[InlineData("{ not json")]
	[InlineData("{\"personalInformation\": {")]
	[InlineData("\0\0\0 binary garbage \0")]
	public void Load_CorruptOrTruncatedFile_ReturnsTypedFailure(string content)
	{
		var path = At("corrupt.revitae.json");
		File.WriteAllText(path, content);

		var result = CvProjectSerializer.Load(path);

		Assert.False(result.Success);
		Assert.NotNull(result.ErrorMessageKey);
	}

	[Fact]
	public void SaveThenLoad_RoundTripsSuccessfully()
	{
		var path = At("roundtrip.revitae.json");
		var source = CvExportTestFixtures.CreateRepresentativeSourceData();
		var settings = CvProjectSettings.CreateDefault(CvExportTemplateId.ClassicSidebar);

		CvProjectSerializer.Save(path, new CvProjectSaveRequest(source, settings));
		var result = CvProjectSerializer.Load(path);

		Assert.True(result.Success);
		Assert.NotNull(result.Import);
		Assert.Equal(source.Personal.FirstName, result.Import!.Personal.FirstName);
		Assert.Equal(source.Personal.LastName, result.Import!.Personal.LastName);
	}

	[Fact]
	public async Task ConcurrentSaves_LeaveALoadableProject()
	{
		var path = At("concurrent-save.revitae.json");
		var source = CvExportTestFixtures.CreateRepresentativeSourceData();
		var settings = CvProjectSettings.CreateDefault(CvExportTemplateId.ClassicSidebar);

		var tasks = Enumerable.Range(0, 12).Select(_ => Task.Run(() =>
		{
			try
			{
				CvProjectSerializer.Save(path, new CvProjectSaveRequest(source, settings));
			}
			catch
			{
				// Temp-file collisions acceptable; the committed file must stay loadable.
			}
		}));

		await Task.WhenAll(tasks);

		var result = CvProjectSerializer.Load(path);
		Assert.True(result.Success);
	}
}
