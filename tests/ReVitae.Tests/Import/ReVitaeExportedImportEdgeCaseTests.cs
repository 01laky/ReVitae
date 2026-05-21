using ReVitae.Core.Import;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Import;

public sealed class ReVitaeExportedImportEdgeCaseTests
{
    [Fact]
    public void Extract_PrefersNameFromSkillsWhenHeaderIsSummaryProse()
    {
        const string text = """
            Full Stack Developer with 12+ years of experience building web and backend systems.
            Strong in React, TypeScript, Node.js, NestJS, Go, .NET, PostgreSQL, and Redis.
            01laky@gmail.com

            Skills
            Ladislav Kostolný
            General
            Go · Intermediate
            NestJS · Intermediate
            """;

        var result = Extract(text);

        Assert.Equal("Ladislav", result.Personal.FirstName);
        Assert.Equal("Kostolný", result.Personal.LastName);
        Assert.Equal("01laky@gmail.com", result.Personal.Email);
    }

    [Fact]
    public void Extract_ParsesSplitPersonNameFromSkillsSection()
    {
        const string text = """
            Senior developer with 12+ years of experience across fintech and SaaS products.
            jane@example.com

            Skills
            Ladislav
            Kostolný
            General
            Docker · Intermediate
            """;

        var result = Extract(text);

        Assert.Equal("Ladislav", result.Personal.FirstName);
        Assert.Equal("Kostolný", result.Personal.LastName);
    }

