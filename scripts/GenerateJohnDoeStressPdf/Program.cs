using ReVitae.Core.Export;
using ReVitae.Core.Export.Pdf;
using ReVitae.Core.Localization;

var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var outputPath = Path.Combine(repoRoot, "John Doe.pdf");

var localizer = new AppLocalizer("en");
var labels = new CvExportSectionLabels(
    Summary: localizer.Get(TranslationKeys.Summary),
    Contact: localizer.Get(TranslationKeys.Contact),
    Profile: localizer.Get(TranslationKeys.Profile),
    Objective: localizer.Get(TranslationKeys.Objective),
    PreviewWorkExperience: localizer.Get(TranslationKeys.PreviewWorkExperience),
    PreviewAchievements: localizer.Get(TranslationKeys.PreviewAchievements),
    PreviewTechnologies: localizer.Get(TranslationKeys.PreviewTechnologies),
    WorkExperienceCompanyUrl: localizer.Get(TranslationKeys.WorkExperienceCompanyUrl),
    PreviewEducation: localizer.Get(TranslationKeys.PreviewEducation),
    PreviewFieldOfStudy: localizer.Get(TranslationKeys.PreviewFieldOfStudy),
    PreviewGrade: localizer.Get(TranslationKeys.PreviewGrade),
    EducationInstitutionUrl: localizer.Get(TranslationKeys.EducationInstitutionUrl),
    PreviewSkills: localizer.Get(TranslationKeys.PreviewSkills),
    PreviewYearsSuffix: localizer.Get(TranslationKeys.PreviewYearsSuffix),
    PreviewLanguages: localizer.Get(TranslationKeys.PreviewLanguages),
    PreviewCertificates: localizer.Get(TranslationKeys.PreviewCertificates),
    PreviewProjects: localizer.Get(TranslationKeys.PreviewProjects),
    PreviewCustomLinks: localizer.Get(TranslationKeys.PreviewCustomLinks),
    PreviewAdditionalInformation: localizer.Get(TranslationKeys.PreviewAdditionalInformation),
    ContactLinks: localizer.Get(TranslationKeys.ContactLinks),
    Digital: localizer.Get(TranslationKeys.Digital),
    Links: localizer.Get(TranslationKeys.Links),
    Online: localizer.Get(TranslationKeys.Online),
    Email: localizer.Get(TranslationKeys.Email),
    Phone: localizer.Get(TranslationKeys.Phone),
    Location: localizer.Get(TranslationKeys.Location),
    LinkedInUrl: localizer.Get(TranslationKeys.LinkedInUrl),
    PortfolioUrl: localizer.Get(TranslationKeys.PortfolioUrl),
    GitHubUrl: localizer.Get(TranslationKeys.GitHubUrl));

var document = new CvExportDocument(
    CvExportTemplateId.ModernSidebar,
    labels,
    FirstName: "John",
    LastName: "Doe",
    ProfessionalTitle: "Senior Full Stack Software Architect & Engineering Leader",
    Email: "john.doe@example.com",
    Phone: "+1 (555) 010-2030",
    Location: "San Francisco, CA 94107, United States",
    LinkedInUrl: "https://www.linkedin.com/in/john-doe-architect",
    PortfolioUrl: "https://johndoe.dev",
    GitHubUrl: "https://github.com/johndoe",
    ShortSummary: BuildSummary(),
    PhotoPath: null,
    WorkExperienceEntries: BuildWorkExperience(),
    EducationEntries: BuildEducation(),
    SkillsGroups: BuildSkills(),
    LanguageEntries: BuildLanguages(),
    CertificateEntries: BuildCertificates(),
    ProjectEntries: BuildProjects(),
    CustomLinkLines: BuildLinks(),
    AdditionalInformationContent: BuildAdditionalInformation());

var pdfBytes = new QuestPdfCvExporter().Export(document);
await File.WriteAllBytesAsync(outputPath, pdfBytes);

Console.WriteLine($"Generated: {outputPath}");
Console.WriteLine($"Size: {pdfBytes.Length:N0} bytes");
Console.WriteLine($"Work: {document.WorkExperienceEntries.Count}");
Console.WriteLine($"Education: {document.EducationEntries.Count}");
Console.WriteLine($"Skill groups: {document.SkillsGroups.Count} ({document.SkillsGroups.Sum(g => g.Skills.Count)} skills)");
Console.WriteLine($"Languages: {document.LanguageEntries.Count}");
Console.WriteLine($"Certificates: {document.CertificateEntries.Count}");
Console.WriteLine($"Projects: {document.ProjectEntries.Count}");
Console.WriteLine($"Links: {document.CustomLinkLines.Count}");

