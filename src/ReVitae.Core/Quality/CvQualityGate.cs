using ReVitae.Core.Cv.ProfilePhoto;
using ReVitae.Core.Export;
using ReVitae.Core.Import;

namespace ReVitae.Core.Quality;

public static class CvQualityGate
{
    public static bool HasStartedCv(CvExportSourceData data)
    {
        if (CvQualityTextHelper.HasText(data.Personal.FirstName)
            || CvQualityTextHelper.HasText(data.Personal.LastName))
        {
            return true;
        }

        return data.WorkExperience.Count > 0
            || data.Education.Count > 0
            || data.Skills.Count > 0
            || data.Languages.Count > 0
            || data.Certificates.Count > 0
            || data.Projects.Count > 0
            || data.Links.Count > 0
            || CvQualityTextHelper.HasText(data.AdditionalInformation)
            || CvQualityTextHelper.HasText(data.Personal.ProfessionalTitle)
            || CvQualityTextHelper.HasText(data.Personal.Email)
            || CvQualityTextHelper.HasText(data.Personal.ShortSummary)
            || ProfilePhotoStorage.FileExists(data.Personal.ProfilePhotoPath);
    }

    public static bool HasOtherSectionData(CvExportSourceData data, CvImportSectionId exclude)
    {
        if (exclude != CvImportSectionId.WorkExperience && data.WorkExperience.Count > 0)
        {
            return true;
        }

        if (exclude != CvImportSectionId.Education && data.Education.Count > 0)
        {
            return true;
        }

        if (exclude != CvImportSectionId.Skills && data.Skills.Count > 0)
        {
            return true;
        }

        if (exclude != CvImportSectionId.Languages && data.Languages.Count > 0)
        {
            return true;
        }

        if (exclude != CvImportSectionId.Certificates && data.Certificates.Count > 0)
        {
            return true;
        }

        if (exclude != CvImportSectionId.Projects && data.Projects.Count > 0)
        {
            return true;
        }

        if (exclude != CvImportSectionId.Links && data.Links.Count > 0)
        {
            return true;
        }

        if (exclude != CvImportSectionId.AdditionalInformation
            && CvQualityTextHelper.HasText(data.AdditionalInformation))
        {
            return true;
        }

        if (exclude != CvImportSectionId.PersonalInformation
            && (CvQualityTextHelper.HasText(data.Personal.ProfessionalTitle)
                || CvQualityTextHelper.HasText(data.Personal.Email)
                || CvQualityTextHelper.HasText(data.Personal.Phone)
                || CvQualityTextHelper.HasText(data.Personal.ShortSummary)
                || ProfilePhotoStorage.FileExists(data.Personal.ProfilePhotoPath)))
        {
            return true;
        }

        return false;
    }
}
