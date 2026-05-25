namespace ReVitae.Core.Cv.ProfilePhoto;

public enum ProfilePhotoSaveError
{
	None,
	UnsupportedFormat,
	FileTooLarge,
	EmptyFile,
	UnreadableImage,
	StorageFailed
}

public sealed record ProfilePhotoSaveResult(
	bool Success,
	string? StoredPath,
	string? ContentType,
	ProfilePhotoSaveError Error = ProfilePhotoSaveError.None)
{
	public static ProfilePhotoSaveResult Succeeded(string storedPath, string contentType) =>
		new(true, storedPath, contentType);

	public static ProfilePhotoSaveResult Failed(ProfilePhotoSaveError error) =>
		new(false, null, null, error);
}
