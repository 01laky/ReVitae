using System.Text.Json;
using ReVitae.Core.Export;

namespace ReVitae.Core.Projects;

public static class CvExportTemplateIdJson
{
    public static string ToJsonId(CvExportTemplateId templateId) =>
        JsonNamingPolicy.CamelCase.ConvertName(templateId.ToString());

    public static CvExportTemplateId ParseOrDefault(string? value, out bool recognized)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            recognized = false;
            return CvExportTemplateId.CleanTopHeader;
        }

        foreach (var templateId in Enum.GetValues<CvExportTemplateId>())
        {
            if (string.Equals(ToJsonId(templateId), value, StringComparison.OrdinalIgnoreCase)
                || string.Equals(templateId.ToString(), value, StringComparison.OrdinalIgnoreCase))
            {
                recognized = true;
                return templateId;
            }
        }

        recognized = false;
        return CvExportTemplateId.CleanTopHeader;
    }
}
