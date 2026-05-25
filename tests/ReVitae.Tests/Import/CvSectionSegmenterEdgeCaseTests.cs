using ReVitae.Core.Import;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Import;

public sealed class CvSectionSegmenterEdgeCaseTests
{
	[Fact]
	public void Segment_NoHeaders_PutsBodyInAdditionalInformation()
	{
		const string text = """
            Jane Doe
            jane@example.com
            Random paragraph without section headers.
            """;

		var result = CvSectionSegmenter.Segment(text);

		Assert.Contains(TranslationKeys.ImportWarningNoSectionsDetected, result.Warnings.Select(w => w.MessageKey));
		Assert.True(result.SectionBodies.ContainsKey(CvImportSectionId.AdditionalInformation));
		Assert.Contains("Random paragraph", result.SectionBodies[CvImportSectionId.AdditionalInformation], StringComparison.Ordinal);
	}

	[Fact]
	public void Segment_EmptyInput_ReturnsWarningAndEmptyHeader()
	{
		var result = CvSectionSegmenter.Segment(string.Empty);

		Assert.NotEmpty(result.Warnings);
		Assert.Equal(string.Empty, result.HeaderBlock);
	}

	[Fact]
	public void Segment_DetectsWorkExperienceSection()
	{
		const string text = """
            Jane Doe
            Work Experience
            2020
            Engineer at Acme
            """;

		var result = CvSectionSegmenter.Segment(text);

		Assert.True(result.SectionBodies.ContainsKey(CvImportSectionId.WorkExperience));
		Assert.Contains("Engineer", result.SectionBodies[CvImportSectionId.WorkExperience], StringComparison.Ordinal);
	}

	[Fact]
	public void Segment_EmailLineNotTreatedAsHeader()
	{
		const string text = """
            Jane Doe
            jane@example.com
            Skills
            C#
            """;

		var result = CvSectionSegmenter.Segment(text);

		Assert.True(result.SectionBodies.ContainsKey(CvImportSectionId.Skills));
		Assert.Contains("jane@example.com", result.HeaderBlock, StringComparison.Ordinal);
	}

	[Fact]
	public void Segment_MergesDuplicateSectionHeaders()
	{
		const string text = """
            Jane Doe
            Skills
            C#
            Skills
            Docker
            """;

		var result = CvSectionSegmenter.Segment(text);

		Assert.True(result.SectionBodies.TryGetValue(CvImportSectionId.Skills, out var body));
		Assert.Contains("C#", body, StringComparison.Ordinal);
		Assert.Contains("Docker", body, StringComparison.Ordinal);
	}

	[Fact]
	public void Segment_SlovakHeaderKeywords()
	{
		const string text = """
            Jana Nováková
            Pracovné skúsenosti
            2021
            Developer
            """;

		var result = CvSectionSegmenter.Segment(text);

		Assert.True(result.SectionBodies.ContainsKey(CvImportSectionId.WorkExperience));
	}

	[Fact]
	public void Segment_ExportSubheadingNotTreatedAsSection()
	{
		const string text = """
            Jane Doe
            Work Experience
            2020
            Engineer
            Technologies
            C#
            """;

		var result = CvSectionSegmenter.Segment(text);

		Assert.False(result.SectionBodies.ContainsKey(CvImportSectionId.Skills));
		Assert.Contains("C#", result.SectionBodies[CvImportSectionId.WorkExperience], StringComparison.Ordinal);
	}

	[Fact]
	public void Segment_HeaderBlockBeforeFirstSection()
	{
		const string text = """
            Jane Doe
            Software Architect
            jane@example.com

            Education
            2020
            MSc
            """;

		var result = CvSectionSegmenter.Segment(text);

		Assert.Contains("Jane Doe", result.HeaderBlock, StringComparison.Ordinal);
		Assert.Contains("Architect", result.HeaderBlock, StringComparison.Ordinal);
	}
}
