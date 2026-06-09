using ReVitae.Core.Import;

namespace ReVitae.Core.Ai.Cv;

/// <summary>
/// Sanitized diagnostics for the per-section advisor and import field repair (045 C.10).
/// Reuses the <c>REVITAE_IMPORT_DEBUG</c> gate and <see cref="CvImportDiagnosticsLogger"/>
/// conventions with <c>ai-advisor</c> / <c>ai-repair</c> step prefixes. Logs only counts,
/// kinds, and timings — never CV text, emails, or model output bodies.
/// </summary>
public static class AiCvDiagnosticsLogger
{
	public const string AdvisorStep = "ai-advisor";
	public const string RepairStep = "ai-repair";

	public static bool IsEnabled => CvImportDiagnosticsLogger.IsEnabled;

	public static void LogAdvisor(
		CvImportSectionId section,
		string profileOrBackend,
		int sectionChars,
		bool hasTargetContext,
		string culture,
		bool cacheHit) =>
		CvImportDiagnosticsLogger.LogStep(
			AdvisorStep,
			$"advisor section={section} backend={profileOrBackend} sectionChars={sectionChars} " +
			$"targetContext={hasTargetContext} culture={culture} cacheHit={cacheHit}");

	public static void LogAdvisorResult(
		CvImportSectionId section,
		bool success,
		int suggestionCount,
		int guardHitCount,
		long durationMs) =>
		CvImportDiagnosticsLogger.LogStep(
			AdvisorStep,
			$"advisorResult section={section} success={success} suggestions={suggestionCount} " +
			$"entityGuardHits={guardHitCount} durationMs={durationMs}");

	public static void LogRepairStart(int requestedFields, int sentFields, int droppedFields, string culture) =>
		CvImportDiagnosticsLogger.LogStep(
			RepairStep,
			$"repairStart requested={requestedFields} sent={sentFields} dropped={droppedFields} culture={culture}");

	public static void LogRepairResult(bool success, int repairedFields, int batchesFailed, long durationMs) =>
		CvImportDiagnosticsLogger.LogStep(
			RepairStep,
			$"repairResult success={success} repaired={repairedFields} batchesFailed={batchesFailed} durationMs={durationMs}");

	public static void LogParseError(string step, string sanitizedError) =>
		CvImportDiagnosticsLogger.LogStep(step, $"parseError={sanitizedError}");
}
