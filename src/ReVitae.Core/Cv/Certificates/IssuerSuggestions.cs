namespace ReVitae.Core.Cv.Certificates;

public static class IssuerSuggestions
{
	public static IReadOnlyList<string> All { get; } =
	[
		"Amazon Web Services",
		"Microsoft",
		"Google",
		"Cisco",
		"CompTIA",
		"Scrum Alliance",
		"Project Management Institute",
		"ISACA",
		"(ISC)²",
		"HashiCorp",
		"Linux Foundation",
		"Oracle",
		"SAP",
		"Salesforce",
		"Adobe",
		"British Council",
		"Goethe-Institut",
		"Instituto Cervantes",
		"Coursera",
		"Udacity"
	];

	public static IReadOnlyList<string> Filter(string? query, int maxResults = 12)
	{
		if (string.IsNullOrWhiteSpace(query))
		{
			return All.Take(maxResults).ToArray();
		}

		return All
			.Where(issuer => issuer.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase))
			.Take(maxResults)
			.ToArray();
	}
}
