using ReVitae.Core.Projects;

namespace ReVitae.Tests.Projects;

[Trait("Category", "Projects")]
public sealed class CvOpenFileRoutingTests
{
	[Theory]
	[InlineData("My Cv.revitae.json", true)]
	[InlineData("project.json", false)]
	[InlineData("resume.pdf", false)]
	[InlineData("notes.txt", false)]
	public void ShouldLoadAsSavedProject_UsesRevitaeExtensionOrSchema(string fileName, bool expected)
	{
		var path = Path.Combine(Path.GetTempPath(), fileName);
		Assert.Equal(expected, CvOpenFileRouting.ShouldLoadAsSavedProject(path));
	}

	[Fact]
	public void ShouldLoadAsSavedProject_DetectsRevitaeJsonWithoutDedicatedExtension()
	{
		var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");
		File.WriteAllText(path, """{"revitaeVersion":2,"cv":{}}""");

		try
		{
			Assert.True(CvOpenFileRouting.ShouldLoadAsSavedProject(path));
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Theory]
	[InlineData("cv.pdf", true)]
	[InlineData("cv.txt", true)]
	[InlineData("cv.docx", true)]
	[InlineData("unknown.xyz", false)]
	public void IsImportableCvFile_RecognizesSupportedExtensions(string fileName, bool expected)
	{
		var path = Path.Combine(Path.GetTempPath(), fileName);
		Assert.Equal(expected, CvOpenFileRouting.IsImportableCvFile(path));
	}
}
