namespace ReVitae.Core.Export.Images;

public sealed record CvImagePageRange(int? FromPage, int? ToPage)
{
    public static CvImagePageRange AllPages => new(null, null);

    public bool IsAllPages => FromPage is null && ToPage is null;
}
