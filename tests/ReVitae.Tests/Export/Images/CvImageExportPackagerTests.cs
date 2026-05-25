using System.IO.Compression;
using ReVitae.Core.Export.Images;
using ReVitae.Tests.Import;

namespace ReVitae.Tests.Export.Images;

public sealed class CvImageExportZipPackagerTests
{
    private static CvImageExportPageBytes Page(int index, CvImageExportFormat format = CvImageExportFormat.Png) =>
        new(index, [0x89, (byte)'P', (byte)'N', (byte)'G', 0, 1, 2, 3], format);

    [Fact]
    public void Write_SinglePage_CreatesValidZip()
    {
        using var temp = new TempImportDirectory();
        var zipPath = Path.Combine(temp.RootPath, "out.zip");
        var packager = new CvImageExportZipPackager();

        var result = packager.Write(
            [Page(1)],
            new CvImageExportDestination.ZipFile(zipPath),
            "Jane",
            "Doe");

        Assert.True(result.Success);
        using var archive = ZipFile.OpenRead(zipPath);
        Assert.Single(archive.Entries);
        Assert.Equal("page-01.png", archive.Entries[0].FullName);
    }

    [Fact]
    public void Write_ThreePages_CreatesOrderedEntries()
    {
        using var temp = new TempImportDirectory();
        var zipPath = Path.Combine(temp.RootPath, "out.zip");
        var packager = new CvImageExportZipPackager();

        var result = packager.Write(
            [Page(3), Page(1), Page(2)],
            new CvImageExportDestination.ZipFile(zipPath),
            null,
            null);

        Assert.True(result.Success);
        using var archive = ZipFile.OpenRead(zipPath);
        Assert.Equal(3, archive.Entries.Count);
        Assert.Equal(["page-01.png", "page-02.png", "page-03.png"], archive.Entries.Select(e => e.FullName).OrderBy(n => n).ToArray());
    }

    [Fact]
    public void Write_RangePages_UsesOriginalIndices()
    {
        using var temp = new TempImportDirectory();
        var zipPath = Path.Combine(temp.RootPath, "range.zip");
        var packager = new CvImageExportZipPackager();

        var result = packager.Write(
            [Page(2), Page(3)],
            new CvImageExportDestination.ZipFile(zipPath),
            "Jane",
            "Doe");

        Assert.True(result.Success);
        using var archive = ZipFile.OpenRead(zipPath);
        Assert.Equal(["page-02.png", "page-03.png"], archive.Entries.Select(e => e.FullName).OrderBy(n => n).ToArray());
    }

    [Fact]
    public void Write_EmptyPages_Fails()
    {
        using var temp = new TempImportDirectory();
        var zipPath = Path.Combine(temp.RootPath, "empty.zip");
        var packager = new CvImageExportZipPackager();

        var result = packager.Write(
            [],
            new CvImageExportDestination.ZipFile(zipPath),
            null,
            null);

        Assert.False(result.Success);
    }

    [Fact]
    public void Write_JpegExtension_UsesDotJpg()
    {
        using var temp = new TempImportDirectory();
        var zipPath = Path.Combine(temp.RootPath, "jpeg.zip");
        var packager = new CvImageExportZipPackager();

        packager.Write(
            [new CvImageExportPageBytes(1, [1, 2, 3], CvImageExportFormat.Jpeg)],
            new CvImageExportDestination.ZipFile(zipPath),
            null,
            null);

        using var archive = ZipFile.OpenRead(zipPath);
        Assert.Equal("page-01.jpg", archive.Entries[0].FullName);
    }

    [Fact]
    public void Write_WrongDestination_Fails()
    {
        using var temp = new TempImportDirectory();
        var packager = new CvImageExportZipPackager();
        var result = packager.Write(
            [Page(1)],
            new CvImageExportDestination.Folder(temp.RootPath),
            null,
            null);
        Assert.False(result.Success);
    }
}

public sealed class CvImageExportSeparateFilesPackagerTests
{
    [Fact]
    public void Write_CreatesPrefixedFiles()
    {
        using var temp = new TempImportDirectory();
        var packager = new CvImageExportSeparateFilesPackager();
        var pages = new[] { new CvImageExportPageBytes(1, [1, 2, 3], CvImageExportFormat.Png) };

        var result = packager.Write(
            pages,
            new CvImageExportDestination.Folder(temp.RootPath),
            "Jane",
            "Doe");

        Assert.True(result.Success);
        Assert.True(File.Exists(Path.Combine(temp.RootPath, "Jane_Doe_CV_page-01.png")));
    }

    [Fact]
    public void Write_MultiplePages_WritesAll()
    {
        using var temp = new TempImportDirectory();
        var packager = new CvImageExportSeparateFilesPackager();
        var pages = new[]
        {
            new CvImageExportPageBytes(1, [1], CvImageExportFormat.Png),
            new CvImageExportPageBytes(2, [2], CvImageExportFormat.Png)
        };

        var result = packager.Write(pages, new CvImageExportDestination.Folder(temp.RootPath), "Jane", "Doe");
        Assert.True(result.Success);
        Assert.Equal(2, Directory.GetFiles(temp.RootPath).Length);
    }

    [Fact]
    public void Write_Collision_AppendsSuffix()
    {
        using var temp = new TempImportDirectory();
        File.WriteAllBytes(Path.Combine(temp.RootPath, "Jane_Doe_CV_page-01.png"), [9]);
        var packager = new CvImageExportSeparateFilesPackager();

        packager.Write(
            [new CvImageExportPageBytes(1, [1], CvImageExportFormat.Png)],
            new CvImageExportDestination.Folder(temp.RootPath),
            "Jane",
            "Doe");

        Assert.True(File.Exists(Path.Combine(temp.RootPath, "Jane_Doe_CV_page-01-2.png")));
    }

    [Fact]
    public void Write_EmptyPages_Fails()
    {
        using var temp = new TempImportDirectory();
        var packager = new CvImageExportSeparateFilesPackager();
        var result = packager.Write([], new CvImageExportDestination.Folder(temp.RootPath), null, null);
        Assert.False(result.Success);
    }

    [Fact]
    public void Write_WrongDestination_Fails()
    {
        using var temp = new TempImportDirectory();
        var packager = new CvImageExportSeparateFilesPackager();
        var zipPath = Path.Combine(temp.RootPath, "out.zip");
        var result = packager.Write(
            [new CvImageExportPageBytes(1, [1], CvImageExportFormat.Png)],
            new CvImageExportDestination.ZipFile(zipPath),
            null,
            null);
        Assert.False(result.Success);
    }
}
