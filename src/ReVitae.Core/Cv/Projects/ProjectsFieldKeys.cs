namespace ReVitae.Core.Cv.Projects;

public static class ProjectsFieldKeys
{
    public const string Prefix = "projects";

    public const string Name = "name";
    public const string Role = "role";
    public const string Organization = "organization";
    public const string StartMonth = "startMonth";
    public const string StartYear = "startYear";
    public const string EndMonth = "endMonth";
    public const string EndYear = "endYear";
    public const string IsCurrentlyActive = "isCurrentlyActive";
    public const string ProjectUrl = "projectUrl";
    public const string Highlights = "highlights";
    public const string Description = "description";
    public const string BulkTechnologies = "bulkTechnologies";
    public const string TechnologiesCollection = "technologies";
    public const string TechnologyName = "technologyName";
    public const string DateRange = "dateRange";

    public static string Build(string entryId, string fieldName)
    {
        return $"{Prefix}.{entryId}.{fieldName}";
    }

    public static string BuildTechnology(string entryId, string technologyId, string fieldName)
    {
        return $"{Prefix}.{entryId}.{technologyId}.{fieldName}";
    }

    public static bool TryParseEntryId(string fieldKey, out string entryId, out string remainder)
    {
        entryId = string.Empty;
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

        entryId = afterPrefix[..separatorIndex];
        remainder = afterPrefix[(separatorIndex + 1)..];
        return true;
    }

    public static bool TryParseTechnologyField(
        string fieldKey,
        out string entryId,
        out string technologyId,
        out string fieldName)
    {
        entryId = string.Empty;
        technologyId = string.Empty;
        fieldName = string.Empty;

        if (!TryParseEntryId(fieldKey, out entryId, out var remainder))
        {
            return false;
        }

        var separatorIndex = remainder.IndexOf('.', StringComparison.Ordinal);
        if (separatorIndex <= 0 || separatorIndex >= remainder.Length - 1)
        {
            fieldName = remainder;
            return fieldName != Name
                && fieldName != Role
                && fieldName != Organization
                && fieldName != BulkTechnologies
                && fieldName != TechnologiesCollection
                && fieldName != DateRange
                && fieldName != TechnologyName;
        }

        technologyId = remainder[..separatorIndex];
        fieldName = remainder[(separatorIndex + 1)..];
        return true;
    }
}
