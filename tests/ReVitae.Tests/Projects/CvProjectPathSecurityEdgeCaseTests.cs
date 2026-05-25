using ReVitae.Core.Projects;

namespace ReVitae.Tests.Projects;

[Trait("Category", "Projects")]
public sealed class CvProjectPathSecurityEdgeCaseTests : IDisposable
{
	private readonly string _root;

	public CvProjectPathSecurityEdgeCaseTests()
	{
		_root = Path.Combine(Path.GetTempPath(), "revitae-path-security", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_root);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void ValidateOpenPath_EmptyPath_IsInvalid(string? path)
	{
		var result = CvProjectPathValidator.ValidateOpenPath(path);

		Assert.False(result.IsValid);
		Assert.Equal(CvProjectPathValidationFailure.EmptyPath, result.Failure);
	}

	[Theory]
	[InlineData("../outside/project.revitae.json")]
	[InlineData("foo/../../etc/passwd")]
	[InlineData("./../secret.revitae.json")]
	public void ValidateOpenPath_PathTraversal_IsRejected(string path)
	{
		var result = CvProjectPathValidator.ValidateOpenPath(path);

		Assert.False(result.IsValid);
		Assert.Equal(CvProjectPathValidationFailure.PathTraversal, result.Failure);
	}

	[Fact]
	public void ValidateSavePath_WrongExtension_IsRejected()
	{
		var result = CvProjectPathValidator.ValidateSavePath(Path.Combine(_root, "project.txt"));

		Assert.False(result.IsValid);
		Assert.Equal(CvProjectPathValidationFailure.NonRevitaeExtension, result.Failure);
	}

	[Theory]
	[InlineData("project.revitae.json")]
	[InlineData("project.json")]
	public void ValidateSavePath_AllowedExtensions_AreValid(string fileName)
	{
		var path = Path.Combine(_root, fileName);

		var result = CvProjectPathValidator.ValidateSavePath(path);

		Assert.True(result.IsValid);
		Assert.Equal(Path.GetFullPath(path), result.NormalizedPath);
	}

	[Fact]
	public void ValidateOpenPath_ValidPath_NormalizesToFullPath()
	{
		var relative = Path.Combine(_root, "cv.revitae.json");
		File.WriteAllText(relative, "{}");

		var result = CvProjectPathValidator.ValidateOpenPath(relative);

		Assert.True(result.IsValid);
		Assert.Equal(Path.GetFullPath(relative), result.NormalizedPath);
	}

	[Theory]
	[InlineData(".", CvProjectPathValidationFailure.PathTraversal)]
	[InlineData("..", CvProjectPathValidationFailure.PathTraversal)]
	public void ValidateOpenPath_DotSegments_AreRejected(string fileName, CvProjectPathValidationFailure expected)
	{
		var result = CvProjectPathValidator.ValidateOpenPath(fileName);

		Assert.False(result.IsValid);
		Assert.Equal(expected, result.Failure);
	}

	[Fact]
	public void ContainsPathTraversal_DetectsDotDotSegments()
	{
		Assert.True(CvProjectPathValidator.ContainsPathTraversal("../secret"));
		Assert.True(CvProjectPathValidator.ContainsPathTraversal(@"foo\..\bar"));
		Assert.False(CvProjectPathValidator.ContainsPathTraversal("valid/name.revitae.json"));
	}

	[Fact]
	public void ValidateSavePath_CreatesParentDirectoryWhenMissing()
	{
		var lifecycle = new CvProjectLifecycleService(
			new SystemClock(),
			new NoOpAutosaveStore(),
			autosaveIntervalSeconds: 0,
			debounceInterval: TimeSpan.Zero);
		var nestedPath = Path.Combine(_root, "new-dir", "nested", "cv.revitae.json");
		var request = CvProjectLifecycleEdgeCaseTestsHelper.CreateMinimalSaveRequest();

		lifecycle.SaveValidatedProject(nestedPath, request);

		Assert.True(File.Exists(nestedPath));
	}

	[Fact]
	public void LoadValidatedProject_MissingFile_ReturnsRecentMissingError()
	{
		var lifecycle = new CvProjectLifecycleService(new SystemClock(), new NoOpAutosaveStore());
		var missingPath = Path.Combine(_root, "missing.revitae.json");

		var loaded = lifecycle.LoadValidatedProject(missingPath);

		Assert.False(loaded.Success);
		Assert.NotNull(loaded.ErrorMessageKey);
	}

	[Fact]
	public void SaveValidatedProject_InvalidPath_Throws()
	{
		var lifecycle = new CvProjectLifecycleService(new SystemClock(), new NoOpAutosaveStore());
		var request = CvProjectLifecycleEdgeCaseTestsHelper.CreateMinimalSaveRequest();

		Assert.Throws<InvalidOperationException>(() => lifecycle.SaveValidatedProject("../bad.txt", request));
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

	private sealed class NoOpAutosaveStore : IProjectAutosaveStore
	{
		public void WriteRecovery(CvProjectSaveRequest request)
		{
		}

		public void DeleteRecovery()
		{
		}

		public bool RecoveryExists() => false;

		public CvProjectLoadResult LoadRecovery() => new(false, null, null, "missing");

		public DateTimeOffset? GetRecoveryLastWriteUtc() => null;
	}
}

internal static class CvProjectLifecycleEdgeCaseTestsHelper
{
	internal static CvProjectSaveRequest CreateMinimalSaveRequest() =>
		new(
			ReVitae.Core.Export.CvExportSourceDataFactory.Create(
				new ReVitae.Core.Import.PersonalInformationImport { FirstName = "Path", LastName = "Test" },
				[], [], [], [], [], [], [], null),
			CvProjectSettings.CreateDefault(ReVitae.Core.Export.CvExportTemplateId.CleanTopHeader));
}
