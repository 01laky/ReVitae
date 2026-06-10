using ReVitae.Core.Export;
using ReVitae.Core.Export.Fixtures;

namespace ReVitae.Tests.Export;

public sealed class CvExportDocumentHashEdgeCaseTests
{
	private static CvExportDocument Doc() =>
		JohnDoeMinimalArchitectCvDataset.CreateDocument(CvExportTemplateId.ClassicSidebar);

	[Fact]
	public void Compute_IsStableAcrossManyCalls()
	{
		var doc = Doc();
		var first = CvExportDocumentHash.Compute(doc);
		for (var i = 0; i < 10; i++)
		{
			Assert.Equal(first, CvExportDocumentHash.Compute(doc));
		}
	}

	[Fact]
	public void Compute_WhitespaceFieldChange_DifferentHash()
	{
		var a = Doc();
		var b = a with { FirstName = a.FirstName + " " };
		Assert.NotEqual(CvExportDocumentHash.Compute(a), CvExportDocumentHash.Compute(b));
	}

	[Fact]
	public void Compute_PhotoPathChange_DifferentHash()
	{
		var a = Doc();
		var b = a with { PhotoPath = "/some/photo.png" };
		Assert.NotEqual(CvExportDocumentHash.Compute(a), CvExportDocumentHash.Compute(b));
	}

	[Fact]
	public void Compute_ReorderedWorkEntries_DifferentHash()
	{
		var a = Doc();
		if (a.WorkExperienceEntries.Count < 2)
		{
			return; // dataset has multiple entries; guard anyway
		}

		var reordered = a.WorkExperienceEntries.Reverse().ToList();
		var b = a with { WorkExperienceEntries = reordered };
		Assert.NotEqual(CvExportDocumentHash.Compute(a), CvExportDocumentHash.Compute(b));
	}

	[Fact]
	public void Compute_FewerWorkEntries_DifferentHash()
	{
		var a = Doc();
		if (a.WorkExperienceEntries.Count < 2)
		{
			return;
		}

		var b = a with { WorkExperienceEntries = a.WorkExperienceEntries.Take(1).ToList() };
		Assert.NotEqual(CvExportDocumentHash.Compute(a), CvExportDocumentHash.Compute(b));
	}

	[Fact]
	public void Compute_SummaryChange_DifferentHash()
	{
		var a = Doc();
		var b = a with { ShortSummary = (a.ShortSummary ?? string.Empty) + " extra clause." };
		Assert.NotEqual(CvExportDocumentHash.Compute(a), CvExportDocumentHash.Compute(b));
	}

	[Fact]
	public void Compute_OnlyTemplateDiffers_DifferentHash()
	{
		var a = Doc();
		var b = a with { TemplateId = CvExportTemplateId.CenteredMinimal };
		Assert.NotEqual(CvExportDocumentHash.Compute(a), CvExportDocumentHash.Compute(b));
	}

	[Fact]
	public void Compute_IdenticalClonedContent_SameHash()
	{
		var a = Doc();
		var b = a with { };
		Assert.Equal(CvExportDocumentHash.Compute(a), CvExportDocumentHash.Compute(b));
	}
}
