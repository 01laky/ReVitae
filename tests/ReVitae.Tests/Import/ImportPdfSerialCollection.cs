namespace ReVitae.Tests.Import;

/// <summary>
/// Serializes PDF import, John Doe matrix, and the template render-signature golden tests.
/// QuestPDF generation, temp matrix files, and PdfPig extraction can flake when xUnit runs these
/// PDF-heavy fixtures in parallel with each other (the golden oracle renders all 56 templates;
/// running that concurrently with the re-import stress loop starved the import parse on Linux CI).
/// </summary>
[CollectionDefinition(nameof(ImportPdfSerialCollection), DisableParallelization = true)]
public sealed class ImportPdfSerialCollection;
