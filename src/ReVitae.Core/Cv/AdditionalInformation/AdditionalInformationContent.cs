namespace ReVitae.Core.Cv.AdditionalInformation;

public sealed class AdditionalInformationContent
{
	public string Content { get; set; } = string.Empty;

	public bool HasUserInput()
	{
		return !string.IsNullOrWhiteSpace(Content);
	}

	public IReadOnlyDictionary<string, string?> ToFieldValues()
	{
		return new Dictionary<string, string?>(StringComparer.Ordinal)
		{
			[AdditionalInformationFieldKeys.Content] = Content
		};
	}
}
