using ReVitae.Core.Cv;
using ReVitae.Core.Cv.AdditionalInformation;
using ReVitae.Core.Cv.Certificates;
using ReVitae.Core.Cv.Education;
using ReVitae.Core.Cv.Languages;
using ReVitae.Core.Cv.Links;
using ReVitae.Core.Cv.ProfilePhoto;
using ReVitae.Core.Cv.Projects;
using ReVitae.Core.Cv.Skills;
using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Import;
using ReVitae.Core.Validation;

namespace ReVitae.Tests.Import.Fixtures.JohnDoe;

public static class JohnDoePostImportValidator
{
    private static readonly FieldValidator PersonalValidator = MainPersonalInformationSchema.CreateValidator();
    private static readonly WorkExperienceCollectionValidator WorkValidator = new();
    private static readonly EducationCollectionValidator EducationValidator = new();
    private static readonly SkillsCollectionValidator SkillsValidator = new();
    private static readonly LanguagesCollectionValidator LanguagesValidator = new();
    private static readonly CertificatesCollectionValidator CertificatesValidator = new();
    private static readonly ProjectsCollectionValidator ProjectsValidator = new();
    private static readonly LinksCollectionValidator LinksValidator = new();
    private static readonly AdditionalInformationValidator AdditionalInformationValidator = new();

    public static FieldValidationResult Validate(CvImportResult result)
    {
        var personalValues = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [MainPersonalInformationFieldKeys.FirstName] = result.Personal.FirstName,
            [MainPersonalInformationFieldKeys.LastName] = result.Personal.LastName,
            [MainPersonalInformationFieldKeys.ProfessionalTitle] = result.Personal.ProfessionalTitle,
            [MainPersonalInformationFieldKeys.Email] = result.Personal.Email,
            [MainPersonalInformationFieldKeys.Phone] = result.Personal.Phone,
            [MainPersonalInformationFieldKeys.Location] = result.Personal.Location,
            [MainPersonalInformationFieldKeys.LinkedInUrl] = result.Personal.LinkedInUrl,
            [MainPersonalInformationFieldKeys.PortfolioUrl] = result.Personal.PortfolioUrl,
            [MainPersonalInformationFieldKeys.GitHubUrl] = result.Personal.GitHubUrl,
            [MainPersonalInformationFieldKeys.ShortSummary] = result.Personal.ShortSummary
        };

        var errors = PersonalValidator.Validate(personalValues).Errors
            .Concat(WorkValidator.Validate(result.WorkExperienceEntries).Errors)
            .Concat(EducationValidator.Validate(result.EducationEntries).Errors)
            .Concat(SkillsValidator.Validate(result.SkillsGroups).Errors)
            .Concat(LanguagesValidator.Validate(result.LanguageEntries).Errors)
            .Concat(CertificatesValidator.Validate(result.CertificateEntries).Errors)
            .Concat(ProjectsValidator.Validate(result.ProjectEntries).Errors)
            .Concat(LinksValidator.Validate(result.LinkEntries).Errors)
            .Concat(AdditionalInformationValidator.Validate(
                new AdditionalInformationContent { Content = result.AdditionalInformationContent ?? string.Empty }).Errors)
            .ToArray();

        return new FieldValidationResult(errors);
    }

    public static string FormatErrors(FieldValidationResult validation, int max = 12)
    {
        return string.Join(
            Environment.NewLine,
            validation.Errors.Take(max).Select(error => $"{error.FieldKey}: {error.Message}"));
    }
}
