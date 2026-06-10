using ReVitae.Core.Export;

namespace ReVitae.Tests.Export;

/// <summary>
/// 047 QG1 — golden layout-signature guard. Every template's deterministic render signature
/// (text + rounded word positions, see <see cref="CvTemplateRenderSignature"/>) must match the
/// committed golden. A behaviour-preserving refactor keeps every signature unchanged; an
/// intentional layout change updates the golden file in the same reviewed commit.
/// </summary>
public sealed class CvTemplateRenderGoldenTests
{
	private const string GoldenRelativePath = "tests/ReVitae.Tests/Export/Goldens/template-render-signatures.txt";

	[Fact]
	public void EveryTemplate_MatchesGoldenSignature()
	{
		var golden = LoadGolden();
		var mismatches = new List<string>();

		foreach (var templateId in Enum.GetValues<CvExportTemplateId>())
		{
			var key = templateId.ToString();
			Assert.True(golden.ContainsKey(key), $"Golden missing signature for template '{key}'.");

			var actual = CvTemplateRenderSignature.Compute(templateId);
			if (!string.Equals(actual, golden[key], StringComparison.Ordinal))
			{
				mismatches.Add($"{key}: expected {golden[key]} but rendered {actual}");
			}
		}

		Assert.True(
			mismatches.Count == 0,
			"Template render signatures changed (update the golden only for an intentional layout change):\n"
			+ string.Join("\n", mismatches));
	}

	[Fact]
	public void Golden_CoversExactlyTheCurrentTemplateSet()
	{
		var golden = LoadGolden();
		var templates = Enum.GetValues<CvExportTemplateId>().Select(id => id.ToString()).ToHashSet(StringComparer.Ordinal);

		Assert.Equal(templates.Count, golden.Count);
		Assert.All(golden.Keys, key => Assert.Contains(key, templates));
	}

	[Fact]
	public void Signature_IsDeterministic()
	{
		var first = CvTemplateRenderSignature.Compute(CvExportTemplateId.ClassicSidebar);
		var second = CvTemplateRenderSignature.Compute(CvExportTemplateId.ClassicSidebar);
		Assert.Equal(first, second);
	}

	[Fact]
	public void Signature_DiffersBetweenDifferentLayouts()
	{
		var sidebar = CvTemplateRenderSignature.Compute(CvExportTemplateId.ClassicSidebar);
		var centered = CvTemplateRenderSignature.Compute(CvExportTemplateId.CenteredMinimal);
		Assert.NotEqual(sidebar, centered);
	}

	private static IReadOnlyDictionary<string, string> LoadGolden()
	{
		var path = Path.Combine(FindRepoRoot(), GoldenRelativePath);
		Assert.True(File.Exists(path), $"Golden signature file not found at {path}.");

		return File.ReadAllLines(path)
			.Where(line => !string.IsNullOrWhiteSpace(line))
			.Select(line => line.Split('=', 2))
			.ToDictionary(parts => parts[0], parts => parts[1], StringComparer.Ordinal);
	}

	private static string FindRepoRoot()
	{
		var directory = new DirectoryInfo(AppContext.BaseDirectory);
		while (directory is not null)
		{
			if (File.Exists(Path.Combine(directory.FullName, "Version.props")))
			{
				return directory.FullName;
			}

			directory = directory.Parent;
		}

		throw new InvalidOperationException("Could not locate repository root.");
	}
}
