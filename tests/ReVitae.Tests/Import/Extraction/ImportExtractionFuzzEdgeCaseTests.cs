using ReVitae.Core.Import;

namespace ReVitae.Tests.Import.Extraction;

[Trait("Category", "ImportExtractionFuzz")]
public sealed class ImportExtractionFuzzEdgeCaseTests
{
	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	[InlineData(4)]
	[InlineData(5)]
	[InlineData(6)]
	[InlineData(7)]
	[InlineData(8)]
	[InlineData(9)]
	public void Extract_SeededRandomText_DoesNotCrash(int caseId)
	{
		var random = new Random(42 + caseId);
		var text = GenerateNoisyCvText(random);

		var segmentation = CvSectionSegmenter.Segment(text);
		var result = CvImportFieldExtractor.Extract(segmentation);

		Assert.NotNull(result);
		Assert.True(result.WorkExperienceEntries.Count <= 50);
		Assert.True(result.EducationEntries.Count <= 50);
		Assert.True(result.SkillsGroups.Count <= 50);
	}

	[Theory]
	[InlineData(10)]
	[InlineData(11)]
	public void Extract_OutOfOrderHeaders_DoesNotBleedSections(int caseId)
	{
		var random = new Random(42 + caseId);
		var text = GenerateOutOfOrderHeaders(random);
		var result = CvImportFieldExtractor.Extract(CvSectionSegmenter.Segment(text));

		foreach (var skill in result.SkillsGroups.SelectMany(g => g.Skills))
		{
			Assert.DoesNotContain("Senior Engineer", skill.Name, StringComparison.OrdinalIgnoreCase);
		}
	}

	private static string GenerateNoisyCvText(Random random)
	{
		var sections = new[]
		{
			"Jane Doe",
			"jane@example.com",
			"Skills",
			string.Join(", ", Enumerable.Range(0, random.Next(3, 8)).Select(_ => RandomToken(random))),
			"Work Experience",
			$"{random.Next(2010, 2024)} - Present",
			"Engineer",
			"Acme",
			"Education",
			"2016 - 2020",
			"BSc Informatics",
		};

		return string.Join(
			random.Next(2) == 0 ? "\n" : "\n\n",
			sections.Select(line => MaybeHyphenate(line, random)));
	}

	private static string GenerateOutOfOrderHeaders(Random random)
	{
		var blocks = new List<string>
		{
			"Jane Doe",
			"Education",
			"2018\nUniversity",
			"Skills",
			"C#, Docker",
			"Work Experience",
			"2020\nSenior Engineer\nAcme",
		};

		if (random.Next(2) == 0)
		{
			blocks.Reverse();
		}

		return string.Join("\n\n", blocks);
	}

	private static string RandomToken(Random random)
	{
		var tokens = new[] { "C#", "Docker", "PostgreSQL", "Avalonia", "Linux", "Azure" };
		return tokens[random.Next(tokens.Length)];
	}

	private static string MaybeHyphenate(string line, Random random)
	{
		if (line.Length < 8 || random.Next(4) != 0)
		{
			return line;
		}

		var split = random.Next(2, line.Length - 2);
		return line[..split] + "-\n" + line[split..];
	}
}
