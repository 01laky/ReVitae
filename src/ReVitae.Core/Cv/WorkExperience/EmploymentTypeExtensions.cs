using ReVitae.Core.Localization;

namespace ReVitae.Core.Cv.WorkExperience;

public static class EmploymentTypeExtensions
{
	public static string ToTranslationKey(this EmploymentType employmentType)
	{
		return employmentType switch
		{
			EmploymentType.FullTime => TranslationKeys.EmploymentTypeFullTime,
			EmploymentType.PartTime => TranslationKeys.EmploymentTypePartTime,
			EmploymentType.Contract => TranslationKeys.EmploymentTypeContract,
			EmploymentType.Freelance => TranslationKeys.EmploymentTypeFreelance,
			EmploymentType.Internship => TranslationKeys.EmploymentTypeInternship,
			_ => TranslationKeys.EmploymentTypeFullTime
		};
	}

	public static IReadOnlyList<EmploymentType> SupportedValues { get; } =
	[
		EmploymentType.FullTime,
		EmploymentType.PartTime,
		EmploymentType.Contract,
		EmploymentType.Freelance,
		EmploymentType.Internship
	];
}
