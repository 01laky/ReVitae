using ReVitae.Core.Cv.ProfilePhoto;
using ReVitae.Core.Import;

namespace ReVitae.Tests;

public sealed class ProfilePhotoBytesTests
{
	[Fact]
	public void TryRead_ReturnsNullForMissingPath()
	{
		Assert.Null(ProfilePhotoBytes.TryRead(null));
		Assert.Null(ProfilePhotoBytes.TryRead(string.Empty));
		Assert.Null(ProfilePhotoBytes.TryRead(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.jpg")));
	}

	[Fact]
	public void TryRead_ReturnsBytesForExistingFile()
	{
		var directory = ProfilePhotoTestHelpers.CreateTempDirectory();
		try
		{
			var path = ProfilePhotoTestHelpers.WriteMinimalPng(directory);

			var bytes = ProfilePhotoBytes.TryRead(path);

			Assert.NotNull(bytes);
			Assert.NotEmpty(bytes!);
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	[Fact]
	public void TryGetDataUri_ReturnsNullWhenFileMissing()
	{
		Assert.Null(ProfilePhotoBytes.TryGetDataUri(null));
	}

	[Fact]
	public void TryGetDataUri_ReturnsDataUriForExistingPng()
	{
		var directory = ProfilePhotoTestHelpers.CreateTempDirectory();
		try
		{
			var path = ProfilePhotoTestHelpers.WriteMinimalPng(directory);

			var dataUri = ProfilePhotoBytes.TryGetDataUri(path);

			Assert.NotNull(dataUri);
			Assert.StartsWith("data:image/png;base64,", dataUri, StringComparison.Ordinal);
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}
}
