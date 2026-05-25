using Avalonia.Controls;

namespace ReVitae.Ui.Validation;

public interface IValidationNavigableSection
{
	bool ExpandAndRevealField(string fieldKey);

	Control? FindControlForFieldKey(string fieldKey);
}
