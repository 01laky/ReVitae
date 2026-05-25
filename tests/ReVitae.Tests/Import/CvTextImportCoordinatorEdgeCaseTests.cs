using ReVitae.Core.Import;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Import;

public sealed class CvTextImportCoordinatorEdgeCaseTests
{
    [Fact]
    public void ImportDeterministicOnly_NormalizesImportErrorEmptyPdfToEmptyDocument()
    {
        using var dir = new TempImportDirectory();
        var path = dir.FilePath("empty.pdf", MinimalPdfWriter.CreateFromLines([]));

        var result = CvTextImportCoordinator.ImportDeterministicOnly(path);

        Assert.False(result.Success);
        Assert.Equal(TranslationKeys.ImportErrorEmptyDocument, result.ErrorMessageKey);
    }

    [Fact]
    public void ImportDeterministicOnly_NormalizesImportErrorUnreadablePdfToUnreadableDocument()
    {
        using var dir = new TempImportDirectory();
        var path = dir.FilePath("corrupt.pdf", [0x00, 0x01, 0x02]);

        var result = CvTextImportCoordinator.ImportDeterministicOnly(path);

        Assert.False(result.Success);
        Assert.Equal(TranslationKeys.ImportErrorUnreadableDocument, result.ErrorMessageKey);
    }

    [Fact]
    public void TryImport_ReturnsNullForMissingFile()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
        Assert.Null(CvTextImportCoordinator.TryImport(missing));
    }

    [Fact]
    public void TryImport_ReturnsNullWhenFileExceedsSizeLimit()
    {
        using var dir = new TempImportDirectory();
        var path = Path.Combine(dir.RootPath, "huge.txt");
        using (var stream = File.Create(path))
        {
            stream.SetLength(CvImportLimits.MaxFileBytes + 1);
        }

        Assert.Null(CvTextImportCoordinator.TryImport(path));
    }
}
