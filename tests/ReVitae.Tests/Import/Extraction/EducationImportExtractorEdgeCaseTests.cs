using ReVitae.Core.Cv.Education;
using ReVitae.Core.Import.Extraction;

namespace ReVitae.Tests.Import.Extraction;

public sealed class EducationImportExtractorEdgeCaseTests
{
	[Fact]
	public void Extract_MergesInstitutionSplitAcrossLines()
	{
		const string body = """
            06/2006
            Bratislava
            High School of Electrical

            and Training
            Engineering
            """;

		var entries = EducationImportExtractor.Extract(body, new ImportSectionExtractionContext());
		var entry = Assert.Single(entries);

		Assert.Contains("Electrical and Training Engineering", entry.Institution, StringComparison.Ordinal);
	}

	[Fact]
	public void Extract_ParsesDegreeAndDates()
	{
		const string body = """
            BSc Computer Science
            Technical University
            09/2016 - 06/2020
            """;

		var entry = Assert.Single(EducationImportExtractor.Extract(body, new ImportSectionExtractionContext()));

		Assert.Equal("BSc Computer Science", entry.Degree);
		Assert.Equal("Technical University", entry.Institution);
		Assert.Equal(9, entry.StartMonth);
		Assert.Equal(2016, entry.StartYear);
		Assert.Equal(6, entry.EndMonth);
		Assert.Equal(2020, entry.EndYear);
	}

	[Fact]
	public void Extract_EmptyBody_ReturnsEmpty()
	{
		Assert.Empty(EducationImportExtractor.Extract(string.Empty, new ImportSectionExtractionContext()));
	}

	[Fact]
	public void Extract_TwoEntriesSeparatedByDateBlock()
	{
		const string body = """
            2006
            High School A

            2016 - 2020
            University B
            """;

		var entries = EducationImportExtractor.Extract(body, new ImportSectionExtractionContext());

		Assert.Equal(2, entries.Count);
	}

	[Fact]
	public void Extract_AddsConfidenceForInferredStartDates()
	{
		var context = new ImportSectionExtractionContext();
		const string body = """
            06/2006
            High School of Electrical Engineering
            """;

		var entries = EducationImportExtractor.Extract(body, context);
		var education = Assert.Single(entries);

		Assert.Contains(
			context.FieldConfidences,
			confidence => confidence.FieldKey.Contains(education.Id, StringComparison.Ordinal));
	}

	[Fact]
	public void Extract_SingleYearUsesEndDateOnly()
	{
		const string body = """
            2010
            Secondary School
            City College
            """;

		var entry = Assert.Single(EducationImportExtractor.Extract(body, new ImportSectionExtractionContext()));

		Assert.Equal(2010, entry.EndYear);
	}
}
