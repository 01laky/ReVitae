using System.Text.Json.Nodes;
using ReVitae.Core.Ai.Import;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Ai.Import;

public sealed class AiCvImportResponseParserTests
{
    [Fact]
    public void TryParse_ValidJson_Succeeds()
    {
        var raw = """{"personalInformation":{"firstName":"John","lastName":"Doe"}}""";
        var result = AiCvImportResponseParser.TryParse(raw, AiImportPhase.Personal);
        Assert.True(result.Success);
        Assert.NotNull(result.Fragment);
    }

    [Fact]
    public void TryParse_FencedMarkdownJson_Succeeds()
    {
        var raw = """
            ```json
            {"personalInformation":{"firstName":"Jane"}}
            ```
            """;
        var result = AiCvImportResponseParser.TryParse(raw, AiImportPhase.Personal);
        Assert.True(result.Success);
    }

    [Fact]
    public void TryParse_InvalidJson_SetsRetryFlag()
    {
        var result = AiCvImportResponseParser.TryParse("{not json", AiImportPhase.Personal);
        Assert.False(result.Success);
        Assert.True(result.ShouldRetry);
    }

    [Fact]
    public void TryParse_ExtraUnknownKeys_IgnoredAndSucceeds()
    {
        var raw = """{"personalInformation":{"firstName":"John"},"unknownField":123}""";
        var result = AiCvImportResponseParser.TryParse(raw, AiImportPhase.Personal);
        Assert.True(result.Success);
    }

    [Fact]
    public void TryParse_ProfilePhotoBase64_StrippedBeforeMerge()
    {
        var raw = """
            {"personalInformation":{"firstName":"John","profilePhotoBase64":"abc123"}}
            """;
        var result = AiCvImportResponseParser.TryParse(raw, AiImportPhase.Personal);
        Assert.True(result.Success);
        var personal = result.Fragment!["personalInformation"] as JsonObject;
        Assert.NotNull(personal);
        Assert.False(personal.ContainsKey("profilePhotoBase64"));
    }

    [Fact]
    public void MapAccumulated_FullDocument_ReturnsCvImportResult()
    {
        var accumulated = AiImportTestHelpers.PersonalJson();
        AiCvImportResultMerger.MergeFragment(accumulated, AiImportTestHelpers.WorkJson(("Acme", 2020)));
        var mapped = AiCvImportResponseParser.MapAccumulated(accumulated, []);
        Assert.True(mapped.Success);
        Assert.Single(mapped.WorkExperienceEntries);
    }
}
