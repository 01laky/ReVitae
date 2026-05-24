using ReVitae.Core.Localization;

namespace ReVitae.Core.Ai;

public static class AiSystemInfoFormatter
{
    public static IReadOnlyList<string> FormatDetailLines(
        SystemProfile profile,
        OllamaRuntimeStatus ollama,
        long? availableDiskBytes,
        AppLocalizer localizer)
    {
        var ramLabel = profile.TotalPhysicalMemoryBytes is long ramBytes
            ? AiFormatBytes.Format(ramBytes)
            : localizer.Get(TranslationKeys.AiSetupDiskSpaceUnknown);

        var diskLabel = availableDiskBytes is long diskBytes
            ? AiFormatBytes.Format(diskBytes)
            : localizer.Get(TranslationKeys.AiSetupDiskSpaceUnknown);

        var ollamaLabel = ollama.IsReachable
            ? localizer.Format(
                TranslationKeys.AiSetupSystemOllamaRunning,
                ollama.InstalledModelTags.Count)
            : localizer.Get(TranslationKeys.AiSetupSystemOllamaStopped);

        return
        [
            localizer.Format(
                TranslationKeys.AiSetupSystemPlatform,
                AiPlatformDisplay.GetPlatformLabel(profile.Platform)),
            localizer.Format(TranslationKeys.AiSetupSystemArchitecture, profile.Architecture),
            localizer.Format(TranslationKeys.AiSetupSystemCpu, profile.ProcessorCount),
            localizer.Format(TranslationKeys.AiSetupSystemRam, ramLabel),
            localizer.Format(TranslationKeys.AiSetupSystemDisk, diskLabel),
            ollamaLabel,
        ];
    }
}