    [Fact]
    public void Extract_DoesNotTreatTechnologyTokensAsPersonNameFromSummaryHeader()
    {
        const string text = """
            Full Stack Developer with 12+ years of experience building scalable systems.
            Strong in React, TypeScript, Node.js, NestJS, Go, .NET, PostgreSQL, and Redis.
            TypeScript,
            Redis,
            jane@example.com

            Skills
            General
            Docker · Intermediate
            """;

        var result = Extract(text);

        Assert.DoesNotContain("TypeScript", result.Personal.FirstName, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Redis", result.Personal.FirstName, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TypeScript", result.Personal.LastName, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Redis", result.Personal.LastName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Extract_DoesNotUseReactCommaTypeScriptLineAsPersonName()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Skills
            React, TypeScript
            General
            Docker · Intermediate
            """;

        var result = Extract(text);

        Assert.Equal("Jane", result.Personal.FirstName);
        Assert.Equal("Doe", result.Personal.LastName);
    }

    [Fact]
    public void Extract_StillParsesClassicNameFromHeaderFirstLine()
    {
        const string text = """
            Peter Novák
            peter@example.com

            Skills
            General
            C# · Advanced
            """;

        var result = Extract(text);

        Assert.Equal("Peter", result.Personal.FirstName);
        Assert.Equal("Novák", result.Personal.LastName);
    }

    [Fact]
    public void Extract_ParsesReVitaeSkillPreviewLinesWithIntermediateProficiency()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Skills
            General
            Node.js · Intermediate
            NestJS · Intermediate
            OAuth · Intermediate
            SAML 2.0 · Intermediate
            """;

        var result = Extract(text);

        var group = Assert.Single(result.SkillsGroups);
        Assert.Equal("General", group.Category);
        Assert.Equal(
            ["Node.js", "NestJS", "OAuth", "SAML 2.0"],
            group.Skills.Select(skill => skill.Name).ToArray());
    }

    [Fact]
    public void Extract_ParsesReVitaeSkillPreviewLineWithYears()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Skills
            Backend
            PostgreSQL · Advanced · 8 years
            Redis · Intermediate · 3 years
            """;

        var result = Extract(text);

        var skills = result.SkillsGroups.SelectMany(group => group.Skills).Select(skill => skill.Name).ToArray();
        Assert.Equal(["PostgreSQL", "Redis"], skills);
    }

    [Fact]
    public void Extract_TrimsGarbagePrefixBeforeGeneralSkillBlock()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Skills
            Senior
            mobile,
            Developed
            Contributed
            OAuth
            General
            Go · Intermediate
            PHP · Intermediate
            Microservices · Intermediate
            """;

        var result = Extract(text);

        var group = Assert.Single(result.SkillsGroups);
        Assert.Equal(
            ["Go", "PHP", "Microservices"],
            group.Skills.Select(skill => skill.Name).ToArray());
        Assert.DoesNotContain(result.SkillsGroups.SelectMany(group => group.Skills), skill =>
            skill.Name.Contains("Developed", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.SkillsGroups.SelectMany(group => group.Skills), skill =>
            skill.Name.Contains("Contributed", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Extract_IgnoresStandaloneTokensWhenReVitaeDotFormatPresent()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Skills
            Senior
            Excalibur
            Copilot
            General
            Cursor · Intermediate
            """;

        var result = Extract(text);

        var skills = result.SkillsGroups.SelectMany(group => group.Skills).Select(skill => skill.Name).ToArray();
        Assert.Equal(["Cursor"], skills);
    }

    [Fact]
    public void Extract_StillParsesCommaSeparatedSkillsWithoutReVitaeDotFormat()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Skills
            PostgreSQL, Redis, React
            """;

        var result = Extract(text);

        var skills = result.SkillsGroups.SelectMany(group => group.Skills).Select(skill => skill.Name).ToArray();
        Assert.Equal(["PostgreSQL", "Redis", "React"], skills);
    }

    [Fact]
    public void Extract_StillParsesColonCategorySkillsWithoutReVitaeDotFormat()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Skills
            Backend: PostgreSQL, Redis
            Frontend: React, TypeScript
            """;

        var result = Extract(text);

        Assert.Equal(2, result.SkillsGroups.Count);
        Assert.Equal(["PostgreSQL", "Redis"], result.SkillsGroups[0].Skills.Select(skill => skill.Name).ToArray());
        Assert.Equal(["React", "TypeScript"], result.SkillsGroups[1].Skills.Select(skill => skill.Name).ToArray());
    }

    [Fact]
    public void Extract_DoesNotParseWorkBleedLineAsSkill()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Skills
            General
            Excalibur s.r.o. · Kosice, Slovakia · Full-time · 01 / 2024 - 05 / 2026
            Go · Intermediate
            """;

        var result = Extract(text);

        var skills = result.SkillsGroups.SelectMany(group => group.Skills).Select(skill => skill.Name).ToArray();
        Assert.Equal(["Go"], skills);
    }

    [Fact]
    public void Extract_IgnoresExportSubheadingLinesInSkills()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Skills
            Technologies
            Achievements
            General
            Docker · Intermediate
            """;

        var result = Extract(text);

        var skills = result.SkillsGroups.SelectMany(group => group.Skills).Select(skill => skill.Name).ToArray();
        Assert.Equal(["Docker"], skills);
        Assert.DoesNotContain(result.SkillsGroups, group =>
            group.Category.Equals("Technologies", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.SkillsGroups, group =>
            group.Category.Equals("Achievements", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Extract_DoesNotTreatSingleCommaSkillLineAsSkillList()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Skills
            TypeScript,
            General
            Go · Intermediate
            """;

        var result = Extract(text);

        var skills = result.SkillsGroups.SelectMany(group => group.Skills).Select(skill => skill.Name).ToArray();
        Assert.Equal(["Go"], skills);
        Assert.DoesNotContain(skills, skill => skill.Contains("TypeScript", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Extract_PutsBulletSkillsInSeparateGeneralGroupFromColonCategory()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Skills
            Programming: C#, Go
            - Git
            - Docker
            """;

        var result = Extract(text);

        Assert.Equal(2, result.SkillsGroups.Count);
        Assert.Equal("Programming", result.SkillsGroups[0].Category);
        Assert.Equal(["C#", "Go"], result.SkillsGroups[0].Skills.Select(skill => skill.Name).ToArray());

        var general = Assert.Single(result.SkillsGroups, group => group.Category == "General");
        Assert.Equal(["Git", "Docker"], general.Skills.Select(skill => skill.Name).ToArray());
    }

    [Fact]
    public void Extract_ParsesSidebarFirstTwoColumnExportTextLayout()
    {
        const string text = """
            Full Stack Developer with 12+ years of experience building web systems.
            Strong in React, TypeScript, Node.js, NestJS, Go, .NET, PostgreSQL, and Redis.

            Work Experience
            Excalibur s.r.o. - Senior full stack developer
            Kosice, Slovakia
            01/2024 - 05/2026
            Developed backend services in Go and Node.js.

            Devcity s.r.o. - Senior full stack developer
            Prague, Czechia
            03/2023 - 01/2024
            Worked on web application development.

            Contact
            Email: 01laky@gmail.com
            Phone: (+421) 944159982
            Location: Turček, Slovakia 03848

            Skills
            Ladislav Kostolný
            General
            Node.js · Intermediate
            NestJS · Intermediate
            Go · Intermediate
            """;

        var result = Extract(text);

        Assert.Equal("Ladislav", result.Personal.FirstName);
        Assert.Equal("Kostolný", result.Personal.LastName);
        Assert.Equal("01laky@gmail.com", result.Personal.Email);
        Assert.Equal(2, result.WorkExperienceEntries.Count);
        Assert.True(result.SkillsGroups.Sum(group => group.Skills.Count) <= 10);
        Assert.Contains(result.SkillsGroups.SelectMany(group => group.Skills), skill =>
            skill.Name.Equals("Node.js", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Import_ReVitaeExportedSidebarPdf_DoesNotDumpWorkDescriptionsIntoSkills()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Import", "Fixtures", "ReVitaeExportedSidebarCv.pdf");
        if (!File.Exists(path))
        {
            return;
        }

        var result = CvDocumentImporter.Import(path);

        Assert.True(result.Success, result.ErrorMessageKey);
        foreach (var skill in result.SkillsGroups.SelectMany(group => group.Skills))
        {
            Assert.DoesNotContain("architecture decisions", skill.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("reviewed AI outputs", skill.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("software architecture", skill.Name, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Extract_PrefersBestPersonNameOverFragmentedPdfHeaderNoise()
    {
        const string text = """
            Email: 01laky@gmail.com
            Phone: (+421) 944159982
            Location: Turček, Slovakia 03848
            / 2024 - 05 / 2026
            and AI-assisted
            and
            flows using
            Ladislav Kostolný
            and backend systems across
            commerce, cybersecurity,

            Summary
            -

            Work Experience
            full stack developer
            s.r.o. · Kosice, Slovakia · Full-time · 01
            Developed backend services.

            Skills
            General
            Go · Intermediate
            """;

        var result = Extract(text);

        Assert.Equal("Ladislav", result.Personal.FirstName);
        Assert.Equal("Kostolný", result.Personal.LastName);
        Assert.DoesNotContain("AI-assisted", result.Personal.FirstName, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("integrating", result.Personal.FirstName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Extract_DoesNotUseContactSectionJobTextAsPersonName()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Contact
            invoice system
            MS SQL, ReactJS, .NET Core
            TypeScript, Redis
            Email: jane@example.com
            Phone: (+421) 900 000 000

            Skills
            General
            Docker · Intermediate
            """;

        var result = Extract(text);

        Assert.Equal("Jane", result.Personal.FirstName);
        Assert.Equal("Doe", result.Personal.LastName);
        Assert.DoesNotContain("invoice", result.Personal.FirstName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Extract_MergesSplitSkillProficiencyLinesFromPdfFragmentation()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Skills
            General
            Node.js · Intermediate
            NestJS · Intermediate
            Microservices ·
            Cybersecurity ·
            Management ·
            Intermediate
            Intermediate
            Intermediate
            """;

        var result = Extract(text);

        var skills = result.SkillsGroups.SelectMany(group => group.Skills).Select(skill => skill.Name).ToArray();
        Assert.Equal(["Node.js", "NestJS", "Microservices", "Cybersecurity", "Management"], skills);
    }

    [Fact]
    public void Extract_AssignsOrphanProficiencyToBareSkillNameLines()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Skills
            General
            Secure API Design
            Intermediate
            """;

        var result = Extract(text);

        var skills = result.SkillsGroups.SelectMany(group => group.Skills).Select(skill => skill.Name).ToArray();
        Assert.Equal(["Secure API Design"], skills);
    }

    [Fact]
    public void Extract_ParsesMultipleWorkEntriesWithOrphanHeaderDateFragments()
    {
        const string text = """
            Email: jane@example.com
            / 2024 - 05 / 2026
            / 2023 - 01 / 2024

            Jane Doe
            jane@example.com

            Work Experience
            full stack developer
            Excalibur s.r.o. · Kosice, Slovakia · Full-time · 01
            Developed backend services in Go.

            full stack developer
            Devcity s.r.o. · Prague, Czechia · Full-time · 03
            Worked on web application development.
            """;

        var result = Extract(text);

        Assert.Equal(2, result.WorkExperienceEntries.Count);

        var excalibur = result.WorkExperienceEntries[0];
        Assert.Equal("full stack developer", excalibur.JobTitle);
        Assert.Equal("Excalibur s.r.o.", excalibur.Company);
        Assert.Equal("Kosice, Slovakia", excalibur.Location);
        Assert.Equal(1, excalibur.StartMonth);
        Assert.Equal(2024, excalibur.StartYear);
        Assert.Equal(5, excalibur.EndMonth);
        Assert.Equal(2026, excalibur.EndYear);

        var devcity = result.WorkExperienceEntries[1];
        Assert.Equal("Devcity s.r.o.", devcity.Company);
        Assert.Equal(3, devcity.StartMonth);
        Assert.Equal(2023, devcity.StartYear);
        Assert.Equal(1, devcity.EndMonth);
        Assert.Equal(2024, devcity.EndYear);
    }

    [Fact]
    public void Extract_ParsesTruncatedWorkMetaWithoutOrphanDatesAsSeparateEntries()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Work Experience
            Senior full stack developer
            Excalibur s.r.o. · Kosice, Slovakia · Full-time · 01 / 2024 - 05 / 2026
            Built backend services.

            Senior full stack developer
            Devcity s.r.o. · Prague, Czechia · Full-time · 03 / 2023 - 01 / 2024
            Delivered frontend modules.
            """;

        var result = Extract(text);

        Assert.Equal(2, result.WorkExperienceEntries.Count);
        Assert.Equal("Excalibur s.r.o.", result.WorkExperienceEntries[0].Company);
        Assert.Equal("Devcity s.r.o.", result.WorkExperienceEntries[1].Company);
        Assert.Equal(2024, result.WorkExperienceEntries[0].StartYear);
        Assert.Equal(2023, result.WorkExperienceEntries[1].StartYear);
    }

    [Fact]
    public void Extract_MergesDotPrefixedProficiencyFragments()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Skills
            General
            OAuth · Intermediate
            RBAC · Intermediate
            · Intermediate
            """;

        var result = Extract(text);

        var skills = result.SkillsGroups.SelectMany(group => group.Skills).Select(skill => skill.Name).ToArray();
        Assert.Equal(["OAuth", "RBAC"], skills);
    }

    private static CvImportResult Extract(string text)
    {
        return CvImportFieldExtractor.Extract(CvSectionSegmenter.Segment(CvTextNormalizer.Normalize(text)));
    }
}

public sealed class ReVitaeExportSubheadingSegmenterEdgeCaseTests
{
    [Fact]
    public void Segment_DoesNotTreatExportSubheadingsAsSectionHeaders()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Work Experience
            Senior Developer at Acme
            2020 - 2024
            Built platform APIs.
            Technologies
            Achievements
            Shipped release on time.
            Company URL
            Institution URL

            Skills
            Go · Intermediate
            """;

        var result = CvSectionSegmenter.Segment(CvTextNormalizer.Normalize(text));

        Assert.True(result.SectionBodies.ContainsKey(CvImportSectionId.WorkExperience));
        var workBody = result.SectionBodies[CvImportSectionId.WorkExperience];
        Assert.Contains("Built platform APIs.", workBody, StringComparison.Ordinal);
        Assert.Contains("Technologies", workBody, StringComparison.Ordinal);
        Assert.Contains("Achievements", workBody, StringComparison.Ordinal);
        Assert.Contains("Shipped release on time.", workBody, StringComparison.Ordinal);
        Assert.Contains("Company URL", workBody, StringComparison.Ordinal);
        Assert.Contains("Institution URL", workBody, StringComparison.Ordinal);
        Assert.True(result.SectionBodies.ContainsKey(CvImportSectionId.Skills));
    }

    [Fact]
    public void Segment_DoesNotTreatStandaloneTechnologiesLabelAsSkillsSection()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Work Experience
            Developer at Acme
            Technologies
            C#, SQL
            """;

        var result = CvSectionSegmenter.Segment(CvTextNormalizer.Normalize(text));

        Assert.False(result.SectionBodies.ContainsKey(CvImportSectionId.Skills));
        var workBody = result.SectionBodies[CvImportSectionId.WorkExperience];
        Assert.Contains("Technologies", workBody, StringComparison.Ordinal);
        Assert.Contains("C#, SQL", workBody, StringComparison.Ordinal);
    }

    [Fact]
    public void Segment_StillDetectsTechnicalSkillsHeader()
    {
        const string text = """
            Jane Doe
            jane@example.com

            Technical Skills
            C#, Go
            """;

        var result = CvSectionSegmenter.Segment(CvTextNormalizer.Normalize(text));

        Assert.True(result.SectionBodies.ContainsKey(CvImportSectionId.Skills));
        Assert.Contains("C#, Go", result.SectionBodies[CvImportSectionId.Skills], StringComparison.Ordinal);
    }
}
