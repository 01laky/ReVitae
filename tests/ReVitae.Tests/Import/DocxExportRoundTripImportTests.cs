using ReVitae.Core.Cv.Education;
using ReVitae.Core.Import;

namespace ReVitae.Tests.Import;

public sealed class DocxExportRoundTripImportTests
{
	[Fact]
	public void Extract_ParsesReVitaeExportedEducationMetaLine()
	{
		const string text = """
            Jane Doe
            jane@example.com

            Education
            High School
            High School of Electrical and Training Engineering · Nizna, Slovakia · High school · 09 / 2002 - 06 / 2006
            """;

		var result = Extract(text);
		var education = Assert.Single(result.EducationEntries);

		Assert.Equal("High School", education.Degree);
		Assert.Equal("High School of Electrical and Training Engineering", education.Institution);
		Assert.Equal("Nizna, Slovakia", education.Location);
		Assert.Equal(DegreeType.HighSchool, education.DegreeType);
		Assert.Equal(9, education.StartMonth);
		Assert.Equal(2002, education.StartYear);
		Assert.Equal(6, education.EndMonth);
		Assert.Equal(2006, education.EndYear);
	}

	[Fact]
	public void Extract_ParsesMultipleReVitaeExportedWorkExperienceEntries()
	{
		const string text = """
            Jane Doe
            jane@example.com

            Work Experience
            Senior full stack developer
            Excalibur s.r.o. · Kosice, Slovakia · Full-time · 01 / 2024 - 05 / 2026
            Developed backend services in Go and Node.js.

            Senior full stack developer
            Devcity s.r.o. · Prague, Czechia · Full-time · 03 / 2023 - 01 / 2024
            Worked on web application development for Make.com.
            """;

		var result = Extract(text);

		Assert.Equal(2, result.WorkExperienceEntries.Count);

		var excalibur = result.WorkExperienceEntries[0];
		Assert.Equal("Senior full stack developer", excalibur.JobTitle);
		Assert.Equal("Excalibur s.r.o.", excalibur.Company);
		Assert.Equal("Kosice, Slovakia", excalibur.Location);
		Assert.Equal(1, excalibur.StartMonth);
		Assert.Equal(2024, excalibur.StartYear);
		Assert.Equal(5, excalibur.EndMonth);
		Assert.Equal(2026, excalibur.EndYear);
		Assert.Contains("Developed backend services", excalibur.Description, StringComparison.Ordinal);

		var devcity = result.WorkExperienceEntries[1];
		Assert.Equal("Senior full stack developer", devcity.JobTitle);
		Assert.Equal("Devcity s.r.o.", devcity.Company);
		Assert.Equal("Prague, Czechia", devcity.Location);
		Assert.Equal(3, devcity.StartMonth);
		Assert.Equal(2023, devcity.StartYear);
		Assert.Equal(1, devcity.EndMonth);
		Assert.Equal(2024, devcity.EndYear);
	}

	private static CvImportResult Extract(string text)
	{
		return CvImportFieldExtractor.Extract(CvSectionSegmenter.Segment(CvTextNormalizer.Normalize(text)));
	}
}
