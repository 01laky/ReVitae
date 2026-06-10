using System.Text.Json;
using ReVitae.Core.Cv.Education;
using ReVitae.Core.Cv.WorkExperience;
using ReVitae.Core.Export;
using ReVitae.Core.Export.Fixtures;
using ReVitae.Core.Import;
using ReVitae.Core.Projects;
using WorkExperienceEntry = ReVitae.Core.Cv.WorkExperience.WorkExperienceEntry;
using EducationEntry = ReVitae.Core.Cv.Education.EducationEntry;

namespace ReVitae.Tests.Projects;

public sealed class CvProjectSerializerTests : IDisposable
{
	private readonly string _root;

	public CvProjectSerializerTests()
	{
		_root = Path.Combine(Path.GetTempPath(), "revitae-project-tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_root);
	}

	[Fact]
	public void SaveLoad_RoundTripsCvContentAndSettings()
	{
		var path = Path.Combine(_root, "Jane_Doe_CV.revitae.json");
		var source = CreateSampleSource();
		var settings = new CvProjectSettings(
			CvProjectConstants.CurrentProjectSettingsSchemaVersion,
			CvExportTemplateId.ModernSidebar,
			["work.generic-description|entry-1|description"],
			new Dictionary<string, bool>(StringComparer.Ordinal)
			{
				["personalInformation"] = true,
				["workExperience"] = true,
				["education"] = false
			},
			DateTimeOffset.UtcNow,
			"0.1.0");

		CvProjectSerializer.Save(path, new CvProjectSaveRequest(source, settings));
		var loaded = CvProjectSerializer.Load(path);

		Assert.True(loaded.Success);
		Assert.NotNull(loaded.Import);
		Assert.Equal("John", loaded.Import!.Personal.FirstName);
		Assert.Equal("Doe", loaded.Import.Personal.LastName);
		Assert.Single(loaded.Import.WorkExperienceEntries);
		Assert.Equal(CvExportTemplateId.ModernSidebar, loaded.Settings!.SelectedTemplateId);
		Assert.Equal(["work.generic-description|entry-1|description"], loaded.Settings.DismissedQualityHintKeys);
		Assert.False(loaded.Settings.SectionExpandState!["education"]);
	}

	[Fact]
	public void Load_InterchangeFileWithoutSettings_SucceedsWithNullSettings()
	{
		var path = Path.Combine(_root, "interchange.revitae.json");
		File.WriteAllText(path, """
            {
              "revitaeVersion": 1,
              "personalInformation": {
                "firstName": "Jane",
                "lastName": "Roe",
                "email": "jane@example.com"
              }
            }
            """);

		var loaded = CvProjectSerializer.Load(path);

		Assert.True(loaded.Success);
		Assert.NotNull(loaded.Import);
		Assert.Equal("Jane", loaded.Import!.Personal.FirstName);
		Assert.Null(loaded.Settings);
		Assert.False(loaded.SettingsPartiallyIgnored);
	}

	[Fact]
	public void Load_UnknownTemplateId_FallsBackToDefaultTemplate()
	{
		var path = Path.Combine(_root, "unknown-template.revitae.json");
		var source = CreateSampleSource();
		CvProjectSerializer.Save(path, new CvProjectSaveRequest(
			source,
			CvProjectSettings.CreateDefault(CvExportTemplateId.ClassicSidebar)));

		var json = File.ReadAllText(path).Replace(
			"\"classicSidebar\"",
			"\"futureTemplate\"",
			StringComparison.Ordinal);
		File.WriteAllText(path, json);

		var loaded = CvProjectSerializer.Load(path);

		Assert.True(loaded.Success);
		Assert.Equal(CvExportTemplateId.CleanTopHeader, loaded.Settings!.SelectedTemplateId);
	}

	[Fact]
	public void Load_NewerSchemaVersion_SetsPartiallyIgnoredFlag()
	{
		var path = Path.Combine(_root, "future-settings.revitae.json");
		var source = CreateSampleSource();
		CvProjectSerializer.Save(path, new CvProjectSaveRequest(
			source,
			CvProjectSettings.CreateDefault(CvExportTemplateId.CleanTopHeader)));

		var json = File.ReadAllText(path).Replace(
			"\"schemaVersion\": 1",
			"\"schemaVersion\": 99",
			StringComparison.Ordinal);
		File.WriteAllText(path, json);

		var loaded = CvProjectSerializer.Load(path);

		Assert.True(loaded.Success);
		Assert.True(loaded.SettingsPartiallyIgnored);
	}

	[Fact]
	public void Load_MalformedProjectSettings_LoadsCvAndMarksMalformed()
	{
		var path = Path.Combine(_root, "malformed-settings.revitae.json");
		File.WriteAllText(path, """
            {
              "revitaeVersion": 1,
              "personalInformation": { "firstName": "Malformed" },
              "projectSettings": []
            }
            """);

		var loaded = CvProjectSerializer.Load(path);

		Assert.True(loaded.Success);
		Assert.Null(loaded.Settings);
		Assert.True(loaded.SettingsMalformed);
	}

	[Fact]
	public void Load_MissingFile_ReturnsFailure()
	{
		var loaded = CvProjectSerializer.Load(Path.Combine(_root, "missing.revitae.json"));

		Assert.False(loaded.Success);
		Assert.NotNull(loaded.ErrorMessageKey);
	}

	[Fact]
	public void Load_OversizedFile_ReturnsFailure()
	{
		var path = Path.Combine(_root, "oversized.revitae.json");
		using (var stream = File.Create(path))
		{
			stream.SetLength(CvImportLimits.MaxFileBytes + 1);
		}

		var loaded = CvProjectSerializer.Load(path);

		Assert.False(loaded.Success);
	}

	[Fact]
	public void Save_WritesValidJsonWithProjectSettingsBlock()
	{
		var path = Path.Combine(_root, "valid.revitae.json");
		CvProjectSerializer.Save(path, new CvProjectSaveRequest(
			CreateSampleSource(),
			CvProjectSettings.CreateDefault(CvExportTemplateId.ModernSidebar)));

		using var document = JsonDocument.Parse(File.ReadAllText(path));
		Assert.True(document.RootElement.TryGetProperty("projectSettings", out var settings));
		Assert.Equal("modernSidebar", settings.GetProperty("selectedTemplateId").GetString());
		Assert.Equal(1, settings.GetProperty("schemaVersion").GetInt32());
	}

	[Fact]
	public void SaveLoad_PreservesEmptySectionsWhenPresentInSource()
	{
		var path = Path.Combine(_root, "minimal.revitae.json");
		var source = CvExportSourceDataFactory.Create(
			new PersonalInformationImport { FirstName = "Ada", LastName = "Lovelace" },
			[],
			[],
			[],
			[],
			[],
			[],
			[],
			null);

		CvProjectSerializer.Save(path, new CvProjectSaveRequest(
			source,
			CvProjectSettings.CreateDefault(CvExportTemplateId.CleanTopHeader)));
		var loaded = CvProjectSerializer.Load(path);

		Assert.True(loaded.Success);
		Assert.Equal("Ada", loaded.Import!.Personal.FirstName);
	}

	[Fact]
	public void SaveLoad_JohnDoeMinimalArchitectDatasetRoundTrip()
	{
		var document = JohnDoeMinimalArchitectCvDataset.CreateDocument();
		var source = CvExportSourceDataFactory.Create(
			new PersonalInformationImport
			{
				FirstName = document.FirstName,
				LastName = document.LastName,
				ProfessionalTitle = document.ProfessionalTitle,
				Email = document.Email,
				Phone = document.Phone,
				Location = document.Location,
				LinkedInUrl = document.LinkedInUrl,
				PortfolioUrl = document.PortfolioUrl,
				GitHubUrl = document.GitHubUrl,
				ShortSummary = document.ShortSummary ?? string.Empty
			},
			document.WorkExperienceEntries.Select(entry => new WorkExperienceEntry
			{
				JobTitle = entry.JobTitle,
				Company = entry.Company,
				Description = entry.Description ?? string.Empty
			}),
			document.EducationEntries.Select(entry => new EducationEntry
			{
				Institution = entry.Institution,
				Degree = entry.Degree
			}),
			[],
			[],
			[],
			[],
			[],
			document.AdditionalInformationContent);

		var path = Path.Combine(_root, "john-doe.revitae.json");
		CvProjectSerializer.Save(path, new CvProjectSaveRequest(
			source,
			CvProjectSettings.CreateDefault(CvExportTemplateId.ModernSidebar)));
		var loaded = CvProjectSerializer.Load(path);

		Assert.True(loaded.Success);
		Assert.Equal(document.WorkExperienceEntries.Count, loaded.Import!.WorkExperienceEntries.Count);
		Assert.Equal(document.EducationEntries.Count, loaded.Import.EducationEntries.Count);
	}

	private static CvExportSourceData CreateSampleSource() =>
		CvExportSourceDataFactory.Create(
			new PersonalInformationImport
			{
				FirstName = "John",
				LastName = "Doe",
				Email = "john.doe@example.com"
			},
			[
				new WorkExperienceEntry
				{
					JobTitle = "Engineer",
					Company = "Acme",
					Description = "Built APIs with measurable impact."
				}
			],
			[
				new EducationEntry
				{
					Institution = "Example University",
					Degree = "BSc"
				}
			],
			[],
			[],
			[],
			[],
			[],
			"Additional notes.");

	public void Dispose()
	{
		try
		{
			if (Directory.Exists(_root))
			{
				Directory.Delete(_root, recursive: true);
			}
		}
		catch
		{
			// Temp cleanup best effort.
		}
	}
}