static string BuildSummary()
{
    return string.Join(
        ' ',
        Enumerable.Repeat(
            "Senior full stack engineer with 18+ years of experience designing distributed systems, leading cross-functional teams, " +
            "and delivering secure, observable platforms for fintech, healthcare, e-commerce, and developer tooling. " +
            "Expert in cloud-native architecture, API design, data pipelines, and pragmatic engineering leadership.",
            6));
}

static IReadOnlyList<WorkExperienceEntry> BuildWorkExperience()
{
    var companies = new[]
    {
        "Nimbus Cloud Systems", "Atlas Fintech Group", "Vertex Health Platform", "Orbit Commerce Labs",
        "Quantum Security Inc.", "Helix Data Services", "Polar SaaS Studio", "Summit AI Research",
        "Blue Harbor Consulting", "Catalyst DevTools", "IronGate Cyber", "Lumen Analytics",
        "Nova Mobility", "Pioneer Payments", "Redwood Enterprise", "Skyline Observability",
        "Terra Infrastructure", "Uplink Communications", "Vantage Robotics", "Zenith Platform Co."
    };

    var titles = new[]
    {
        "Principal Software Engineer", "Staff Full Stack Developer", "Engineering Manager",
        "Senior Backend Engineer", "Lead Platform Architect", "Director of Engineering",
        "Senior Frontend Engineer", "Site Reliability Engineer", "Head of Developer Experience"
    };

    var cities = new[]
    {
        "San Francisco, CA", "New York, NY", "Austin, TX", "Seattle, WA", "Boston, MA",
        "Chicago, IL", "Denver, CO", "Remote, EU", "London, UK", "Berlin, Germany"
    };

    var techStacks = new[]
    {
        "C#, .NET, PostgreSQL, Redis, Kafka, Kubernetes, Azure",
        "TypeScript, React, Node.js, NestJS, GraphQL, Docker, AWS",
        "Go, gRPC, Terraform, Prometheus, Grafana, GCP",
        "Java, Spring Boot, MySQL, RabbitMQ, Elasticsearch, OpenTelemetry",
        "Python, FastAPI, Spark, Airflow, Snowflake, dbt",
        "Rust, WebAssembly, PostgreSQL, NATS, Linkerd, Helm"
    };

    var entries = new List<WorkExperienceEntry>();
    for (var index = 0; index < companies.Length; index++)
    {
        var startYear = 2026 - (index * 2);
        var endYear = index == 0 ? (int?)null : startYear + 2;
        var dateRange = endYear is null
            ? $"03 / {startYear - 1} – Present"
            : $"03 / {startYear - 1} – 01 / {endYear}";

        var achievementLines = Enumerable.Range(1, 8)
            .Select(line => $"Achievement {line}: Delivered measurable outcome #{index * 10 + line} with strong reliability and stakeholder alignment.")
            .ToArray();

        var descriptionLines = Enumerable.Range(1, 6)
            .Select(line => $"Responsibility {line}: Owned architecture, implementation, and operational excellence for mission-critical service area {line}.")
            .ToArray();

        entries.Add(new WorkExperienceEntry(
            titles[index % titles.Length],
            companies[index],
            cities[index % cities.Length],
            index % 3 == 0 ? "Contract" : "Full-time",
            dateRange,
            string.Join('\n', descriptionLines),
            string.Join('\n', achievementLines),
            techStacks[index % techStacks.Length],
            $"https://{Slug(companies[index])}.example.com"));
    }

    return entries;
}

