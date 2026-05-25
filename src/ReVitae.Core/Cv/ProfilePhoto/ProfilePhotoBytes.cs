namespace ReVitae.Core.Cv.ProfilePhoto;

public static class ProfilePhotoBytes
{
	public static byte[]? TryRead(string? photoPath)
	{
		if (!ProfilePhotoStorage.FileExists(photoPath))
		{
			return null;
		}

		try
		{
			return File.ReadAllBytes(photoPath!);
		}
		catch
		{
			return null;
		}
	}

	public static string? TryGetDataUri(string? photoPath)
	{
		var bytes = TryRead(photoPath);
		if (bytes is null || bytes.Length == 0)
		{
			return null;
		}

		var contentType = ProfilePhotoFormats.GetContentTypeForExtension(Path.GetExtension(photoPath!));
		return $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
	}
}
