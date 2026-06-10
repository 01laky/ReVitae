using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using ReVitae.Core.Localization;

namespace ReVitae.Tests;

/// <summary>
/// 047 T5 — orphan translation-key audit. Flags <see cref="TranslationKeys"/> constants that are
/// declared but never referenced by production source (`src/`, excluding the key/dictionary files
/// themselves). The current orphans are an explicit allow-list — documented debt or keys reserved
/// for imminent UI use — and the test fails if a <b>new</b> orphan appears, so dead strings cannot
/// silently accumulate.
/// </summary>
public sealed class TranslationKeyOrphanAuditTests
{
	// Known unreferenced keys (reviewed): month/year field labels, a few reserved AI labels,
	// and legacy strings kept for imminent re-use. Remove from here when a key is wired up.
	private static readonly HashSet<string> KnownOrphanAllowList = new(StringComparer.Ordinal)
	{
		"AiCvMeasurableResults", "AiImportEnhancePartial", "AiSetupPullComplete", "AiSetupPullProgress",
		"AiSetupSystemSummary", "CertificatesExpirationMonth", "CertificatesExpirationYear",
		"CertificatesIssueMonth", "CertificatesIssueYear", "EducationEndMonth", "EducationEndYear",
		"EducationStartMonth", "EducationStartYear", "ExportCv", "ExportImageDeliverySeparateHint",
		"ExportImageDeliveryZipHint", "ExportPdf", "ExportedPdfTo", "FirstLaunchAiWizardCompleteSkipped",
		"FirstLaunchAiWizardOfflineConfirm", "FirstLaunchAiWizardSkipConfirm", "ImportAiReviewEmptyDash",
		"ImportAiReviewEntryCount", "ImportAiReviewPersonalComplete", "ImportAiReviewPersonalPartial",
		"ImportAiReviewSummaryAfter", "ImportAiReviewSummaryBefore", "ImportAiTryButton",
		"ImportDefaultSkillsCategory", "ImportWarningUnmappedTextAppended", "ImportWarningWorkExperiencePartial",
		"ProjectOpenEmpty", "ProjectUnsavedIndicator", "ProjectsEndMonth", "ProjectsEndYear",
		"ProjectsStartMonth", "ProjectsStartYear", "SkillsProficiency", "SkillsYearsOfExperience",
		"ValidationProjectsBulkTechnologiesMax", "ValidationSkillsBulkSkillsMax", "WorkExperienceEndMonth",
		"WorkExperienceEndYear", "WorkExperienceStartMonth", "WorkExperienceStartYear",
	};

	[Fact]
	public void NoNewOrphanTranslationKeys()
	{
		var unexpected = ComputeOrphans()
			.Where(key => !KnownOrphanAllowList.Contains(key))
			.OrderBy(k => k, StringComparer.Ordinal)
			.ToList();

		Assert.True(
			unexpected.Count == 0,
			"New orphan translation keys — reference them in code/XAML, or add to the allow-list "
			+ "if reserved for imminent use:\n  " + string.Join("\n  ", unexpected));
	}

	private static IReadOnlyCollection<string> ComputeOrphans()
	{
		var keyNames = typeof(TranslationKeys)
			.GetFields(BindingFlags.Public | BindingFlags.Static)
			.Where(f => f is { IsLiteral: true, IsInitOnly: false } && f.FieldType == typeof(string))
			.Select(f => f.Name)
			.ToList();

		var src = Path.Combine(FindRepoRoot(), "src");
		var blob = new StringBuilder();
		foreach (var file in Directory.EnumerateFiles(src, "*.*", SearchOption.AllDirectories))
		{
			if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
				|| file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
			{
				continue;
			}

			if (!file.EndsWith(".cs", StringComparison.Ordinal) && !file.EndsWith(".axaml", StringComparison.Ordinal))
			{
				continue;
			}

			var name = Path.GetFileName(file);
			if (name is "TranslationKeys.cs" or "AppLocalizer.cs")
			{
				continue;
			}

			blob.Append(File.ReadAllText(file)).Append('\n');
		}

		var text = blob.ToString();
		return keyNames
			.Where(key => !Regex.IsMatch(text, $@"\b{Regex.Escape(key)}\b"))
			.ToList();
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
