using ReVitae.Core.Cv.ProfilePhoto;

namespace ReVitae.Tests;

public sealed class ProfilePhotoStorageTests : IDisposable
{
    private readonly string _tempDirectory = ProfilePhotoTestHelpers.CreateTempDirectory();
    private readonly ProfilePhotoStorage _storage;

    public ProfilePhotoStorageTests()
    {
        _storage = new ProfilePhotoStorage(_tempDirectory);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup for temp test directories.
        }
    }

    [Fact]
    public void TrySaveCopy_CreatesStoredFileWithExpectedExtension()
    {
        var source = ProfilePhotoTestHelpers.WriteMinimalPng(_tempDirectory);

        var result = _storage.TrySaveCopy(source);

        Assert.True(result.Success);
        Assert.EndsWith(".png", result.StoredPath, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(result.StoredPath));
        Assert.StartsWith(_tempDirectory, result.StoredPath, StringComparison.Ordinal);
    }

    [Fact]
    public void TrySaveCopy_ReplaceDeletesPreviousStoredFile()
    {
        var source = ProfilePhotoTestHelpers.WriteMinimalPng(_tempDirectory);
        var first = _storage.TrySaveCopy(source);
        Assert.True(first.Success);

        var second = _storage.TrySaveCopy(source, first.StoredPath);

        Assert.True(second.Success);
        Assert.False(File.Exists(first.StoredPath));
        Assert.True(File.Exists(second.StoredPath));
    }

    [Fact]
    public void TryDelete_RemovesStoredFile()
    {
        var source = ProfilePhotoTestHelpers.WriteMinimalPng(_tempDirectory);
        var saved = _storage.TrySaveCopy(source);
        Assert.True(saved.Success);

        Assert.True(_storage.TryDelete(saved.StoredPath));
        Assert.False(File.Exists(saved.StoredPath));
    }

    [Theory]
    [InlineData(".gif")]
    [InlineData(".bmp")]
    [InlineData(".heic")]
    [InlineData(".pdf")]
    public void TrySaveCopy_UnsupportedExtension_IsRejected(string extension)
    {
        var path = Path.Combine(_tempDirectory, "bad" + extension);
        File.WriteAllBytes(path, [0x01, 0x02, 0x03]);

        var result = _storage.TrySaveCopy(path);

        Assert.False(result.Success);
        Assert.Equal(ProfilePhotoSaveError.UnsupportedFormat, result.Error);
    }

    [Fact]
    public void TrySaveCopy_FileAtMaxSize_IsAccepted()
    {
        var source = ProfilePhotoTestHelpers.WriteMinimalJpeg(_tempDirectory);
        var target = Path.Combine(_tempDirectory, "max.jpg");
        File.Copy(source, target, overwrite: true);
        using (var stream = File.OpenWrite(target))
        {
            stream.SetLength(ProfilePhotoFormats.MaxFileSizeBytes);
        }

        var result = _storage.TrySaveCopy(target);

        Assert.True(result.Success);
        Assert.True(File.Exists(result.StoredPath));
    }

    [Fact]
    public void TrySaveCopy_FileOverMaxSize_IsRejected()
    {
        var path = ProfilePhotoTestHelpers.WriteOversizedPlaceholder(
            _tempDirectory,
            ProfilePhotoFormats.MaxFileSizeBytes + 1);

        var result = _storage.TrySaveCopy(path);

        Assert.False(result.Success);
        Assert.Equal(ProfilePhotoSaveError.FileTooLarge, result.Error);
    }

    [Fact]
    public void TrySaveCopy_EmptyFile_IsRejected()
    {
        var path = Path.Combine(_tempDirectory, "empty.jpg");
        File.WriteAllBytes(path, []);

        var result = _storage.TrySaveCopy(path);

        Assert.False(result.Success);
        Assert.Equal(ProfilePhotoSaveError.EmptyFile, result.Error);
    }

    [Fact]
    public void TrySaveCopy_CorruptBytes_AreRejected()
    {
        var path = Path.Combine(_tempDirectory, "corrupt.jpg");
        File.WriteAllBytes(path, [0x00, 0x01, 0x02, 0x03]);

        var result = _storage.TrySaveCopy(path);

        Assert.False(result.Success);
        Assert.Equal(ProfilePhotoSaveError.UnreadableImage, result.Error);
    }

    [Theory]
    [InlineData((ushort)1, 40, 20)]
    [InlineData((ushort)3, 40, 20)]
    [InlineData((ushort)6, 20, 40)]
    [InlineData((ushort)8, 20, 40)]
    public void TrySaveCopy_ExifOrientation_IsNormalized(ushort orientation, int expectedWidth, int expectedHeight)
    {
        var source = ProfilePhotoTestHelpers.WriteMinimalJpeg(_tempDirectory, orientation);

        var result = _storage.TrySaveCopy(source);

        Assert.True(result.Success);
        var (width, height) = ProfilePhotoTestHelpers.ReadImageDimensions(result.StoredPath!);
        Assert.Equal(expectedWidth, width);
        Assert.Equal(expectedHeight, height);
    }

    [Fact]
    public void TrySaveCopy_JpegWithoutExif_SavesSuccessfully()
    {
        var source = ProfilePhotoTestHelpers.WriteMinimalJpeg(_tempDirectory);

        var result = _storage.TrySaveCopy(source);

        Assert.True(result.Success);
        Assert.True(File.Exists(result.StoredPath));
    }

    [Fact]
    public void TrySaveCopy_Webp_IsTranscodedToJpeg()
    {
        var source = ProfilePhotoTestHelpers.WriteMinimalWebp(_tempDirectory);

        var result = _storage.TrySaveCopy(source);

        Assert.True(result.Success);
        Assert.EndsWith(".jpg", result.StoredPath, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("image/jpeg", result.ContentType);
    }

    [Fact]
    public void FileExists_StalePath_ReturnsFalseWithoutThrowing()
    {
        Assert.False(ProfilePhotoStorage.FileExists(Path.Combine(_tempDirectory, "missing.jpg")));
        Assert.False(ProfilePhotoStorage.FileExists(null));
        Assert.False(ProfilePhotoStorage.FileExists(string.Empty));
    }

    [Fact]
    public void TrySaveBytes_OversizedPayload_IsRejected()
    {
        var bytes = new byte[ProfilePhotoFormats.MaxFileSizeBytes + 1];

        var result = _storage.TrySaveBytes(bytes, "image/png");

        Assert.False(result.Success);
        Assert.Equal(ProfilePhotoSaveError.FileTooLarge, result.Error);
    }
}
