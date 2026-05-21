using ReVitae.Core.Cv.Education;
using ReVitae.Core.Import;

namespace ReVitae.Tests.Import;

public sealed class EducationImportEdgeCaseTests
{
    [Fact]
    public void Extract_MergesPdfWrappedInstitutionNameIntoSingleEntry()
    {
        // Reproduces Ladislav_Kostolny_CV.pdf sidebar layout where institution name
        // is broken across blank lines: "High School of Electrical" / "and Training" / "Engineering".
        const string text = """
            Jane Doe
            jane@example.com

            Education
            06/2006
            Nizna, Slovakia
            High School of Electrical

            and Training
            Engineering
            """;

        var result = Extract(text);
        var education = Assert.Single(result.EducationEntries);

        Assert.Equal("High School", education.Degree);
        Assert.Equal("High School of Electrical and Training Engineering", education.Institution);
        Assert.Equal("Nizna, Slovakia", education.Location);
        Assert.Equal(DegreeType.HighSchool, education.DegreeType);
        Assert.Equal(6, education.EndMonth);
        Assert.Equal(2006, education.EndYear);
        Assert.Equal(9, education.StartMonth);
        Assert.Equal(2002, education.StartYear);
    }

    [Fact]
    public void Extract_MergesInstitutionContinuationStartingWithAnd()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Education
            2010
            Bratislava, Slovakia
            Secondary Technical School of Informatics

            and Business
            """;

        var result = Extract(text);
        var education = Assert.Single(result.EducationEntries);

        Assert.Contains("Informatics and Business", education.Institution, StringComparison.Ordinal);
    }

    [Fact]
    public void Extract_KeepsTwoDistinctEducationEntriesSeparatedByBlankLineAndDate()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Education
            06/2006
            Bratislava, Slovakia
            High School of Electrical Engineering

            09/2016 - 06/2020
            Brno, Czechia
            BSc Computer Science
            Technical University
            """;

        var result = Extract(text);

        Assert.Equal(2, result.EducationEntries.Count);

        Assert.Equal("High School", result.EducationEntries[0].Degree);
        Assert.Contains("Electrical Engineering", result.EducationEntries[0].Institution, StringComparison.Ordinal);

        Assert.Equal("BSc Computer Science", result.EducationEntries[1].Degree);
        Assert.Equal("Technical University", result.EducationEntries[1].Institution);
        Assert.Equal(9, result.EducationEntries[1].StartMonth);
        Assert.Equal(2016, result.EducationEntries[1].StartYear);
    }

    [Fact]
    public void Extract_ParsesDegreeFieldOfStudyAndInstitutionWhenThreeHeaderLinesPrecedeDate()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Education
            Master of Science
            Artificial Intelligence
            Stanford University
            09/2018 - 06/2020
            """;

        var result = Extract(text);
        var education = Assert.Single(result.EducationEntries);

        Assert.Equal("Master of Science", education.Degree);
        Assert.Equal("Artificial Intelligence", education.FieldOfStudy);
        Assert.Equal("Stanford University", education.Institution);
    }

    [Fact]
    public void Extract_DoesNotCreateGarbageEntryFromOrphanEngineeringFragment()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Education
            06/2006
            High School of Electrical

            Engineering
            """;

        var result = Extract(text);

        Assert.Single(result.EducationEntries);
        Assert.Contains("Electrical", result.EducationEntries[0].Institution, StringComparison.Ordinal);
        Assert.Contains("Engineering", result.EducationEntries[0].Institution, StringComparison.Ordinal);
    }

    [Fact]
    public void Extract_JoinsMultilineInstitutionWithoutBlankLineSeparators()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Education
            06/2006
            Kosice, Slovakia
            High School of Electrical
            and Training
            Engineering
            """;

        var result = Extract(text);
        var education = Assert.Single(result.EducationEntries);

        Assert.Equal("High School of Electrical and Training Engineering", education.Institution);
    }

    [Fact]
    public void Extract_PreservesEducationDescriptionAfterInstitutionBlock()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Education
            BSc Computer Science
            Technical University
            09/2016 - 06/2020
            Graduated with honors and specialization in distributed systems.
            """;

        var result = Extract(text);
        var education = Assert.Single(result.EducationEntries);

        Assert.Equal("Graduated with honors and specialization in distributed systems.", education.Description);
    }

    [Fact]
    public void Extract_DetectsIncompleteInstitutionEndingWithOf()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Education
            2012
            Prague, Czechia
            Faculty of

            Information Technology
            """;

        var result = Extract(text);
        var education = Assert.Single(result.EducationEntries);

        Assert.Equal("Faculty of Information Technology", education.Institution);
    }

    [Theory]
    [InlineData("University of")]
    [InlineData("High School of Electrical")]
    [InlineData("College of Applied")]
    public void Extract_TreatsLinesEndingMidPhraseAsIncompleteInstitution(string incompleteLine)
    {
        const string continuation = "and Training\nEngineering";
        var text = $"""
            Jane Doe
            jane@example.com

            Education
            2008
            Bratislava, Slovakia
            {incompleteLine}

            {continuation}
            """;

        var result = Extract(text);

        Assert.Single(result.EducationEntries);
        Assert.Contains("Training", result.EducationEntries[0].Institution, StringComparison.Ordinal);
    }

    [Fact]
    public void Extract_DoesNotMergeSecondEntryStartingWithExplicitDegree()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Education
            06/2006
            High School Alpha

            BSc Computer Science
            Technical University
            09/2016 - 06/2020
            """;

        var result = Extract(text);

        Assert.Equal(2, result.EducationEntries.Count);
        Assert.Equal("High School", result.EducationEntries[0].Degree);
        Assert.Equal("BSc Computer Science", result.EducationEntries[1].Degree);
    }

    [Fact]
    public void Extract_DoesNotMergeSecondEntryStartingWithDate()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Education
            06/2006
            High School Alpha

            09/2016 - 06/2020
            Technical University
            BSc Computer Science
            """;

        var result = Extract(text);

        Assert.Equal(2, result.EducationEntries.Count);
    }

    [Fact]
    public void Extract_SingleLineInstitutionStillWorks()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Education
            06/2006
            Bratislava, Slovakia
            High School of Electrical Engineering
            """;

        var result = Extract(text);
        var education = Assert.Single(result.EducationEntries);

        Assert.Equal("High School", education.Degree);
        Assert.Equal("High School of Electrical Engineering", education.Institution);
    }

    [Fact]
    public void Extract_ClassicDegreeInstitutionDateLayoutUnchanged()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Education
            BSc Computer Science
            Technical University
            09/2016 - 06/2020
            Graduated with honors.
            """;

        var result = Extract(text);
        var education = Assert.Single(result.EducationEntries);

        Assert.Equal("BSc Computer Science", education.Degree);
        Assert.Equal("Technical University", education.Institution);
        Assert.Equal(9, education.StartMonth);
        Assert.Equal(2016, education.StartYear);
        Assert.Equal(6, education.EndMonth);
        Assert.Equal(2020, education.EndYear);
    }

    [Fact]
    public void Extract_SectionHasDataTrueForSingleMergedEducationEntry()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Education
            06/2006
            Nizna, Slovakia
            High School of Electrical

            and Training
            Engineering
            """;

        var result = Extract(text);

        Assert.True(result.SectionHasData[CvImportSectionId.Education]);
    }

    private static CvImportResult Extract(string text)
    {
        return CvImportFieldExtractor.Extract(CvSectionSegmenter.Segment(CvTextNormalizer.Normalize(text)));
    }
}
