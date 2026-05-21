namespace ReVitae.Core.Cv.Skills;

public sealed class SkillItem
{
    public SkillItem()
    {
        Id = Guid.NewGuid().ToString("N");
    }

    public SkillItem(string id)
    {
        Id = id;
    }

    public string Id { get; }

    public string Name { get; set; } = string.Empty;

    public ProficiencyLevel Proficiency { get; set; } = ProficiencyLevel.Intermediate;

    public int? YearsOfExperience { get; set; }

    public bool HasUserInput() => !string.IsNullOrWhiteSpace(Name);

    public SkillItem Duplicate()
    {
        return new SkillItem
        {
            Name = Name,
            Proficiency = Proficiency,
            YearsOfExperience = YearsOfExperience
        };
    }
}
