using ReVitae.Core.Localization;

namespace ReVitae.Core.Import;

/// <summary>Mirrors legacy <see cref="CvPdfImporter.ImportFromText"/> behavior using shared keys.</summary>
public static class CvTextImportPipeline
{
    /// <returns>
    /// A parsed <see cref="CvImportResult"/> or failure with <see cref="TranslationKeys.ImportErrorEmptyDocument"/>
    /// when text is whitespace-only after normalization, otherwise <see cref="TranslationKeys.ImportErrorNoStructuredData"/>.
    /// </returns>
    public static CvImportResult Import(
        string rawText,
        IReadOnlyList<string>? hyperlinkUrls = null,
        IReadOnlyList<CvImportWarning>? extractionWarnings = null)
    {
        var normalized = CvTextNormalizer.Normalize(rawText);
        CvImportDiagnosticsLogger.LogStep(
            "text-pipeline",
            $"Normalize: raw={rawText.Length} chars → normalized={normalized.Length} chars");

        if (string.IsNullOrWhiteSpace(normalized))
        {
            CvImportDiagnosticsLogger.LogStep("text-pipeline", "Normalized text empty — aborting");
            return CvImportResult.Failed(TranslationKeys.ImportErrorEmptyDocument);
        }

        var segmentation = CvSectionSegmenter.Segment(normalized);
        CvImportDiagnosticsLogger.LogStep(
            "text-pipeline",
            $"Segmented: {segmentation.SectionBodies.Count} section(s), " +
            $"header={segmentation.HeaderBlock.Length} chars, warnings={segmentation.Warnings.Count}");
        var result = CvImportFieldExtractor.Extract(segmentation, hyperlinkUrls);
        CvImportDiagnosticsLogger.LogStep(
            "text-pipeline",
            $"Extract: success={result.Success}, work={result.WorkExperienceEntries.Count}, " +
            $"education={result.EducationEntries.Count}, skills={result.SkillsGroups.Count}");
        if (!result.Success)
        {
            CvImportDiagnosticsLogger.LogTextPipeline(rawText, segmentation, result);
            return CvImportResult.Failed(TranslationKeys.ImportErrorNoStructuredData);
        }

        var merged = MergePrefixes(result, extractionWarnings, segmentation.Warnings);
        CvImportDiagnosticsLogger.LogTextPipeline(rawText, segmentation, merged);
        return merged;
    }

    /// <remarks>Prepends extractor warnings ahead of segmentation and field warnings without changing duplicates.</remarks>
    private static CvImportResult MergePrefixes(
        CvImportResult inner,
        IReadOnlyList<CvImportWarning>? extractionWarnings,
        IReadOnlyList<CvImportWarning>? segmentationWarnings)
    {
        if ((extractionWarnings is null || extractionWarnings.Count == 0)
            && (segmentationWarnings is null || segmentationWarnings.Count == 0))
        {
            return inner;
        }

        var merged = new List<CvImportWarning>();
        if (extractionWarnings is { Count: > 0 })
        {
            merged.AddRange(extractionWarnings);
        }

        if (segmentationWarnings is { Count: > 0 })
        {
            merged.AddRange(segmentationWarnings);
        }

        merged.AddRange(inner.Warnings);

        return new CvImportResult
        {
            Success = inner.Success,
            ErrorMessageKey = inner.ErrorMessageKey,
            Personal = inner.Personal,
            WorkExperienceEntries = inner.WorkExperienceEntries,
            EducationEntries = inner.EducationEntries,
            SkillsGroups = inner.SkillsGroups,
            LanguageEntries = inner.LanguageEntries,
            CertificateEntries = inner.CertificateEntries,
            ProjectEntries = inner.ProjectEntries,
            LinkEntries = inner.LinkEntries,
            AdditionalInformationContent = inner.AdditionalInformationContent,
            SectionHasData = inner.SectionHasData,
            Warnings = merged,
            FieldConfidences = inner.FieldConfidences
        };
    }
}
