namespace ReVitae.Core.Import;

public sealed record ImportedFieldConfidence(
    string FieldKey,
    CvImportConfidence Confidence);
