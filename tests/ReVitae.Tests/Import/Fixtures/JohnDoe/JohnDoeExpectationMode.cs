namespace ReVitae.Tests.Import.Fixtures.JohnDoe;

public enum JohnDoeExpectationMode
{
    Full,
    PdfFull,
    StandardEntryCounts,
    PdfTemplateLayout
}

public static class JohnDoeExpectationModes
{
    public static bool RequiresZeroValidationErrors(JohnDoeExpectationMode mode) =>
        mode is JohnDoeExpectationMode.Full
            or JohnDoeExpectationMode.PdfFull
            or JohnDoeExpectationMode.StandardEntryCounts;
}
