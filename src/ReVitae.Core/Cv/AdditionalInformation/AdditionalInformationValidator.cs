using ReVitae.Core.Validation;

namespace ReVitae.Core.Cv.AdditionalInformation;

public sealed class AdditionalInformationValidator
{
    private readonly FieldValidator _validator = AdditionalInformationSchema.CreateValidator();

    public FieldValidationResult Validate(AdditionalInformationContent content)
    {
        if (!content.HasUserInput())
        {
            return new FieldValidationResult([]);
        }

        return _validator.Validate(content.ToFieldValues());
    }
}
