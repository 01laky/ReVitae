namespace ReVitae.Core.Import;

public static class CvImportSectionMetrics
{
    public static int CountPopulatedSections(IReadOnlyDictionary<CvImportSectionId, bool> flags) =>
        flags.Count(pair => pair.Value);

    public static bool IsStructuredFormat(CvImportFormat format) =>
        format is CvImportFormat.ReVitaeJson
            or CvImportFormat.JsonResume
            or CvImportFormat.EuropassXml
            or CvImportFormat.HrXml
            or CvImportFormat.CsvTabular;

    public static bool IsTextRouteFormat(CvImportFormat format) =>
        !IsStructuredFormat(format) && format != CvImportFormat.Unknown;
}
