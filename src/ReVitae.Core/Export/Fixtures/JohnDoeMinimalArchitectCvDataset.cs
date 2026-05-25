using ReVitae.Core.Localization;

namespace ReVitae.Core.Export.Fixtures;

/// <summary>
/// Compact John Doe CV with realistic English content in every section — one senior
/// software architect profile for manual import, demos, and quality-hint testing.
/// </summary>
public static class JohnDoeMinimalArchitectCvDataset
{
	public static CvExportDocument CreateDocument(
		CvExportTemplateId templateId = CvExportTemplateId.CleanTopHeader,
		AppLocalizer? localizer = null)
	{
		localizer ??= new AppLocalizer("en");
		var labels = JohnDoeStressCvDataset.CreateEnglishLabels(localizer);

		return new CvExportDocument(
			templateId,
			labels,
			FirstName: "John",
			LastName: "Doe",
			ProfessionalTitle: "Senior Software Architect",
			Email: "john.doe@example.com",
			Phone: "+1 (415) 555-0142",
			Location: "Austin, TX, USA",
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
	}

	public static string BuildSummary() =>
		"Senior software architect with 15 years of experience designing cloud-native platforms, " +
		"integration boundaries, and engineering standards for product-led SaaS companies. " +
		"Known for pragmatic architecture decisions, clear technical communication, and helping " +
		"teams ship reliable systems without unnecessary complexity.";

	public static IReadOnlyList<WorkExperienceEntry> BuildWorkExperience() =>
	[
		new(
			"Principal Software Architect",
			"Meridian Payments",
			"Austin, TX",
			"Full-time",
			"04 / 2021 – Present",
			"Own the architecture for Meridian's payment orchestration platform, which processes more than 40 million transactions per month across North America and Europe.",
			"Defined event-driven integration standards adopted by 12 product teams.\nReduced cross-service incident volume by 35% through clearer ownership boundaries and observability baselines.",
			"C#, .NET 8, PostgreSQL, Kafka, AWS, Kubernetes, Terraform, OpenTelemetry",
			"https://meridianpayments.example.com"),
		new(
			"Senior Software Engineer",
			"Harbor Health Technologies",
			"Boston, MA",
			"Full-time",
			"06 / 2016 – 03 / 2021",
			"Led modernization of a clinical scheduling and billing module from a legacy monolith into independently deployable services.",
			"Introduced domain-driven design workshops and an architecture review cadence used by engineering leadership.\nMentored four engineers who progressed into tech lead roles.",
			"C#, ASP.NET Core, SQL Server, Azure, Docker, RabbitMQ, Redis",
			"https://harborhealth.example.com"),
	];

	public static IReadOnlyList<EducationEntry> BuildEducation() =>
	[
		new(
			"MSc Computer Science",
			"University of Texas at Austin",
			"Software Engineering",
			"Austin, TX",
			"Master's",
			"09 / 2012 – 05 / 2014",
			"Distinction",
			"Thesis on resilient microservice communication patterns under partial network failure.",
			"https://www.utexas.edu"),
	];

	public static IReadOnlyList<SkillsGroup> BuildSkills() =>
	[
		new(
			"Architecture & Platforms",
			[
				new SkillItem("Microservices", "Expert", 12),
				new SkillItem("Event-Driven Architecture", "Expert", 10),
				new SkillItem("Domain-Driven Design", "Advanced", 9),
				new SkillItem("API & Integration Design", "Expert", 14),
			]),
		new(
			"Engineering",
			[
				new SkillItem("C# / .NET", "Expert", 15),
				new SkillItem("TypeScript", "Advanced", 8),
				new SkillItem("PostgreSQL", "Advanced", 12),
				new SkillItem("AWS", "Advanced", 9),
				new SkillItem("Kubernetes", "Advanced", 7),
				new SkillItem("Terraform", "Intermediate", 5),
			]),
	];

	public static IReadOnlyList<LanguageEntry> BuildLanguages() =>
	[
		new("English — Native", ["Reading: C2", "Writing: C2", "Speaking: C2"]),
		new("German — Professional working proficiency", ["Reading: B2", "Writing: B1", "Speaking: B2"]),
	];

	public static IReadOnlyList<CertificateEntry> BuildCertificates() =>
	[
		new(
			"AWS Certified Solutions Architect – Professional",
			[
				"Issuing organization: Amazon Web Services",
				"Credential ID: AWS-SAP-JD-2023",
				"Issued: 03 / 2023",
				"Valid through: 03 / 2026",
			]),
	];

	public static IReadOnlyList<ProjectEntry> BuildProjects() =>
	[
		new(
			"RelayGate — API reliability toolkit",
			[
				"Role: Creator & maintainer",
				"Organization: Open source",
				"Date range: 01 / 2022 – Present",
				"URL: https://github.com/johndoe/relaygate",
				"Technologies: Go, gRPC, Prometheus, Grafana",
				"Built a lightweight gateway helper for retries, circuit breaking, and structured request logging in distributed services.",
			]),
	];

	public static IReadOnlyList<string> BuildLinks() =>
	[
		"Architecture Weekly (blog) — https://blog.johndoe.dev",
		"QCon Austin 2024 — Platform engineering talk — https://qcon.ai/speakers/john-doe",
	];

	public static string BuildAdditionalInformation() =>
		"Conference speaker at QCon and DevOpsDays on platform engineering and architecture governance. " +
		"Contributor to the CNCF TAG App Delivery working group. Based in Austin, TX; open to hybrid senior architecture roles.";
}
