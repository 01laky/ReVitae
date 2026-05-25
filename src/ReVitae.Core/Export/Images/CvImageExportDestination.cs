namespace ReVitae.Core.Export.Images;

public abstract record CvImageExportDestination
{
    public sealed record ZipFile(string Path) : CvImageExportDestination;

    public sealed record Folder(string Path) : CvImageExportDestination;
}
