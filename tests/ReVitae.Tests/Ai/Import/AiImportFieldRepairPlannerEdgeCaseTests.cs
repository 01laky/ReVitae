using ReVitae.Core.Ai.Import;
using ReVitae.Core.Import;

namespace ReVitae.Tests.Ai.Import;

public sealed class AiImportFieldRepairPlannerEdgeCaseTests
{
	private static AiImportFieldRepairTarget T(string value, CvImportConfidence c = CvImportConfidence.Low, CvImportSectionId section = CvImportSectionId.PersonalInformation) =>
		new(section, $"f.{value}", null, value, c);

	[Fact]
	public void SelectTargets_ExactlyAtCap_NoDrop()
	{
		var targets = Enumerable.Range(0, AiImportLimits.MaxRepairFields).Select(i => T($"v{i}")).ToList();
		var selected = AiImportFieldRepairPlanner.SelectTargets(targets, out var dropped);
		Assert.Equal(AiImportLimits.MaxRepairFields, selected.Count);
		Assert.Equal(0, dropped);
	}

	[Fact]
	public void SelectTargets_OneOverCap_DropsOne()
	{
		var targets = Enumerable.Range(0, AiImportLimits.MaxRepairFields + 1).Select(i => T($"v{i}")).ToList();
		var selected = AiImportFieldRepairPlanner.SelectTargets(targets, out var dropped);
		Assert.Equal(AiImportLimits.MaxRepairFields, selected.Count);
		Assert.Equal(1, dropped);
	}

	[Fact]
	public void SelectTargets_Empty_ReturnsEmptyNoDrop()
	{
		var selected = AiImportFieldRepairPlanner.SelectTargets([], out var dropped);
		Assert.Empty(selected);
		Assert.Equal(0, dropped);
	}

	[Fact]
	public void SelectTargets_AllWhitespaceValues_AllDropped()
	{
		var targets = new[] { T("   "), T("\t") };
		var selected = AiImportFieldRepairPlanner.SelectTargets(targets, out _);
		Assert.Empty(selected);
	}

	[Fact]
	public void SelectTargets_MixedConfidence_LowFirstThenMediumThenHigh()
	{
		var targets = new[]
		{
			T("high", CvImportConfidence.High),
			T("low", CvImportConfidence.Low),
			T("medium", CvImportConfidence.Medium),
		};
		var selected = AiImportFieldRepairPlanner.SelectTargets(targets, out _);
		Assert.Equal(CvImportConfidence.Low, selected[0].Confidence);
		Assert.Equal(CvImportConfidence.Medium, selected[1].Confidence);
		Assert.Equal(CvImportConfidence.High, selected[2].Confidence);
	}

	[Fact]
	public void GroupBySection_MultipleSections_SeparateGroups()
	{
		var targets = new[]
		{
			T("a", section: CvImportSectionId.PersonalInformation),
			T("b", section: CvImportSectionId.WorkExperience),
			T("c", section: CvImportSectionId.PersonalInformation),
		};
		var groups = AiImportFieldRepairPlanner.GroupBySection(targets);
		Assert.Equal(2, groups.Count);
		Assert.Equal(2, groups.First(g => g[0].Section == CvImportSectionId.PersonalInformation).Count);
	}

	[Fact]
	public void GroupBySection_SingleSection_OneGroup()
	{
		var groups = AiImportFieldRepairPlanner.GroupBySection([T("a"), T("b")]);
		Assert.Single(groups);
	}
}
