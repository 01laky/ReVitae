using ReVitae.Core.Cv.WorkExperience;

namespace ReVitae.Core.Cv.Skills;

public static class SkillsDeduplication
{
    public static IReadOnlyList<SkillsGroupEntry> DeduplicateAcrossGroups(IReadOnlyList<SkillsGroupEntry> groups)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<SkillsGroupEntry>();

        foreach (var group in groups)
        {
            var clone = new SkillsGroupEntry(group.Id)
            {
                Category = group.Category
            };

            foreach (var skill in group.Skills)
            {
                if (!skill.HasUserInput())
                {
                    continue;
                }

                var normalized = skill.Name.Trim();
                if (!seen.Add(normalized))
                {
                    continue;
                }

                clone.Skills.Add(CopySkill(skill));
            }

            result.Add(clone);
        }

        return result;
    }

    public static IReadOnlyList<SkillsGroupEntry> ExcludeWorkExperienceTechnologies(
        IReadOnlyList<SkillsGroupEntry> groups,
        IReadOnlyList<WorkExperienceEntry> workExperienceEntries)
    {
        var technologyNames = CollectWorkExperienceTechnologyNames(workExperienceEntries);
        if (technologyNames.Count == 0)
        {
            return groups;
        }

        var result = new List<SkillsGroupEntry>();
        foreach (var group in groups)
        {
            var clone = new SkillsGroupEntry(group.Id)
            {
                Category = group.Category
            };

            foreach (var skill in group.Skills)
            {
                if (!skill.HasUserInput())
                {
                    continue;
                }

                if (technologyNames.Contains(skill.Name.Trim()))
                {
                    continue;
                }

                clone.Skills.Add(CopySkill(skill));
            }

            result.Add(clone);
        }

        return result;
    }

    public static IReadOnlyList<SkillsGroupEntry> PrepareForPreview(
        IReadOnlyList<SkillsGroupEntry> groups,
        IReadOnlyList<WorkExperienceEntry> workExperienceEntries)
    {
        var deduplicated = DeduplicateAcrossGroups(groups);
        return ExcludeWorkExperienceTechnologies(deduplicated, workExperienceEntries);
    }

    private static HashSet<string> CollectWorkExperienceTechnologyNames(IReadOnlyList<WorkExperienceEntry> entries)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Technologies))
            {
                continue;
            }

            foreach (var name in SkillsTextParser.ParseSkillNames(entry.Technologies))
            {
                names.Add(name);
            }
        }

        return names;
    }

    private static SkillItem CopySkill(SkillItem skill)
    {
        return new SkillItem(skill.Id)
        {
            Name = skill.Name,
            Proficiency = skill.Proficiency,
            YearsOfExperience = skill.YearsOfExperience
        };
    }
}
