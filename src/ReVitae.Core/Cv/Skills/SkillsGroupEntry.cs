namespace ReVitae.Core.Cv.Skills;

public sealed class SkillsGroupEntry
{
	public SkillsGroupEntry()
	{
		Id = Guid.NewGuid().ToString("N");
	}

	public SkillsGroupEntry(string id)
	{
		Id = id;
	}

	public string Id { get; }

	public string Category { get; set; } = string.Empty;

	public List<SkillItem> Skills { get; } = [];

	public bool HasUserInput()
	{
		if (!string.IsNullOrWhiteSpace(Category))
		{
			return true;
		}

		return Skills.Any(skill => skill.HasUserInput());
	}

	public SkillsGroupEntry Duplicate()
	{
		var duplicate = new SkillsGroupEntry
		{
			Category = Category
		};

		foreach (var skill in Skills)
		{
			duplicate.Skills.Add(skill.Duplicate());
		}

		return duplicate;
	}

	public IReadOnlyDictionary<string, string?> ToFieldValues()
	{
		var values = new Dictionary<string, string?>(StringComparer.Ordinal)
		{
			[SkillsFieldKeys.BuildGroup(Id, SkillsFieldKeys.Category)] = Category
		};

		foreach (var skill in Skills)
		{
			values[SkillsFieldKeys.BuildSkill(Id, skill.Id, SkillsFieldKeys.SkillName)] = skill.Name;
			values[SkillsFieldKeys.BuildSkill(Id, skill.Id, SkillsFieldKeys.SkillProficiency)] = skill.Proficiency.ToString();
			values[SkillsFieldKeys.BuildSkill(Id, skill.Id, SkillsFieldKeys.SkillYearsOfExperience)] =
				skill.YearsOfExperience?.ToString(System.Globalization.CultureInfo.InvariantCulture);
		}

		return values;
	}

	public string BuildHeaderSummary()
	{
		var category = string.IsNullOrWhiteSpace(Category) ? "-" : Category.Trim();
		var skillLabels = Skills
			.Where(skill => skill.HasUserInput())
			.Select(skill => skill.Name.Trim())
			.Take(3)
			.ToArray();

		if (skillLabels.Length == 0)
		{
			return category;
		}

		var preview = string.Join(", ", skillLabels);
		if (Skills.Count(skill => skill.HasUserInput()) > 3)
		{
			preview += "…";
		}

		return $"{category} · {preview}";
	}
}
