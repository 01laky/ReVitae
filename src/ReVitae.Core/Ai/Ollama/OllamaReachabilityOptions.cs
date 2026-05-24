namespace ReVitae.Core.Ai.Ollama;

public sealed record OllamaReachabilityOptions(
    int InitialProbeAttempts = 2,
    TimeSpan InitialProbeDelay = default,
    int LaunchProbeAttempts = 20,
    TimeSpan LaunchProbeDelay = default)
{
    public static OllamaReachabilityOptions Default { get; } = new(
        InitialProbeAttempts: 2,
        InitialProbeDelay: TimeSpan.FromMilliseconds(500),
        LaunchProbeAttempts: 20,
        LaunchProbeDelay: TimeSpan.FromSeconds(1));

    public static OllamaReachabilityOptions FastForTests { get; } = new(
        InitialProbeAttempts: 1,
        InitialProbeDelay: TimeSpan.Zero,
        LaunchProbeAttempts: 1,
        LaunchProbeDelay: TimeSpan.Zero);

    public static OllamaReachabilityOptions ForResume { get; } = new(
        InitialProbeAttempts: 6,
        InitialProbeDelay: TimeSpan.FromMilliseconds(500),
        LaunchProbeAttempts: 30,
        LaunchProbeDelay: TimeSpan.FromSeconds(1));
}
