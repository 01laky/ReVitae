namespace ReVitae.Core.Cv.Links;

public sealed class LinkEntry
{
    public LinkEntry()
    {
        Id = Guid.NewGuid().ToString("N");
    }

    public LinkEntry(string id)
    {
        Id = id;
    }

    public string Id { get; }

    public string Label { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;

    public bool HasUserInput()
    {
        return HasText(Label) || HasText(Url) || HasText(Note);
    }

    public LinkEntry Duplicate()
    {
        return new LinkEntry
        {
            Label = Label,
            Url = Url,
            Note = Note
        };
    }

    public IReadOnlyDictionary<string, string?> ToFieldValues()
    {
        return new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [LinksFieldKeys.Build(Id, LinksFieldKeys.Label)] = Label,
            [LinksFieldKeys.Build(Id, LinksFieldKeys.Url)] = Url,
            [LinksFieldKeys.Build(Id, LinksFieldKeys.Note)] = Note
        };
    }

    public string BuildHeaderSummary()
    {
        var label = string.IsNullOrWhiteSpace(Label) ? "-" : Label.Trim();
        var url = string.IsNullOrWhiteSpace(Url) ? "-" : Url.Trim();
        return $"{label} · {url}";
    }

    private static bool HasText(string? value) => !string.IsNullOrWhiteSpace(value);
}
