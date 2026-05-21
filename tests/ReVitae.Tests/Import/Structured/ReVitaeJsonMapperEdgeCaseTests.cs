using ReVitae.Core.Import;
using ReVitae.Core.Import.Structured;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Import.Structured;

public sealed class ReVitaeJsonMapperEdgeCaseTests
{
    [Fact]
    public void Map_ReturnsUnreadableDocument_OnInvalidJson()
    {
        var result = ReVitaeJsonMapper.Map("{");

        Assert.False(result.Success);
        Assert.Equal(TranslationKeys.ImportErrorUnreadableDocument, result.ErrorMessageKey);
    }

    [Fact]
    public void Map_ReturnsUnsupportedStructuredFormat_OnWrongRevision()
    {
        var json = """
            {
              "revitaeVersion": 999,
              "personalInformation": { "email": "jane@example.com" }
            }
            """;

        var result = ReVitaeJsonMapper.Map(json);

        Assert.False(result.Success);
        Assert.Equal(TranslationKeys.ImportErrorUnsupportedStructuredFormat, result.ErrorMessageKey);
    }

    [Fact]
    public void Map_ReturnsNoStructuredData_WhenDocumentHasOnlyEmptySections()
    {
        var json = """
            {
              "revitaeVersion": 1,
              "personalInformation": {},
              "workExperience": [],
              "education": [],
              "skills": [],
              "languages": [],
              "certificates": [],
              "projects": [],
              "links": []
            }
            """;

        var result = ReVitaeJsonMapper.Map(json);

        Assert.False(result.Success);
        Assert.Equal(TranslationKeys.ImportErrorNoStructuredData, result.ErrorMessageKey);
    }

    [Fact]
    public void Map_HappyPath_LoadsPersonalInformationAndSkills()
    {
        var json = """
            {
              "revitaeVersion": 1,
              "personalInformation": {
                "firstName": "Jane",
                "lastName": "Doe",
                "email": "jane@example.com"
              },
              "skills": [
                {
                  "category": "Languages",
                  "skills": [ { "name": "C#" }, { "name": "Go" } ]
                }
              ]
            }
            """;

        var result = ReVitaeJsonMapper.Map(json);

        Assert.True(result.Success);
        Assert.Equal("Jane", result.Personal.FirstName);
        Assert.Equal("jane@example.com", result.Personal.Email);
        Assert.Single(result.SkillsGroups);
        Assert.Equal(2, result.SkillsGroups[0].Skills.Count);
    }
}
