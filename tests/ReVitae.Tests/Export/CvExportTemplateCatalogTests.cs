using ReVitae.Core.Export;
using ReVitae.Core.Import;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Export;

public sealed class CvExportTemplateCatalogTests
{
	[Fact]
	public void All_ContainsEveryTemplate()
	{
		Assert.Equal(106, CvExportTemplateCatalog.All.Count);
		Assert.Equal(Enum.GetValues<CvExportTemplateId>().Length, CvExportTemplateCatalog.All.Count);
		Assert.Equal(90, CvThemedTemplateRegistry.All.Count);
	}

	[Fact]
	public void All_EveryTemplateHasResolvableLocalizationKeys()
	{
		var localizer = new AppLocalizer("en");

		foreach (var template in CvExportTemplateCatalog.All)
		{
			Assert.False(string.IsNullOrWhiteSpace(localizer.Get(template.NameKey)));
			Assert.False(string.IsNullOrWhiteSpace(localizer.Get(template.DescriptionKey)));
			Assert.Matches(@"^#[0-9A-Fa-f]{6}$", template.AccentColor);
		}
	}

	[Theory]
	[InlineData(CvExportTemplateId.ClassicSidebar)]
	[InlineData(CvExportTemplateId.NavyOverlapPhoto)]
	public void Get_ReturnsMatchingDescriptor(CvExportTemplateId templateId)
	{
		var descriptor = CvExportTemplateCatalog.Get(templateId);

		Assert.Equal(templateId, descriptor.Id);
		Assert.Equal(descriptor.AccentColor, CvExportTemplateCatalog.GetAccentColor(templateId));
	}
}

public sealed class CvExportSourceDataFactoryTests
{
	[Fact]
	public void Create_FiltersEmptyEntriesAndTrimsAdditionalInformation()
	{
		var personal = new PersonalInformationImport { FirstName = "Jane", LastName = "Doe" };
		var work = new ReVitae.Core.Cv.WorkExperience.WorkExperienceEntry { JobTitle = "Engineer", Company = "Acme" };
		var emptyWork = new ReVitae.Core.Cv.WorkExperience.WorkExperienceEntry();

		var source = CvExportSourceDataFactory.Create(
			personal,
			[work, emptyWork],
			[],
			[],
			[],
			[],
			[],
			[],
			"  extra notes  ");

		Assert.Single(source.WorkExperience);
		Assert.Equal("extra notes", source.AdditionalInformation);
	}

	[Fact]
	public void Create_ReturnsNullAdditionalInformationForWhitespace()
	{
		var personal = new PersonalInformationImport { FirstName = "Jane", LastName = "Doe" };

		var source = CvExportSourceDataFactory.Create(
			personal,
			[],
			[],
			[],
			[],
			[],
			[],
			[],
			"   ");

		Assert.Null(source.AdditionalInformation);
	}
}
