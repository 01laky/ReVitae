namespace ReVitae.Core.Export.Images;

public sealed record CvImageExportOptions(
    CvImageExportFormat Format,
    CvImageExportDelivery Delivery,
    CvImageExportScale Scale,
    int Quality,
    CvImagePageRange PageRange)
{
    public static CvImageExportOptions Default => new(
        CvImageExportFormat.Png,
        CvImageExportDelivery.ZipArchive,
        CvImageExportScale.High,
        90,
        CvImagePageRange.AllPages);
}
