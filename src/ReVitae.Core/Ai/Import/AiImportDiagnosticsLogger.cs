using ReVitae.Core.Import;

namespace ReVitae.Core.Ai.Import;

public static class AiImportDiagnosticsLogger
{
    public static bool IsEnabled =>
        string.Equals(Environment.GetEnvironmentVariable("REVITAE_IMPORT_DEBUG"), "1", StringComparison.Ordinal);

    public static void LogSessionStart(string profileId, int totalBatches, string uiCulture) =>
        CvImportDiagnosticsLogger.LogStep(
            "ai-import",
            $"AI import started — profile={profileId}, totalBatches={totalBatches}, uiCulture={uiCulture}");

    public static void LogBatch(
        AiImportPhase phase,
        int batchIndex,
        int batchCountInPhase,
        string profileId,
        int inputChars,
        int carryForwardChars,
        int outputChars,
        bool parseOk,
        bool retryUsed,
        long durationMs) =>
        CvImportDiagnosticsLogger.LogStep(
            "ai-import",
            $"batch phase={phase} index={batchIndex}/{batchCountInPhase} profile={profileId} " +
            $"inputChars={inputChars} carryForwardChars={carryForwardChars} outputChars={outputChars} " +
            $"parseOk={parseOk} retryUsed={retryUsed} durationMs={durationMs}");

    public static void LogSessionEnd(bool success, int batchesOk, int batchesFailed) =>
        CvImportDiagnosticsLogger.LogStep(
            "ai-import",
            $"AI import finished — success={success}, batchesOk={batchesOk}, batchesFailed={batchesFailed}");

    public static void LogParseError(string sanitizedError) =>
        CvImportDiagnosticsLogger.LogStep("ai-import", $"parseError={sanitizedError}");
}
