namespace ReVitae.Tests.Import;

/// <summary>
/// Serializes PDF import and John Doe matrix tests. QuestPDF generation, temp matrix files,
/// and PdfPig extraction can flake when xUnit runs these fixtures in parallel with the full suite.
/// </summary>
[CollectionDefinition(nameof(ImportPdfSerialCollection), DisableParallelization = true)]
public sealed class ImportPdfSerialCollection;
