namespace ReVitae.Core.Ai.Import;

public sealed record AiImportBatchDescriptor(
    AiImportPhase Phase,
    int BatchIndex,
    int BatchCountInPhase,
    int GlobalStepIndex,
    string SliceText);

public sealed record AiImportBatchPlan(
    AiImportBatchProfile Profile,
    IReadOnlyList<AiImportBatchDescriptor> Batches)
{
    public int TotalBatchCount => Batches.Count;
}
