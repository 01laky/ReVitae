using System.IO.Compression;

namespace ReVitae.Core.Export.Images;

public interface ICvImageExportPackager
{
    CvExportResult Write(
        IReadOnlyList<CvImageExportPageBytes> pages,
        CvImageExportDestination destination,
        string? firstName,
        string? lastName);
}

public sealed record CvImageExportPageBytes(int PageIndex, byte[] Bytes, CvImageExportFormat Format);

public sealed class CvImageExportZipPackager : ICvImageExportPackager
{
    public CvExportResult Write(
        IReadOnlyList<CvImageExportPageBytes> pages,
        CvImageExportDestination destination,
        string? firstName,
        string? lastName)
    {
        if (pages.Count == 0)
        {
            return new CvExportResult(false, Localization.TranslationKeys.ExportImageRasterFailed);
        }

        if (destination is not CvImageExportDestination.ZipFile zip)
        {
            return new CvExportResult(false, Localization.TranslationKeys.ExportFailed);
        }

        try
        {
            if (File.Exists(zip.Path))
            {
                File.Delete(zip.Path);
            }

            using (var stream = File.Create(zip.Path))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: false))
            {
                foreach (var page in pages.OrderBy(p => p.PageIndex))
                {
                    var entryName = CvImageExportFilenameHelper.FormatZipEntryName(page.PageIndex, page.Format);
                    var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    entryStream.Write(page.Bytes, 0, page.Bytes.Length);
                }
            }

            return new CvExportResult(true);
        }
        catch
        {
            return new CvExportResult(false, Localization.TranslationKeys.ExportFailed);
        }
    }
}

public sealed class CvImageExportSeparateFilesPackager : ICvImageExportPackager
{
    public CvExportResult Write(
        IReadOnlyList<CvImageExportPageBytes> pages,
        CvImageExportDestination destination,
        string? firstName,
        string? lastName)
    {
        if (pages.Count == 0)
        {
            return new CvExportResult(false, Localization.TranslationKeys.ExportImageRasterFailed);
        }

        if (destination is not CvImageExportDestination.Folder folder)
        {
            return new CvExportResult(false, Localization.TranslationKeys.ExportFailed);
        }

        try
        {
            Directory.CreateDirectory(folder.Path);

            foreach (var page in pages.OrderBy(p => p.PageIndex))
            {
                var filename = CvImageExportFilenameHelper.SuggestImagePageFilename(
                    firstName,
                    lastName,
                    page.PageIndex,
                    page.Format);
                var targetPath = CvImageExportFilenameHelper.ResolveCollisionSafePath(folder.Path, filename);
                File.WriteAllBytes(targetPath, page.Bytes);
            }

            return new CvExportResult(true);
        }
        catch
        {
            return new CvExportResult(false, Localization.TranslationKeys.ExportFailed);
        }
    }
}

public static class CvImageExportPackagerFactory
{
    public static ICvImageExportPackager Create(CvImageExportDelivery delivery) => delivery switch
    {
        CvImageExportDelivery.ZipArchive => new CvImageExportZipPackager(),
        CvImageExportDelivery.SeparateFiles => new CvImageExportSeparateFilesPackager(),
        _ => throw new ArgumentOutOfRangeException(nameof(delivery), delivery, null)
    };
}
