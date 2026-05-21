using ReVitae.Core.Import.Structured;

namespace ReVitae.Tests.Import.Structured;

public sealed class CvStructuredImportMapperEdgeCaseTests
{
    [Fact]
    public void AddUniqueHref_SkipsJavascriptUrls()
    {
        var store = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        CvStructuredImportMapper.AddUniqueHref(store, seen, "javascript:alert(1)");
        CvStructuredImportMapper.AddUniqueHref(store, seen, "https://example.com/profile");

        Assert.Single(store);
        Assert.Equal("https://example.com/profile", store[0]);
    }

    [Fact]
    public void AddUniqueHref_DeduplicatesCaseInsensitive()
    {
        var store = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        CvStructuredImportMapper.AddUniqueHref(store, seen, "https://Example.com/a");
        CvStructuredImportMapper.AddUniqueHref(store, seen, "https://example.com/a");

        Assert.Single(store);
    }
}