static IReadOnlyList<EducationEntry> BuildEducation()
{
    var institutions = new[]
    {
        "Massachusetts Institute of Technology", "Stanford University", "Carnegie Mellon University",
        "University of California, Berkeley", "Georgia Institute of Technology", "University of Washington",
        "ETH Zurich", "Imperial College London", "Technical University of Munich", "University of Toronto",
        "Cornell University", "University of Illinois Urbana-Champaign"
    };

    var degrees = new[]
    {
        "MSc Computer Science", "BSc Software Engineering", "PhD Distributed Systems",
        "MEng Information Security", "MBA Technology Management", "BSc Mathematics"
    };

    var fields = new[]
    {
        "Distributed Systems", "Machine Learning", "Software Architecture", "Cybersecurity",
        "Human-Computer Interaction", "Database Systems", "Cloud Computing", "Data Engineering"
    };

    return institutions.Select((institution, index) =>
    {
        var startYear = 2000 + index;
        return new EducationEntry(
            degrees[index % degrees.Length],
            institution,
            fields[index % fields.Length],
            index % 2 == 0 ? "Cambridge, MA, USA" : "Zurich, Switzerland",
            index % 2 == 0 ? "Master's" : "Bachelor's",
            $"09 / {startYear} – 06 / {startYear + (index % 3 + 2)}",
            index % 4 == 0 ? "Summa Cum Laude" : "Magna Cum Laude",
            string.Join('\n', Enumerable.Range(1, 5).Select(line =>
                $"Thesis focus {line}: Research on scalable platform design, fault tolerance, and production-grade software delivery.")),
            $"https://{Slug(institution)}.edu");
    }).ToArray();
}

static IReadOnlyList<SkillsGroup> BuildSkills()
{
    var categories = new Dictionary<string, string[]>
    {
        ["Languages & Runtimes"] = ["C#", ".NET", "TypeScript", "JavaScript", "Go", "Rust", "Python", "Java", "Kotlin", "PHP", "Ruby", "Swift"],
        ["Frontend"] = ["React", "Next.js", "Vue.js", "Angular", "Avalonia", "Blazor", "Tailwind CSS", "Webpack", "Vite", "Storybook"],
        ["Backend & APIs"] = ["ASP.NET Core", "Node.js", "NestJS", "Express", "FastAPI", "Spring Boot", "GraphQL", "REST", "gRPC", "OpenAPI"],
        ["Data & Storage"] = ["PostgreSQL", "MySQL", "SQL Server", "MongoDB", "Redis", "Elasticsearch", "Snowflake", "BigQuery", "DynamoDB", "Cassandra"],
        ["Cloud & DevOps"] = ["Azure", "AWS", "GCP", "Docker", "Kubernetes", "Terraform", "Helm", "Argo CD", "GitHub Actions", "Azure DevOps"],
        ["Observability"] = ["OpenTelemetry", "Prometheus", "Grafana", "Datadog", "ELK Stack", "Jaeger", "Loki", "Sentry", "New Relic"],
        ["Security"] = ["OAuth 2.0", "OpenID Connect", "SAML 2.0", "RBAC", "ABAC", "Zero Trust", "Vault", "Keycloak", "OWASP", "SAST/DAST"],
        ["Architecture"] = ["Microservices", "Event-Driven Architecture", "CQRS", "DDD", "Clean Architecture", "Hexagonal Architecture", "API Gateway", "Service Mesh"],
        ["AI & Automation"] = ["LLM Integration", "Prompt Engineering", "RAG Pipelines", "GitHub Copilot", "Cursor", "MLOps", "Feature Stores", "Vector DBs"],
        ["Leadership"] = ["Team Leadership", "Hiring", "Mentoring", "Roadmapping", "Stakeholder Management", "Technical Writing", "Architecture Reviews", "Incident Response"],
        ["Testing"] = ["xUnit", "NUnit", "Jest", "Playwright", "Cypress", "Contract Testing", "Load Testing", "Chaos Engineering", "TDD", "BDD"],
        ["Mobile & Edge"] = ["React Native", "Flutter", "iOS", "Android", ".NET MAUI", "PWA", "WebSockets", "SSE", "CDN", "Edge Functions"]
    };

    var proficiencies = new[] { "Expert", "Advanced", "Intermediate" };
    return categories.Select(pair => new SkillsGroup(
        pair.Key,
        pair.Value.Select((skill, index) => new SkillItem(
            skill,
            proficiencies[index % proficiencies.Length],
            18 - (index % 12))).ToArray())).ToArray();
}

