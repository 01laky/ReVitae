namespace ReVitae.Core.Cv.Projects;

public sealed class ProjectTechnologyItem
{
	public ProjectTechnologyItem()
	{
		Id = Guid.NewGuid().ToString("N");
	}

	public ProjectTechnologyItem(string id)
	{
		Id = id;
	}

	public string Id { get; }

	public string Name { get; set; } = string.Empty;

	public bool HasUserInput() => !string.IsNullOrWhiteSpace(Name);

	public ProjectTechnologyItem Duplicate()
	{
		return new ProjectTechnologyItem { Name = Name };
	}
}
