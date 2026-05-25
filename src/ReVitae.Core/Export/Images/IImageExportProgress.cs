namespace ReVitae.Core.Export.Images;

public enum ImageExportProgressPhase
{
    Rendering,
    Writing
}

public interface IImageExportProgress
{
    void Report(ImageExportProgressPhase phase, int currentPage, int totalPages);
}
