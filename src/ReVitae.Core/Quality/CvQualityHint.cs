using ReVitae.Core.Import;

namespace ReVitae.Core.Quality;

public sealed record CvQualityHint(
	string Id,
	string MessageKey,
	CvQualityHintSeverity Severity,
	CvImportSectionId? Section = null,
	string? FieldKey = null,
	string? EntryId = null);
