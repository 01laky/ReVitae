namespace ReVitae.Core.Ai.Ollama;

public static class OllamaHost
{
    private const string DefaultBaseUrl = "http://127.0.0.1:11434";

    public static Uri BaseUri { get; } = ResolveBaseUri();

    public static Uri TagsUri { get; } = new(BaseUri, "/api/tags");

    public static Uri PullUri { get; } = new(BaseUri, "/api/pull");

    public static Uri DeleteUri { get; } = new(BaseUri, "/api/delete");

    public static Uri ChatUri { get; } = new(BaseUri, "/api/chat");

    public static string DisplayAddress =>
        $"{BaseUri.Host}:{BaseUri.Port}";

    internal static Uri ResolveBaseUri()
    {
        var host = Environment.GetEnvironmentVariable("OLLAMA_HOST");
        if (string.IsNullOrWhiteSpace(host))
        {
            return new Uri(DefaultBaseUrl);
        }

        if (!host.Contains("://", StringComparison.Ordinal))
        {
            host = $"http://{host}";
        }

        return Uri.TryCreate(host, UriKind.Absolute, out var uri)
            ? uri
            : new Uri(DefaultBaseUrl);
    }
}
