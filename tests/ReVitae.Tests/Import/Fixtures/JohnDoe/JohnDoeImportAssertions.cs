using ReVitae.Core.Import;

namespace ReVitae.Tests.Import.Fixtures.JohnDoe;

public static class JohnDoeImportAssertions
{
    public static void AssertMatchesExpectations(
        CvImportResult result,
        JohnDoeVariantSpec spec)
    {
        Assert.True(result.Success, $"[{spec.Id}] import failed: {result.ErrorMessageKey}");

        if (spec.ExpectationMode == JohnDoeExpectationMode.PdfTemplateLayout)
        {
            AssertPdfTemplateLayout(result, spec);
            return;
        }

        var counts = JohnDoeCanonicalExpectations.Counts;
        Assert.Equal(counts.WorkExperience, result.WorkExperienceEntries.Count);
        Assert.Equal(spec.MinimumEducationEntries ?? counts.Education, result.EducationEntries.Count);
        Assert.Equal(counts.LanguageEntries, result.LanguageEntries.Count);
        Assert.Equal(counts.CertificateEntries, result.CertificateEntries.Count);
        Assert.Equal(counts.ProjectEntries, result.ProjectEntries.Count);
        Assert.Equal(counts.LinkEntries, result.LinkEntries.Count);
        Assert.True(result.SkillsGroups.Sum(group => group.Skills.Count) >= counts.MinimumSkillItems);

        switch (spec.ExpectationMode)
        {
            case JohnDoeExpectationMode.Full:
            case JohnDoeExpectationMode.PdfFull:
                AssertExactPersonalIdentity(result);
                AssertPersonalUrls(result);
                Assert.InRange(result.SkillsGroups.Count, counts.MinimumSkillGroups, counts.MaximumSkillGroups);
                break;
            case JohnDoeExpectationMode.StandardEntryCounts:
                AssertOptionalPersonalHints(result);
                Assert.True(result.SkillsGroups.Count >= 1);
                break;
        }

        AssertSpotChecks(result);

        if (JohnDoeExpectationModes.RequiresZeroValidationErrors(spec.ExpectationMode))
        {
            var validation = JohnDoePostImportValidator.Validate(result);
            Assert.True(
                validation.IsValid,
                $"[{spec.Id}] expected zero post-import validation errors.{Environment.NewLine}{JohnDoePostImportValidator.FormatErrors(validation, max: 20)}");
        }
    }

    private static void AssertPdfTemplateLayout(CvImportResult result, JohnDoeVariantSpec spec)
    {
        var totalItems = result.WorkExperienceEntries.Count
            + result.EducationEntries.Count
            + result.CertificateEntries.Count
            + result.ProjectEntries.Count
            + result.LanguageEntries.Count
            + result.SkillsGroups.Sum(group => group.Skills.Count);

        Assert.True(
            totalItems >= 20,
            $"[{spec.Id}] expected meaningful import content from alternate PDF layout, got {totalItems} items.");

        if (!string.IsNullOrWhiteSpace(result.Personal.Email))
        {
            Assert.Contains("john.doe", result.Personal.Email, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static void AssertExactPersonalIdentity(CvImportResult result)
    {
        Assert.Equal(JohnDoeCanonicalExpectations.FirstName, result.Personal.FirstName);
        Assert.Equal(JohnDoeCanonicalExpectations.LastName, result.Personal.LastName);
        Assert.Equal(JohnDoeCanonicalExpectations.Email, result.Personal.Email);
    }

    private static void AssertOptionalPersonalHints(CvImportResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.Personal.LinkedInUrl))
        {
            Assert.Contains("linkedin.com", result.Personal.LinkedInUrl, StringComparison.OrdinalIgnoreCase);
        }

        if (!string.IsNullOrWhiteSpace(result.Personal.Location))
        {
            Assert.Contains(JohnDoeCanonicalExpectations.LocationFragment, result.Personal.Location, StringComparison.Ordinal);
        }
    }

    private static void AssertPersonalUrls(CvImportResult result)
    {
        Assert.Equal(JohnDoeCanonicalExpectations.LinkedInUrl, result.Personal.LinkedInUrl);
        Assert.Equal(JohnDoeCanonicalExpectations.GitHubUrl, result.Personal.GitHubUrl);
        Assert.Equal(JohnDoeCanonicalExpectations.PortfolioUrl, result.Personal.PortfolioUrl);
        Assert.Contains(JohnDoeCanonicalExpectations.LocationFragment, result.Personal.Location, StringComparison.Ordinal);
        Assert.Contains(JohnDoeCanonicalExpectations.TitleFragment, result.Personal.ProfessionalTitle, StringComparison.Ordinal);
    }

    private static void AssertSpotChecks(CvImportResult result)
    {
        Assert.Equal(JohnDoeCanonicalExpectations.FirstWorkCompany, result.WorkExperienceEntries[0].Company);
        Assert.Equal(JohnDoeCanonicalExpectations.LastWorkCompany, result.WorkExperienceEntries[^1].Company);
        Assert.Contains(
            JohnDoeCanonicalExpectations.FirstEducationInstitution,
            result.EducationEntries[0].Institution,
            StringComparison.Ordinal);
        Assert.Contains(
            JohnDoeCanonicalExpectations.LastEducationInstitution,
            result.EducationEntries[^1].Institution,
            StringComparison.Ordinal);

        var frontend = result.SkillsGroups.FirstOrDefault(group =>
            group.Category.Contains(JohnDoeCanonicalExpectations.SampleSkillGroup, StringComparison.OrdinalIgnoreCase));
        if (frontend is not null)
        {
            Assert.Contains(
                frontend.Skills,
                skill => skill.Name.Equals(JohnDoeCanonicalExpectations.SampleSkillName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