static IReadOnlyList<LanguageEntry> BuildLanguages()
{
    var languages = new[]
    {
        ("English", "Native", new[] { "Reading: C2", "Writing: C2", "Speaking: C2", "Listening: C2" }),
        ("Slovak", "Native", new[] { "Reading: C2", "Writing: C2", "Speaking: C2", "Listening: C2" }),
        ("German", "Fluent", new[] { "Reading: C1", "Writing: B2", "Speaking: C1", "Listening: C1" }),
        ("French", "Advanced", new[] { "Reading: B2", "Writing: B2", "Speaking: B1", "Listening: B2" }),
        ("Spanish", "Intermediate", new[] { "Reading: B1", "Writing: B1", "Speaking: B1", "Listening: B1" }),
        ("Italian", "Intermediate", new[] { "Reading: B1", "Writing: A2", "Speaking: B1", "Listening: B1" }),
        ("Czech", "Advanced", new[] { "Reading: C1", "Writing: B2", "Speaking: C1", "Listening: C1" }),
        ("Polish", "Intermediate", new[] { "Reading: B1", "Writing: A2", "Speaking: B1", "Listening: B1" }),
        ("Portuguese", "Beginner", new[] { "Reading: A2", "Writing: A2", "Speaking: A1", "Listening: A2" }),
        ("Japanese", "Beginner", new[] { "Reading: A1", "Writing: A1", "Speaking: A1", "Listening: A1" }),
        ("Mandarin Chinese", "Beginner", new[] { "Reading: A1", "Writing: A1", "Speaking: A1", "Listening: A1" }),
        ("Latin", "Academic", new[] { "Reading: B1", "Writing: A2", "Translation: B1" })
    };

    return languages.Select(entry => new LanguageEntry(
        $"{entry.Item1} — {entry.Item2}",
        entry.Item3)).ToArray();
}

static IReadOnlyList<CertificateEntry> BuildCertificates()
{
    var certs = new List<CertificateEntry>();
    for (var index = 1; index <= 24; index++)
    {
        certs.Add(new CertificateEntry(
            $"Professional Certification #{index:D2} — Enterprise Platform Engineering",
            [
                $"Issuing Organization: Global Tech Certification Board {index}",
                $"Credential ID: JD-CERT-{index:D4}-202{index % 6}",
                $"Issued: {index % 12 + 1:D2} / 202{index % 6}",
                $"Valid through: {index % 12 + 1:D2} / 202{index % 6 + 3}",
                $"Focus area: Cloud architecture, secure SDLC, and large-scale service operations."
            ]));
    }

    return certs;
}

static IReadOnlyList<ProjectEntry> BuildProjects()
{
    var projects = new List<ProjectEntry>();
    for (var index = 1; index <= 24; index++)
    {
        projects.Add(new ProjectEntry(
            $"Project Atlas {index:D2} — Multi-Region Platform Modernization",
            [
                $"Role: Technical Lead / Architect",
                $"Organization: Internal Innovation Lab {index}",
                $"Date range: 01 / 201{index % 10} – 12 / 202{index % 6}",
                $"URL: https://projects.johndoe.dev/atlas-{index:D2}",
                $"Technologies: .NET, React, PostgreSQL, Kafka, Kubernetes, Terraform, OpenTelemetry",
                string.Join('\n', Enumerable.Range(1, 4).Select(line =>
                    $"Highlight {line}: Delivered resilient subsystem {line} with strict SLOs, automated rollout safety, and measurable cost optimization.")),
                string.Join('\n', Enumerable.Range(1, 3).Select(line =>
                    $"Outcome {line}: Reduced incident rate, improved deployment frequency, and increased developer productivity across {100 + index * 5} engineers."))
            ]));
    }

    return projects;
}

static IReadOnlyList<string> BuildLinks()
{
    return Enumerable.Range(1, 20)
        .Select(index => $"Professional Link {index:D2} — https://links.johndoe.dev/resource-{index:D2}")
        .ToArray();
}

static string BuildAdditionalInformation()
{
    var paragraphs = new[]
    {
        "Conference speaking: Regular speaker at regional and international software architecture, platform engineering, and security conferences.",
        "Open source: Maintainer and contributor to multiple libraries focused on observability, API tooling, and developer productivity.",
        "Volunteering: Mentor for early-career engineers, university guest lecturer, and reviewer for technical publications.",
        "Publications: Author of internal architecture playbooks, postmortem guides, and engineering onboarding curricula adopted by multiple teams.",
        "Awards: Repeated recognition for incident response leadership, cross-team collaboration, and high-impact modernization programs.",
        "Interests: Distributed systems design, applied machine learning, technical writing, long-distance running, chess, and classical music."
    };

    return string.Join(
        "\n\n",
        paragraphs.Concat(Enumerable.Range(1, 40).Select(index =>
            $"Additional note {index:D2}: Extended detail about leadership philosophy, delivery practices, architecture decision records, " +
            "operational readiness reviews, and mentorship programs used to scale engineering organizations while preserving product quality.")));
}

static string Slug(string value) =>
    new string(value.ToLowerInvariant()
        .Select(character => char.IsLetterOrDigit(character) ? character : '-')
        .ToArray())
        .Trim('-');
