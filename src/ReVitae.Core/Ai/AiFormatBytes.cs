namespace ReVitae.Core.Ai;

public static class AiFormatBytes
{
    private static readonly string[] Units = ["B", "KB", "MB", "GB", "TB"];

    public static string Format(long bytes)
    {
        if (bytes <= 0)
        {
            return "0 B";
        }

        var value = (double)bytes;
        var unitIndex = 0;
        while (value >= 1024 && unitIndex < Units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return unitIndex == 0
            ? $"{bytes} {Units[unitIndex]}"
            : $"{value:0.#} {Units[unitIndex]}";
    }
}

public static class AiPlatformDisplay
{
    public static string GetPlatformLabel(AiPlatform platform) =>
        platform switch
        {
            AiPlatform.Windows => "Windows",
            AiPlatform.MacOS => "macOS",
            AiPlatform.Linux => "Linux",
            _ => "Unknown",
        };
}
