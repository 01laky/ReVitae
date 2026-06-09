using ReVitae.Core.Ai.Cv;
using ReVitae.Core.Cv.Skills;
using ReVitae.Core.Export;
using ReVitae.Core.Import;
using CvWorkExperienceEntry = ReVitae.Core.Cv.WorkExperience.WorkExperienceEntry;
using CvEducationEntry = ReVitae.Core.Cv.Education.EducationEntry;
using CvLanguageEntry = ReVitae.Core.Cv.Languages.LanguageEntry;
using CvProjectEntry = ReVitae.Core.Cv.Projects.ProjectEntry;
using SkillItem = ReVitae.Core.Cv.Skills.SkillItem;

namespace ReVitae.Tests.Ai.Cv;

public sealed class AiCvSectionContentEdgeCaseTests
{
	private static CvExportSourceData Empty() =>
		new(new PersonalInformationImport(), [], [], [], [], [], [], [], null);

	[Fact]
	public void Describe_Languages_IncludesLanguageAndProficiency()
	{
		var snapshot = Empty() with
		{
			Languages = [new CvLanguageEntry("l1") { Language = "Slovak" }],
		};
		var text = AiCvSectionContent.Describe(CvImportSectionId.Languages, snapshot);
		Assert.Contains("Slovak", text, StringComparison.Ordinal);
	}

	[Fact]
	public void Describe_Projects_IncludesNameRoleAndDescription()
	{
		var snapshot = Empty() with
		{
			Projects = [new CvProjectEntry("p1") { Name = "Atlas", Role = "Lead", Description = "Built the core." }],
		};
		var text = AiCvSectionContent.Describe(CvImportSectionId.Projects, snapshot);
		Assert.Contains("Atlas", text, StringComparison.Ordinal);
		Assert.Contains("Lead", text, StringComparison.Ordinal);
		Assert.Contains("Built the core.", text, StringComparison.Ordinal);
	}

	[Fact]
	public void Describe_Work_AppendsAchievements()
	{
		var snapshot = Empty() with
		{
			WorkExperience =
			[
				new CvWorkExperienceEntry("w1")
				{
					JobTitle = "Engineer",
					Company = "Acme",
					Description = "Did work.",
					Achievements = "Shipped v2.",
				},
			],
		};
		var text = AiCvSectionContent.Describe(CvImportSectionId.WorkExperience, snapshot);
		Assert.Contains("Shipped v2.", text, StringComparison.Ordinal);
	}

	[Fact]
	public void Describe_Skills_FiltersEmptySkillNames()
	{
		var group = new SkillsGroupEntry { Category = "Backend" };
		group.Skills.Add(new SkillItem { Name = "C#" });
		group.Skills.Add(new SkillItem { Name = "  " });
		var snapshot = Empty() with { Skills = [group] };

		var text = AiCvSectionContent.Describe(CvImportSectionId.Skills, snapshot);
		Assert.Contains("C#", text, StringComparison.Ordinal);
		Assert.DoesNotContain(",  ,", text, StringComparison.Ordinal);
	}

	[Fact]
	public void Describe_NonAdvisorSection_ReturnsEmpty()
	{
		Assert.Equal(string.Empty, AiCvSectionContent.Describe(CvImportSectionId.Certificates, Empty()));
		Assert.Equal(string.Empty, AiCvSectionContent.Describe(CvImportSectionId.Links, Empty()));
	}

	[Theory]
	[InlineData(CvImportSectionId.Certificates)]
	[InlineData(CvImportSectionId.Links)]
	[InlineData(CvImportSectionId.Contact)]
	public void IsAdvisorSection_NonScopeSections_False(CvImportSectionId section)
	{
		Assert.False(AiCvSectionContent.IsAdvisorSection(section));
	}

	[Fact]
	public void CountEntries_MatchesEachSection()
	{
		var snapshot = Empty() with
		{
			WorkExperience = [new CvWorkExperienceEntry("w1") { JobTitle = "X" }],
			Education = [new CvEducationEntry("e1") { Degree = "BSc" }],
		};
		Assert.Equal(1, AiCvSectionContent.CountEntries(CvImportSectionId.WorkExperience, snapshot));
		Assert.Equal(1, AiCvSectionContent.CountEntries(CvImportSectionId.Education, snapshot));
		Assert.Equal(0, AiCvSectionContent.CountEntries(CvImportSectionId.Skills, snapshot));
	}

	[Fact]
	public void Measure_SummaryWhitespaceOnly_IsEmpty()
	{
		var snapshot = Empty() with { Personal = new PersonalInformationImport { ShortSummary = "   " } };
		Assert.True(AiCvSectionContent.Measure(CvImportSectionId.Summary, snapshot).IsEmpty);
	}

	[Fact]
	public void Describe_EmptyWork_ReturnsEmpty()
	{
		Assert.Equal(string.Empty, AiCvSectionContent.Describe(CvImportSectionId.WorkExperience, Empty()));
	}
}
