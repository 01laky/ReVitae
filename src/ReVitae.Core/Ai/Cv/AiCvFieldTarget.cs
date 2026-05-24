using ReVitae.Core.Import;

namespace ReVitae.Core.Ai.Cv;

public sealed record AiCvFieldTarget(
    CvImportSectionId Section,
    string FieldKey,
    string? EntryId = null);
