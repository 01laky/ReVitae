namespace ReVitae.Core.Export;

using System.IO;
using System.Text;

public static class CvExportFilenameHelper
{
	public static string SuggestFilename(string? firstName, string? lastName, CvExportFormat format)
	{
		var extension = CvExportFormatCatalog.GetExtension(format);
		var suffix = CvExportFormatCatalog.GetFilenameSuffix(format);
		var sanitizedFirstName = SanitizePart(firstName);
		var sanitizedLastName = SanitizePart(lastName);

		if (string.IsNullOrWhiteSpace(sanitizedFirstName) || string.IsNullOrWhiteSpace(sanitizedLastName))
		{
			return $"ReVitae_CV{suffix}{extension}";
		}

		return $"{sanitizedFirstName}_{sanitizedLastName}_CV{suffix}{extension}";
	}

	public static string SuggestFilename(string? firstName, string? lastName) =>
		SuggestFilename(firstName, lastName, CvExportFormat.Pdf);

	private static string SanitizePart(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return string.Empty;
		}

		var invalidChars = Path.GetInvalidFileNameChars().ToHashSet();
		var builder = new StringBuilder(value.Trim().Length);

		foreach (var character in value.Trim())
		{
			if (char.IsWhiteSpace(character))
			{
				builder.Append('_');
				continue;
			}

			builder.Append(invalidChars.Contains(character) ? '_' : character);
		}

		return builder.ToString().Trim('_', '.');
	}
}
