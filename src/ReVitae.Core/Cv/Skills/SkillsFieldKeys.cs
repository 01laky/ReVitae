namespace ReVitae.Core.Cv.Skills;

public static class SkillsFieldKeys
{
    public const string Prefix = "skills";

    public const string Category = "category";
    public const string SkillsCollection = "skills";
    public const string SkillName = "name";
    public const string SkillProficiency = "proficiency";
    public const string SkillYearsOfExperience = "yearsOfExperience";

    public static string BuildGroup(string groupId, string fieldName)
    {
        return $"{Prefix}.{groupId}.{fieldName}";
    }

    public static string BuildSkill(string groupId, string skillId, string fieldName)
    {
        return $"{Prefix}.{groupId}.{skillId}.{fieldName}";
    }

    public static bool TryParseGroupId(string fieldKey, out string groupId, out string remainder)
    {
        groupId = string.Empty;
        remainder = string.Empty;

        if (!fieldKey.StartsWith(Prefix + ".", StringComparison.Ordinal))
        {
            return false;
        }

        var afterPrefix = fieldKey[(Prefix.Length + 1)..];
        var separatorIndex = afterPrefix.IndexOf('.', StringComparison.Ordinal);
        if (separatorIndex <= 0)
        {
            return false;
        }

        groupId = afterPrefix[..separatorIndex];
        remainder = afterPrefix[(separatorIndex + 1)..];
        return true;
    }

    public static bool TryParseSkillField(string fieldKey, out string groupId, out string skillId, out string fieldName)
    {
        groupId = string.Empty;
        skillId = string.Empty;
        fieldName = string.Empty;

        if (!TryParseGroupId(fieldKey, out groupId, out var remainder))
        {
            return false;
        }

        var separatorIndex = remainder.IndexOf('.', StringComparison.Ordinal);
        if (separatorIndex <= 0 || separatorIndex >= remainder.Length - 1)
        {
            fieldName = remainder;
            return fieldName != Category && fieldName != SkillsCollection;
        }

        skillId = remainder[..separatorIndex];
        fieldName = remainder[(separatorIndex + 1)..];
        return true;
    }
}
