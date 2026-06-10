using ReVitae.Core.Export;
using ReVitae.Core.Export.Fixtures;

namespace ReVitae.Tests.Export;

public sealed class CvExportDocumentHashTests
{
	[Fact]
	public void Compute_SameDocument_SameHash()
	{
		var a = JohnDoeMinimalArchitectCvDataset.CreateDocument(CvExportTemplateId.ClassicSidebar);
		var b = JohnDoeMinimalArchitectCvDataset.CreateDocument(CvExportTemplateId.ClassicSidebar);
		Assert.Equal(CvExportDocumentHash.Compute(a), CvExportDocumentHash.Compute(b));
	}

	[Fact]
	public void Compute_DifferentTemplate_DifferentHash()
	{
		var a = JohnDoeMinimalArchitectCvDataset.CreateDocument(CvExportTemplateId.ClassicSidebar);
		var b = JohnDoeMinimalArchitectCvDataset.CreateDocument(CvExportTemplateId.CenteredMinimal);
		Assert.NotEqual(CvExportDocumentHash.Compute(a), CvExportDocumentHash.Compute(b));
	}

	[Fact]
	public void Compute_DifferentContent_DifferentHash()
	{
		var a = JohnDoeMinimalArchitectCvDataset.CreateDocument(CvExportTemplateId.ClassicSidebar);
		var b = a with { FirstName = "Changed" };
		Assert.NotEqual(CvExportDocumentHash.Compute(a), CvExportDocumentHash.Compute(b));
	}

	[Fact]
	public void Compute_Null_Throws()
	{
		Assert.Throws<ArgumentNullException>(() => CvExportDocumentHash.Compute(null!));
	}

	[Fact]
	public void Compute_ReturnsHexString()
	{
		var hash = CvExportDocumentHash.Compute(
			JohnDoeMinimalArchitectCvDataset.CreateDocument(CvExportTemplateId.ClassicSidebar));
		Assert.Equal(64, hash.Length); // SHA-256 -> 64 hex chars
		Assert.Matches("^[0-9A-F]+$", hash);
	}
}
