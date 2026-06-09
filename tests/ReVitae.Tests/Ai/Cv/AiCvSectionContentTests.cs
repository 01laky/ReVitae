using ReVitae.Core.Ai.Cv;
using ReVitae.Core.Cv.Skills;
using ReVitae.Core.Export;
using ReVitae.Core.Import;
using CvWorkExperienceEntry = ReVitae.Core.Cv.WorkExperience.WorkExperienceEntry;
using CvEducationEntry = ReVitae.Core.Cv.Education.EducationEntry;
using SkillItem = ReVitae.Core.Cv.Skills.SkillItem;

namespace ReVitae.Tests.Ai.Cv;

public sealed class AiCvSectionContentTests
{
	[Fact]
	public void AdvisorSections_ContainExpectedSet()
	{
		Assert.Contains(CvImportSectionId.Skills, AiCvSectionContent.AdvisorSections);
		Assert.Contains(CvImportSectionId.Summary, AiCvSectionContent.AdvisorSections);
		Assert.DoesNotContain(CvImportSectionId.Certificates, AiCvSectionContent.AdvisorSections);
	}

	[Fact]
	public void Describe_Work_IncludesTitleCompanyAndDescription()
	{
		var snapshot = Empty() with
		{
			WorkExperience =
			[
				new CvWorkExperienceEntry("w1") { JobTitle = "Engineer", Company = "Acme", Description = "Built APIs." },
			],
		};

		var text = AiCvSectionContent.Describe(CvImportSectionId.WorkExperience, snapshot);

		Assert.Contains("Engineer", text, StringComparison.Ordinal);
		Assert.Contains("Acme", text, StringComparison.Ordinal);
		Assert.Contains("Built APIs.", text, StringComparison.Ordinal);
	}

	[Fact]
	public void Describe_Skills_ListsCategoryAndSkills()
	{
		var group = new SkillsGroupEntry { Category = "Backend" };
		group.Skills.Add(new SkillItem { Name = "C#" });
		group.Skills.Add(new SkillItem { Name = "SQL" });
		var snapshot = Empty() with { Skills = [group] };

		var text = AiCvSectionContent.Describe(CvImportSectionId.Skills, snapshot);

		Assert.Contains("Backend", text, StringComparison.Ordinal);
		Assert.Contains("C#", text, StringComparison.Ordinal);
		Assert.Contains("SQL", text, StringComparison.Ordinal);
	}

	[Fact]
	public void Measure_EmptySection_IsEmpty()
	{
		var metrics = AiCvSectionContent.Measure(CvImportSectionId.Education, Empty());
		Assert.True(metrics.IsEmpty);
		Assert.Equal(0, metrics.EntryCount);
	}

	[Fact]
	public void Measure_Education_CountsEntries()
	{
		var snapshot = Empty() with
		{
			Education = [new CvEducationEntry("e1") { Degree = "BSc", Institution = "MIT" }],
		};

		var metrics = AiCvSectionContent.Measure(CvImportSectionId.Education, snapshot);
		Assert.Equal(1, metrics.EntryCount);
		Assert.True(metrics.NonWhitespaceCharCount > 0);
	}

	[Fact]
	public void Measure_Summary_CountsAsOneEntryWhenPresent()
	{
		var snapshot = Empty() with { Personal = new PersonalInformationImport { ShortSummary = "Senior engineer." } };
		var metrics = AiCvSectionContent.Measure(CvImportSectionId.Summary, snapshot);
		Assert.Equal(1, metrics.EntryCount);
	}

	private static CvExportSourceData Empty() =>
		new(new PersonalInformationImport(), [], [], [], [], [], [], [], null);
}
