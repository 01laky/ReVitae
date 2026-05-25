using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Import;
using ReVitae.Core.Import.Extraction;

namespace ReVitae.Tests.Import.Extraction;

public sealed class WorkExperienceImportExtractorEdgeCaseTests
{
	private static IReadOnlyList<WorkExperienceEntry> ExtractFromCv(string text, IEnumerable<string>? sidebarSkills = null)
	{
		var segmentation = CvSectionSegmenter.Segment(CvTextNormalizer.Normalize(text));
		var sidebar = sidebarSkills?.ToHashSet(StringComparer.OrdinalIgnoreCase)
			?? ImportFieldExtractionCore.CollectSidebarSkillTokens(
				ImportFieldExtractionCore.GetBody(segmentation, CvImportSectionId.Skills));
		var orphanDates = ImportFieldExtractionCore.CollectOrphanWorkDateFragments(segmentation.HeaderBlock);
		var body = ImportFieldExtractionCore.GetBody(segmentation, CvImportSectionId.WorkExperience);
		return WorkExperienceImportExtractor.Extract(body, sidebar, orphanDates);
	}

	private static IReadOnlyList<WorkExperienceEntry> Extract(
		string body,
		IEnumerable<string>? sidebarSkills = null,
		IEnumerable<string>? orphanDates = null) =>
		WorkExperienceImportExtractor.Extract(
			body,
			(sidebarSkills ?? []).ToHashSet(StringComparer.OrdinalIgnoreCase),
			new Queue<string>(orphanDates ?? []));

	[Fact]
	public void Extract_ParsesDateRangeAndJobTitle()
	{
		const string body = """
            Senior Engineer at Acme Corp
            Jan 2020 - Mar 2024
            Built reliable systems.
            """;

		var entry = Assert.Single(Extract(body));

		Assert.Equal("Senior Engineer", entry.JobTitle);
		Assert.Equal("Acme Corp", entry.Company);
		Assert.Equal(1, entry.StartMonth);
		Assert.Equal(2020, entry.StartYear);
		Assert.Equal(3, entry.EndMonth);
		Assert.Equal(2024, entry.EndYear);
	}

	[Fact]
	public void Extract_ParsesCompanyAndLocationBeforeDateLine()
	{
		const string text = """
            Jane Doe
            jane@example.com

            Work Experience
            Excalibur s.r.o. - Senior full stack developer
            Kosice, Slovakia
            01/2024 - 05/2026
            Developed backend services in Go and Node.js.
            """;

		var entry = Assert.Single(ExtractFromCv(text));

		Assert.Equal("Senior full stack developer", entry.JobTitle);
		Assert.Equal("Excalibur s.r.o.", entry.Company);
		Assert.Equal("Kosice, Slovakia", entry.Location);
	}

	[Fact]
	public void Extract_CollectsOrphanDateFragmentsFromHeader()
	{
		const string header = "Jane Doe\n/ 2020 - 05 / 2024";

		var fragments = ImportFieldExtractionCore.CollectOrphanWorkDateFragments(header);

		Assert.NotEmpty(fragments);
	}

	[Fact]
	public void Extract_FiltersSidebarSkillTokensFromBody()
	{
		const string text = """
            Jane Doe
            jane@example.com

            Skills
            C#
            Avalonia

            Work Experience
            2021
            Engineer
            Acme
            C#
            Avalonia
            """;

		var entries = ExtractFromCv(text);

		Assert.Single(entries);
		Assert.Contains("Engineer", entries[0].JobTitle, StringComparison.Ordinal);
	}

	[Fact]
	public void Extract_EmptyBody_ReturnsEmpty()
	{
		Assert.Empty(Extract(string.Empty));
	}

	[Fact]
	public void Extract_MultipleEntriesSeparatedByBlankLines()
	{
		const string body = """
            First Role
            Company A
            2018 - 2019

            Second Role
            Company B
            2020 - 2021
            """;

		var entries = Extract(body);

		Assert.Equal(2, entries.Count);
		Assert.Equal("First Role", entries[0].JobTitle);
		Assert.Equal("Second Role", entries[1].JobTitle);
	}

	[Fact]
	public void Extract_PreservesAchievementBullets()
	{
		const string text = """
            Jane Doe
            jane@example.com

            Work Experience
            Senior Developer at Acme
            Jan 2020 - Mar 2024
            - Built platform APIs
            - Improved CI pipeline
            """;

		var entry = Assert.Single(ExtractFromCv(text));

		Assert.Contains("Built platform APIs", entry.Achievements, StringComparison.Ordinal);
	}

	[Fact]
	public void Extract_CurrentRoleWithoutEndDate()
	{
		const string body = """
            Consultant
            Freelance
            03/2023 - Present
            """;

		var entry = Assert.Single(Extract(body));

		Assert.True(entry.IsCurrentlyWorking);
		Assert.Equal(3, entry.StartMonth);
		Assert.Equal(2023, entry.StartYear);
	}
}
