namespace ReVitae.Core.Projects;

public enum CvProjectPathValidationFailure
{
	None,
	EmptyPath,
	PathTraversal,
	NonRevitaeExtension,
	InvalidFileName
}

public sealed record CvProjectPathValidationResult(
	bool IsValid,
	CvProjectPathValidationFailure Failure = CvProjectPathValidationFailure.None,
	string? NormalizedPath = null)
{
	public static CvProjectPathValidationResult Valid(string normalizedPath) =>
		new(true, CvProjectPathValidationFailure.None, normalizedPath);

	public static CvProjectPathValidationResult Invalid(CvProjectPathValidationFailure failure) =>
		new(false, failure);
}

public static class CvProjectPathValidator
{
	private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
	{
		".revitae.json",
		".json"
	};

	public static CvProjectPathValidationResult ValidateOpenPath(string? filePath) =>
		ValidateProjectPath(filePath, requireRevitaeExtension: false);

	public static CvProjectPathValidationResult ValidateSavePath(string? filePath) =>
		ValidateProjectPath(filePath, requireRevitaeExtension: true);

	public static CvProjectPathValidationResult ValidateProjectPath(
		string? filePath,
		bool requireRevitaeExtension)
	{
		if (string.IsNullOrWhiteSpace(filePath))
		{
			return CvProjectPathValidationResult.Invalid(CvProjectPathValidationFailure.EmptyPath);
		}

		var trimmed = filePath.Trim();
		if (ContainsPathTraversal(trimmed))
		{
			return CvProjectPathValidationResult.Invalid(CvProjectPathValidationFailure.PathTraversal);
		}

		var fileName = Path.GetFileName(trimmed);
		if (string.IsNullOrWhiteSpace(fileName) || fileName is "." or "..")
		{
			return CvProjectPathValidationResult.Invalid(CvProjectPathValidationFailure.InvalidFileName);
		}

		if (requireRevitaeExtension && !HasAllowedExtension(trimmed))
		{
			return CvProjectPathValidationResult.Invalid(CvProjectPathValidationFailure.NonRevitaeExtension);
		}

		try
		{
			var fullPath = Path.GetFullPath(trimmed);
			return CvProjectPathValidationResult.Valid(fullPath);
		}
		catch
		{
			return CvProjectPathValidationResult.Invalid(CvProjectPathValidationFailure.InvalidFileName);
		}
	}

	public static bool ContainsPathTraversal(string path)
	{
		if (path.Contains("..", StringComparison.Ordinal))
		{
			return true;
		}

		try
		{
			var parts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			return parts.Any(part => part is "." or "..");
		}
		catch
		{
			return true;
		}
	}

	private static bool HasAllowedExtension(string path)
	{
		foreach (var extension in AllowedExtensions)
		{
			if (path.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}
}
