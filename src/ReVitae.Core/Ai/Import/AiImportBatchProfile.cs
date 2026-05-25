namespace ReVitae.Core.Ai.Import;

public enum AiImportPhaseMode
{
    SequentialMicro = 0,
    SequentialSmall = 1,
    SectionBatch = 2,
    SectionFull = 3,
}

public enum AiImportPhase
{
    Personal = 0,
    Work = 1,
    Education = 2,
    Skills = 3,
    Languages = 4,
    SkillsAndLanguages = 5,
    Certificates = 6,
    Projects = 7,
    Links = 8,
    Additional = 9,
}

public enum AiCvImportMergeMode
{
    ReplaceAll = 0,
    FillEmptyOnly = 1,
}

public sealed record AiImportBatchProfile(
    string ProfileId,
    int MaxInputChars,
    int MaxOutputTokens,
    int WorkEntriesPerBatch,
    int EducationEntriesPerBatch,
    int ProjectsEntriesPerBatch,
    AiImportPhaseMode PhaseMode,
    int MaxCarryForwardSummaryChars,
    bool CombineSkillsAndLanguages,
    int PromptOverheadChars)
{
    public static AiImportBatchProfile Compact { get; } = new(
        "compact",
        1200,
        384,
        1,
        1,
        1,
        AiImportPhaseMode.SequentialMicro,
        280,
        true,
        350);

    public static AiImportBatchProfile Small { get; } = new(
        "small",
        2400,
        512,
        2,
        2,
        2,
        AiImportPhaseMode.SequentialSmall,
        450,
        false,
        500);

    public static AiImportBatchProfile Medium { get; } = new(
        "medium",
        5000,
        768,
        4,
        3,
        3,
        AiImportPhaseMode.SectionBatch,
        700,
        false,
        700);

    public static AiImportBatchProfile Large { get; } = new(
        "large",
        10_000,
        1024,
        8,
        6,
        6,
        AiImportPhaseMode.SectionBatch,
        1000,
        false,
        700);

    public static AiImportBatchProfile ExtraLarge { get; } = new(
        "extralarge",
        16_000,
        1536,
        12,
        8,
        8,
        AiImportPhaseMode.SectionFull,
        1200,
        false,
        700);
}
