using ReVitae.Core.Cv.ProfilePhoto;
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

	[Fact]
	public void Map_VersionTwo_WithInvalidBase64_SkipsPhotoButImportsText()
	{
		var json = """
            {
              "revitaeVersion": 2,
              "personalInformation": {
                "firstName": "Jane",
                "lastName": "Doe",
                "email": "jane@example.com",
                "profilePhotoBase64": "%%%not-base64%%%"
              }
            }
            """;

		var result = ReVitaeJsonMapper.Map(json);

		Assert.True(result.Success);
		Assert.Equal("Jane", result.Personal.FirstName);
		Assert.False(ProfilePhotoStorage.FileExists(result.Personal.ProfilePhotoPath));
	}

	[Fact]
	public void Map_VersionTwo_WithValidPhoto_WritesPhotoToStorage()
	{
		var tempDirectory = ProfilePhotoTestHelpers.CreateTempDirectory();
		try
		{
			var storage = new ProfilePhotoStorage(tempDirectory);
			var png = ProfilePhotoTestHelpers.WriteMinimalPng(tempDirectory);
			var saved = storage.TrySaveCopy(png);
			Assert.True(saved.Success);
			var bytes = File.ReadAllBytes(saved.StoredPath!);
			var base64 = Convert.ToBase64String(bytes);

			var json = $$"""
                {
                  "revitaeVersion": 2,
                  "personalInformation": {
                    "firstName": "Jane",
                    "profilePhotoBase64": "{{base64}}",
                    "profilePhotoContentType": "image/png"
                  }
                }
                """;

			var result = ReVitaeJsonMapper.Map(json);

			Assert.True(result.Success);
			Assert.True(ProfilePhotoStorage.FileExists(result.Personal.ProfilePhotoPath));
		}
		finally
		{
			if (Directory.Exists(tempDirectory))
			{
				Directory.Delete(tempDirectory, recursive: true);
			}
		}
	}

	[Fact]
	public void Map_VersionOne_DoesNotExpectPhotoFields()
	{
		var json = """
            {
              "revitaeVersion": 1,
              "personalInformation": {
                "firstName": "Jane",
                "email": "jane@example.com"
              }
            }
            """;

		var result = ReVitaeJsonMapper.Map(json);

		Assert.True(result.Success);
		Assert.False(ProfilePhotoStorage.FileExists(result.Personal.ProfilePhotoPath));
	}

	[Fact]
	public void PersonalInformationImport_HasAnyData_IsTrueWhenOnlyPhotoPathExists()
	{
		var tempDirectory = ProfilePhotoTestHelpers.CreateTempDirectory();
		try
		{
			var storage = new ProfilePhotoStorage(tempDirectory);
			var saved = storage.TrySaveCopy(ProfilePhotoTestHelpers.WriteMinimalPng(tempDirectory));
			Assert.True(saved.Success);

			var personal = new PersonalInformationImport { ProfilePhotoPath = saved.StoredPath! };

			Assert.True(personal.HasAnyData());
		}
		finally
		{
			if (Directory.Exists(tempDirectory))
			{
				Directory.Delete(tempDirectory, recursive: true);
			}
		}
	}
}
