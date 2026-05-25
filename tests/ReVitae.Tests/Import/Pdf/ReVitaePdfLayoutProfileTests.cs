using ReVitae.Core.Export;
using ReVitae.Core.Import.Pdf;

namespace ReVitae.Tests.Import.Pdf;

public sealed class ReVitaePdfLayoutProfileTests
{
	[Fact]
	public void MatrixPdfTemplateIds_ContainsTwelveTemplates()
	{
		Assert.Equal(12, ReVitaePdfLayoutProfiles.MatrixPdfTemplateIds.Count);
	}

	[Theory]
	[MemberData(nameof(MatrixTemplateIds))]
	public void EveryMatrixPdfTemplate_HasDefinedProfile(CvExportTemplateId templateId)
	{
		var profile = ReVitaePdfLayoutProfiles.Get(templateId);
		Assert.NotNull(profile);

		switch (templateId)
		{
			case CvExportTemplateId.CenteredMinimal:
			case CvExportTemplateId.CleanTopHeader:
				Assert.Equal(ReVitaePdfColumnKind.SingleColumn, profile.ColumnKind);
				Assert.Null(profile.SidebarWidthRatio);
				break;
			case CvExportTemplateId.PhotoLeftBand:
				Assert.Equal(ReVitaePdfColumnKind.PhotoLeftBand, profile.ColumnKind);
				Assert.Equal(ReVitaePdfLayoutProfiles.PhotoLeftBandRatio, profile.SidebarWidthRatio);
				break;
			case CvExportTemplateId.NavyProfileSplit:
				Assert.Equal(ReVitaePdfColumnKind.NavyProfileSplit, profile.ColumnKind);
				break;
			case CvExportTemplateId.ClassicSidebar:
				Assert.Equal(0.36, profile.SidebarWidthRatio);
				break;
			case CvExportTemplateId.ExecutiveBlueSidebar:
				Assert.Equal(ReVitaePdfLayoutProfiles.ExecutiveBlueSidebarRatio, profile.SidebarWidthRatio);
				break;
			default:
				Assert.Equal(ReVitaePdfColumnKind.TwoColumnSidebar, profile.ColumnKind);
				Assert.Equal(ReVitaePdfLayoutProfiles.DefaultSidebarRatio, profile.SidebarWidthRatio);
				break;
		}
	}

	[Fact]
	public void ForHints_UsesTemplateProfileWhenTemplateIdPresent()
	{
		var hints = new ReVitaePdfExportHints(true, CvExportTemplateId.ExecutiveBlueSidebar, null, false);
		var profile = ReVitaePdfLayoutProfiles.ForHints(hints);
		Assert.Equal(ReVitaePdfLayoutProfiles.ExecutiveBlueSidebarRatio, profile.SidebarWidthRatio);
	}

	public static IEnumerable<object[]> MatrixTemplateIds =>
		ReVitaePdfLayoutProfiles.MatrixPdfTemplateIds.Select(id => new object[] { id });
}
