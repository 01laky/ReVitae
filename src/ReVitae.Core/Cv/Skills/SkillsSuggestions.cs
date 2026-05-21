namespace ReVitae.Core.Cv.Skills;

public static class SkillsSuggestions
{
    public static IReadOnlyList<string> All { get; } =
    [
        "C#",
        "TypeScript",
        "JavaScript",
        "Python",
        "Java",
        "Go",
        "Rust",
        "SQL",
        "HTML",
        "CSS",
        "React",
        "Vue.js",
        "Angular",
        "Avalonia",
        "WPF",
        ".NET",
        "ASP.NET Core",
        "Node.js",
        "Docker",
        "Kubernetes",
        "Azure",
        "AWS",
        "Git",
        "CI/CD",
        "Linux",
        "PostgreSQL",
        "MongoDB",
        "Redis",
        "REST APIs",
        "GraphQL",
        "Unit Testing",
        "Agile",
        "Scrum",
        "Communication",
        "Leadership",
        "Problem Solving",
        "Team Collaboration",
        "Project Management",
        "Figma",
        "UI/UX Design"
    ];

    public static IReadOnlyList<string> Filter(string? query, int maxResults = 12)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return All.Take(maxResults).ToArray();
        }

        return All
            .Where(skill => skill.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase))
            .Take(maxResults)
            .ToArray();
    }
}
