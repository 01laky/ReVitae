using ReVitae.Core.Cv.ProfilePhoto;
using ReVitae.Core.Import;

namespace ReVitae.Core.Export;

internal static class RevitaeJsonDtoBuilder
{
	public static Dictionary<string, object?> Build(CvExportSourceData source)
	{
		var photoBytes = ProfilePhotoBytes.TryRead(source.Personal.ProfilePhotoPath);
		var hasPhoto = photoBytes is { Length: > 0 };
		var root = new Dictionary<string, object?> { ["revitaeVersion"] = hasPhoto ? 2 : 1 };

		if (HasPersonal(source.Personal) || hasPhoto)
		{
			root["personalInformation"] = BuildPersonalInformationDto(source.Personal, photoBytes);
		}

		if (source.WorkExperience.Count > 0)
		{
			root["workExperience"] = source.WorkExperience;
		}

		if (source.Education.Count > 0)
		{
			root["education"] = source.Education;
		}

		if (source.Skills.Count > 0)
		{
			root["skills"] = source.Skills.Select(g => new
			{
				category = g.Category,
				skills = g.Skills.Select(s => new { name = s.Name }).ToArray()
			}).ToArray();
		}

		if (source.Languages.Count > 0)
		{
			root["languages"] = source.Languages;
		}

		if (source.Certificates.Count > 0)
		{
			root["certificates"] = source.Certificates;
		}

		if (source.Projects.Count > 0)
		{
			root["projects"] = source.Projects;
		}

		if (source.Links.Count > 0)
		{
			root["links"] = source.Links;
		}

		if (!string.IsNullOrWhiteSpace(source.AdditionalInformation))
		{
			root["additionalInformation"] = new { content = source.AdditionalInformation };
		}

		return root;
	}

	private static Dictionary<string, object?> BuildPersonalInformationDto(
		PersonalInformationImport personal,
		byte[]? photoBytes)
	{
		var dto = new Dictionary<string, object?>
		{
			["firstName"] = personal.FirstName,
			["lastName"] = personal.LastName,
			["professionalTitle"] = personal.ProfessionalTitle,
			["email"] = personal.Email,
			["phone"] = personal.Phone,
			["location"] = personal.Location,
			["linkedInUrl"] = personal.LinkedInUrl,
			["portfolioUrl"] = personal.PortfolioUrl,
			["gitHubUrl"] = personal.GitHubUrl,
			["shortSummary"] = personal.ShortSummary
		};

		if (photoBytes is { Length: > 0 })
		{
			var contentType = ProfilePhotoFormats.GetContentTypeForExtension(
				Path.GetExtension(personal.ProfilePhotoPath));
			dto["profilePhotoBase64"] = Convert.ToBase64String(photoBytes);
			dto["profilePhotoContentType"] = contentType;
		}

		return dto;
	}

	private static bool HasPersonal(PersonalInformationImport personal) =>
		!string.IsNullOrWhiteSpace(personal.FirstName)
		|| !string.IsNullOrWhiteSpace(personal.LastName)
		|| !string.IsNullOrWhiteSpace(personal.Email)
		|| !string.IsNullOrWhiteSpace(personal.ProfessionalTitle)
		|| !string.IsNullOrWhiteSpace(personal.Phone)
		|| !string.IsNullOrWhiteSpace(personal.Location)
		|| !string.IsNullOrWhiteSpace(personal.LinkedInUrl)
		|| !string.IsNullOrWhiteSpace(personal.PortfolioUrl)
		|| !string.IsNullOrWhiteSpace(personal.GitHubUrl)
		|| !string.IsNullOrWhiteSpace(personal.ShortSummary);
}
