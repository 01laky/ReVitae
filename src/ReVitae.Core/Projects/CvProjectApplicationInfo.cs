using System.Reflection;

namespace ReVitae.Core.Projects;

public static class CvProjectApplicationInfo
{
    public static string Version =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.1.0";
}
