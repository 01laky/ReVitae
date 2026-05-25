using ReVitae.Core.Import;
using ReVitae.Core.Import.Structured;
using ReVitae.Core.Localization;

namespace ReVitae.Tests.Import.Structured;

public sealed class JsonResumeMapperEdgeCaseTests
{
	[Fact]
	public void Map_ReturnsUnreadableDocument_OnInvalidJson()
	{
		var result = JsonResumeMapper.Map("{");

		Assert.False(result.Success);
		Assert.Equal(TranslationKeys.ImportErrorUnreadableDocument, result.ErrorMessageKey);
	}

	[Fact]
	public void Map_ReturnsNoStructuredData_OnEmptyObject()
	{
		var result = JsonResumeMapper.Map("{}");

		Assert.False(result.Success);
		Assert.Equal(TranslationKeys.ImportErrorNoStructuredData, result.ErrorMessageKey);
	}

	[Fact]
	public void Map_AcceptsTrailingCommentsAndWhitespace()
	{
		var json = """
            {
              // profile
              "basics": { "email": "jane@example.com" },
            }
            """;

		var result = JsonResumeMapper.Map(json);

		Assert.True(result.Success);
		Assert.Equal("jane@example.com", result.Personal.Email);
	}

	[Fact]
	public void Map_MapsNestedProfilesIntoPersonalUrls()
	{
		var json = """
            {
              "basics": {
                "name": "Jane Doe",
                "profiles": [
                  { "network": "LinkedIn", "url": "https://linkedin.com/in/jane" },
                  { "network": "GitHub", "url": "https://github.com/jane" }
                ]
              }
            }
            """;

		var result = JsonResumeMapper.Map(json);

		Assert.True(result.Success);
		Assert.Equal("https://linkedin.com/in/jane", result.Personal.LinkedInUrl);
		Assert.Equal("https://github.com/jane", result.Personal.GitHubUrl);
	}
}
