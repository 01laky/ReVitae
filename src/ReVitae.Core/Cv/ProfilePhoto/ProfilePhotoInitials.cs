using System.Globalization;

namespace ReVitae.Core.Cv.ProfilePhoto;

public static class ProfilePhotoInitials
{
	public static string Derive(string? firstName, string? lastName)
	{
		var first = GetFirstTextElement(firstName);
		var last = GetFirstTextElement(lastName);

		if (!string.IsNullOrEmpty(first) && !string.IsNullOrEmpty(last))
		{
			return string.Concat(first, last).ToUpperInvariant();
		}

		var single = string.IsNullOrWhiteSpace(firstName) ? lastName : firstName;
		if (string.IsNullOrWhiteSpace(single))
		{
			return string.Empty;
		}

		var elements = new List<string>();
		var enumerator = StringInfo.GetTextElementEnumerator(single.Trim());
		while (enumerator.MoveNext() && elements.Count < 2)
		{
			elements.Add(enumerator.GetTextElement());
		}

		return string.Concat(elements).ToUpperInvariant();
	}

	private static string GetFirstTextElement(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return string.Empty;
		}

		var enumerator = StringInfo.GetTextElementEnumerator(value.Trim());
		return enumerator.MoveNext() ? enumerator.GetTextElement() : string.Empty;
	}
}
