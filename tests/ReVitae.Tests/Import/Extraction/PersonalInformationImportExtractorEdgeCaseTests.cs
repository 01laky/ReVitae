using ReVitae.Core.Cv;
using ReVitae.Core.Import;
using ReVitae.Core.Import.Extraction;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Import.Extraction;

[Trait("Category", "ImportExtraction")]
public sealed class PersonalInformationImportExtractorEdgeCaseTests
{
	[Fact]
	public void Extract_SplitEmailAndPhoneOnSeparateLines()
	{
		var segmentation = Segment("""
            Jane Doe
            Software Engineer
            jane@example.com
            +421 900 123 456
            """);
		var context = new ImportSectionExtractionContext();

		var personal = PersonalInformationImportExtractor.Extract(segmentation, context);

		Assert.Equal("jane@example.com", personal.Email);
		Assert.Equal("+421 900 123 456", personal.Phone);
		Assert.Contains(context.FieldConfidences, c => c.FieldKey == MainPersonalInformationFieldKeys.Email);
	}

	[Fact]
	public void Extract_UrlOnNextLine_AssignsPortfolioUrl()
	{
		var segmentation = Segment("""
            Jane Doe
            https://jane.dev
            """);
		var context = new ImportSectionExtractionContext();

		var personal = PersonalInformationImportExtractor.Extract(segmentation, context);

		Assert.Equal("https://jane.dev", personal.PortfolioUrl);
	}

	[Fact]
	public void Extract_LinkedInHyphenatedAcrossLines_StillDetectedViaHyperlinks()
	{
		var segmentation = Segment("Jane Doe\nlinkedin.com/in/jane-doe");
		var context = new ImportSectionExtractionContext();
		var hyperlinks = new[] { "https://www.linkedin.com/in/jane-doe" };

		var personal = PersonalInformationImportExtractor.Extract(segmentation, context, hyperlinks);

		Assert.Equal("https://www.linkedin.com/in/jane-doe", personal.LinkedInUrl);
	}

	[Fact]
	public void Extract_GitHubUrlFromHyperlinks()
	{
		var segmentation = Segment("Jane Doe");
		var context = new ImportSectionExtractionContext();
		var hyperlinks = new[] { "https://github.com/janedoe" };

		var personal = PersonalInformationImportExtractor.Extract(segmentation, context, hyperlinks);

		Assert.Equal("https://github.com/janedoe", personal.GitHubUrl);
	}

	[Fact]
	public void Extract_NameFromHeaderBlock()
	{
		var segmentation = Segment("""
            Jane Marie Doe
            jane@example.com
            """);
		var context = new ImportSectionExtractionContext();

		var personal = PersonalInformationImportExtractor.Extract(segmentation, context);

		Assert.Contains("Jane", personal.FirstName, StringComparison.Ordinal);
		Assert.Contains("Doe", personal.LastName, StringComparison.Ordinal);
	}

	[Fact]
	public void Extract_EmptySegmentation_DoesNotThrow()
	{
		var segmentation = new CvSegmentationResult();
		var context = new ImportSectionExtractionContext();

		var personal = PersonalInformationImportExtractor.Extract(segmentation, context);

		Assert.False(personal.HasAnyData());
	}

	[Fact]
	public void Extract_ContactSectionSupplementsHeader()
	{
		var segmentation = new CvSegmentationResult
		{
			HeaderBlock = "Jane Doe",
			SectionBodies = new Dictionary<CvImportSectionId, string>
			{
				[CvImportSectionId.Contact] = "jane@example.com\nBratislava, Slovakia",
			},
		};
		var context = new ImportSectionExtractionContext();

		var personal = PersonalInformationImportExtractor.Extract(segmentation, context);

		Assert.Equal("jane@example.com", personal.Email);
		Assert.Contains("Bratislava", personal.Location, StringComparison.Ordinal);
	}

	[Fact]
	public void Extract_DuplicateUrlsPreferLinkedInThenGitHubThenPortfolio()
	{
		var segmentation = Segment("Jane Doe");
		var context = new ImportSectionExtractionContext();
		var hyperlinks = new[]
		{
			"https://github.com/jane",
			"https://linkedin.com/in/jane",
			"https://jane.dev",
		};

		var personal = PersonalInformationImportExtractor.Extract(segmentation, context, hyperlinks);

		Assert.Equal("https://linkedin.com/in/jane", personal.LinkedInUrl);
		Assert.Equal("https://github.com/jane", personal.GitHubUrl);
		Assert.Equal("https://jane.dev", personal.PortfolioUrl);
	}

	private static CvSegmentationResult Segment(string text) => CvSectionSegmenter.Segment(text);
}
