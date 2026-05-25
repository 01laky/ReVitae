namespace ReVitae.Tests.Import.Fixtures.JohnDoe;

public sealed record JohnDoeSectionCountExpectations(
	int WorkExperience,
	int Education,
	int LanguageEntries,
	int CertificateEntries,
	int ProjectEntries,
	int LinkEntries,
	int MinimumSkillItems,
	int MinimumSkillGroups,
	int MaximumSkillGroups);

public static class JohnDoeCanonicalExpectations
{
	public static JohnDoeSectionCountExpectations Counts { get; } = new(
		WorkExperience: 20,
		Education: 12,
		LanguageEntries: 12,
		CertificateEntries: 24,
		ProjectEntries: 24,
		LinkEntries: 20,
		MinimumSkillItems: 80,
		MinimumSkillGroups: 11,
		MaximumSkillGroups: 12);

	public const string FirstName = "John";
	public const string LastName = "Doe";
	public const string Email = "john.doe@example.com";
	public const string LinkedInUrl = "https://www.linkedin.com/in/john-doe-architect";
	public const string GitHubUrl = "https://github.com/johndoe";
	public const string PortfolioUrl = "https://johndoe.dev";
	public const string LocationFragment = "San Francisco";
	public const string TitleFragment = "Senior";

	public const string FirstWorkCompany = "Nimbus Cloud Systems";
	public const string LastWorkCompany = "Zenith Platform Co.";
	public const string FirstEducationInstitution = "Massachusetts Institute of Technology";
	public const string LastEducationInstitution = "University of Illinois Urbana-Champaign";
	public const string SampleSkillGroup = "Frontend";
	public const string SampleSkillName = "React";
}
