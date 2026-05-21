namespace ReVitae.Core.Import;

public sealed class PersonalInformationImport
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string ProfessionalTitle { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string LinkedInUrl { get; set; } = string.Empty;

    public string PortfolioUrl { get; set; } = string.Empty;

    public string GitHubUrl { get; set; } = string.Empty;

    public string ShortSummary { get; set; } = string.Empty;

    public bool HasAnyData()
    {
        return !string.IsNullOrWhiteSpace(FirstName)
            || !string.IsNullOrWhiteSpace(LastName)
            || !string.IsNullOrWhiteSpace(ProfessionalTitle)
            || !string.IsNullOrWhiteSpace(Email)
            || !string.IsNullOrWhiteSpace(Phone)
            || !string.IsNullOrWhiteSpace(Location)
            || !string.IsNullOrWhiteSpace(LinkedInUrl)
            || !string.IsNullOrWhiteSpace(PortfolioUrl)
            || !string.IsNullOrWhiteSpace(GitHubUrl)
            || !string.IsNullOrWhiteSpace(ShortSummary);
    }
}
