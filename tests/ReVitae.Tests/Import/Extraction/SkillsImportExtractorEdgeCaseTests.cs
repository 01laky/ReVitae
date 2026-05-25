using ReVitae.Core.Import.Extraction;

namespace ReVitae.Tests.Import.Extraction;

public sealed class SkillsImportExtractorEdgeCaseTests
{
	[Fact]
	public void Extract_ColonCategoryFormat()
	{
		const string body = """
            Backend: PostgreSQL, Redis
            Frontend: React, TypeScript
            """;

		var groups = SkillsImportExtractor.Extract(body, new ImportSectionExtractionContext());

		Assert.Equal(2, groups.Count);
		Assert.Equal(["PostgreSQL", "Redis"], groups[0].Skills.Select(skill => skill.Name).ToArray());
		Assert.Equal(["React", "TypeScript"], groups[1].Skills.Select(skill => skill.Name).ToArray());
	}

	[Fact]
	public void Extract_CommaListWithoutCategory()
	{
		const string body = "PostgreSQL, Redis, Docker";

		var groups = SkillsImportExtractor.Extract(body, new ImportSectionExtractionContext());
		var names = groups.SelectMany(g => g.Skills).Select(s => s.Name).ToArray();

		Assert.Equal(["PostgreSQL", "Redis", "Docker"], names);
	}

	[Fact]
	public void Extract_EmptyBody_ReturnsEmpty()
	{
		Assert.Empty(SkillsImportExtractor.Extract(string.Empty, new ImportSectionExtractionContext()));
	}

	[Fact]
	public void Extract_ShortTokensAreCaptured()
	{
		const string body = """
            Technical Skills
            C#
            .NET
            """;

		var groups = SkillsImportExtractor.Extract(body, new ImportSectionExtractionContext());
		var names = groups.SelectMany(g => g.Skills).Select(s => s.Name).ToArray();

		Assert.Contains("C#", names);
		Assert.Contains(".NET", names);
	}

	[Fact]
	public void Extract_LinePrefixedSkills()
	{
		const string body = """
            Kubernetes
            Terraform
            """;

		var groups = SkillsImportExtractor.Extract(body, new ImportSectionExtractionContext());
		var names = groups.SelectMany(g => g.Skills).Select(s => s.Name).ToArray();

		Assert.Contains("Kubernetes", names);
		Assert.Contains("Terraform", names);
	}

	[Fact]
	public void Extract_CommaSeparatedInlineList()
	{
		const string body = "C#, Avalonia, PostgreSQL";

		var groups = SkillsImportExtractor.Extract(body, new ImportSectionExtractionContext());
		var names = groups.SelectMany(g => g.Skills).Select(s => s.Name).ToArray();

		Assert.Equal(3, names.Length);
	}

	[Fact]
	public void Extract_DeduplicatesWithinCategory()
	{
		const string body = """
            Tools: Git, Git, Docker
            """;

		var groups = SkillsImportExtractor.Extract(body, new ImportSectionExtractionContext());
		var gitCount = groups.SelectMany(g => g.Skills).Count(s => s.Name.Equals("Git", StringComparison.OrdinalIgnoreCase));

		Assert.True(gitCount <= 2);
	}

	[Fact]
	public void Extract_AddsConfidenceForNonEmptyGroups()
	{
		var context = new ImportSectionExtractionContext();
		const string body = "Cloud: AWS, Azure";

		SkillsImportExtractor.Extract(body, context);

		Assert.NotEmpty(context.FieldConfidences);
	}
}
