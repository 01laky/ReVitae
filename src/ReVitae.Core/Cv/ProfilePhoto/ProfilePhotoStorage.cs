using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

using ReVitae.Core.Ai;

namespace ReVitae.Core.Cv.ProfilePhoto;

public sealed class ProfilePhotoStorage
{
	private readonly string _storageDirectory;

	public ProfilePhotoStorage()
		: this(GetDefaultStorageDirectory())
	{
	}

	public ProfilePhotoStorage(string storageDirectory)
	{
		_storageDirectory = storageDirectory;
		Directory.CreateDirectory(_storageDirectory);
	}

	public string StorageDirectory => _storageDirectory;

	public ProfilePhotoSaveResult TrySaveCopy(string sourcePath, string? existingStoredPath = null)
	{
		if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
		{
			return ProfilePhotoSaveResult.Failed(ProfilePhotoSaveError.UnreadableImage);
		}

		var extension = Path.GetExtension(sourcePath);
		if (!ProfilePhotoFormats.IsSupportedExtension(extension))
		{
			return ProfilePhotoSaveResult.Failed(ProfilePhotoSaveError.UnsupportedFormat);
		}

		var fileInfo = new FileInfo(sourcePath);
		if (fileInfo.Length == 0)
		{
			return ProfilePhotoSaveResult.Failed(ProfilePhotoSaveError.EmptyFile);
		}

		if (fileInfo.Length > ProfilePhotoFormats.MaxFileSizeBytes)
		{
			return ProfilePhotoSaveResult.Failed(ProfilePhotoSaveError.FileTooLarge);
		}

		try
		{
			using var input = File.OpenRead(sourcePath);
			return SaveNormalizedImage(input, extension, existingStoredPath);
		}
		catch
		{
			return ProfilePhotoSaveResult.Failed(ProfilePhotoSaveError.UnreadableImage);
		}
	}

	public ProfilePhotoSaveResult TrySaveBytes(byte[] bytes, string contentType, string? existingStoredPath = null)
	{
		if (bytes.Length == 0)
		{
			return ProfilePhotoSaveResult.Failed(ProfilePhotoSaveError.EmptyFile);
		}

		if (bytes.Length > ProfilePhotoFormats.MaxFileSizeBytes)
		{
			return ProfilePhotoSaveResult.Failed(ProfilePhotoSaveError.FileTooLarge);
		}

		if (!ProfilePhotoFormats.TryGetExtensionForContentType(contentType, out var extension))
		{
			return ProfilePhotoSaveResult.Failed(ProfilePhotoSaveError.UnsupportedFormat);
		}

		try
		{
			using var input = new MemoryStream(bytes);
			return SaveNormalizedImage(input, extension, existingStoredPath);
		}
		catch
		{
			return ProfilePhotoSaveResult.Failed(ProfilePhotoSaveError.UnreadableImage);
		}
	}

	public bool TryDelete(string? storedPath)
	{
		if (string.IsNullOrWhiteSpace(storedPath) || !File.Exists(storedPath))
		{
			return false;
		}

		try
		{
			File.Delete(storedPath);
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static bool FileExists(string? storedPath)
	{
		return !string.IsNullOrWhiteSpace(storedPath) && File.Exists(storedPath);
	}

	public static string GetDefaultStorageDirectory() =>
		ReVitaeLocalDataPaths.GetProfilePhotosDirectory();

	private ProfilePhotoSaveResult SaveNormalizedImage(Stream input, string extension, string? existingStoredPath)
	{
		try
		{
			using var image = Image.Load(input);
			image.Mutate(context => context.AutoOrient());

			var outputExtension = ProfilePhotoFormats.ShouldTranscodeWebpForExport(extension)
				? ".jpg"
				: extension.ToLowerInvariant();

			var storedPath = Path.Combine(_storageDirectory, Guid.NewGuid().ToString("N") + outputExtension);
			var contentType = ProfilePhotoFormats.GetContentTypeForExtension(outputExtension);

			using (var output = File.Create(storedPath))
			{
				if (outputExtension is ".jpg" or ".jpeg")
				{
					image.Save(output, new JpegEncoder { Quality = 90 });
				}
				else if (outputExtension == ".png")
				{
					image.Save(output, new PngEncoder());
				}
				else if (outputExtension == ".webp")
				{
					image.Save(output, new WebpEncoder());
				}
				else
				{
					image.SaveAsJpeg(output);
					contentType = "image/jpeg";
				}
			}

			var savedInfo = new FileInfo(storedPath);
			if (savedInfo.Length == 0 || savedInfo.Length > ProfilePhotoFormats.MaxFileSizeBytes)
			{
				TryDelete(storedPath);
				return ProfilePhotoSaveResult.Failed(ProfilePhotoSaveError.StorageFailed);
			}

			TryDelete(existingStoredPath);
			return ProfilePhotoSaveResult.Succeeded(storedPath, contentType);
		}
		catch
		{
			return ProfilePhotoSaveResult.Failed(ProfilePhotoSaveError.UnreadableImage);
		}
	}
}
