namespace ReVitae.Tests.Import;

internal static class ImportPdfFixtureFactory
{
	public static string FixturesDirectory =>
		Path.Combine(AppContext.BaseDirectory, "Import", "Fixtures", "Pdf");

	public static string GetFixturePath(string fileName) =>
		Path.Combine(FixturesDirectory, fileName);

	public static void EnsureFixturesExist()
	{
		Directory.CreateDirectory(FixturesDirectory);

		WriteIfMissing("sample-cv-en-basic.pdf", EnglishBasicLines);
		WriteIfMissing("sample-cv-sk-basic.pdf", SlovakBasicLines);
		WriteIfMissing("sample-cv-en-messy.pdf", EnglishMessyLines);
	}

	private static void WriteIfMissing(string fileName, IReadOnlyList<string> lines)
	{
		var path = GetFixturePath(fileName);
		if (File.Exists(path))
		{
			return;
		}

		File.WriteAllBytes(path, MinimalPdfWriter.CreateFromLines(lines));
	}

	public static readonly string[] EnglishBasicLines =
	[
		"Jane Doe",
		"Product Designer",
		"jane.doe@example.com",
		"+1 555 123 4567",
		"https://www.linkedin.com/in/janedoe",
		"Summary",
		"Designer focused on product systems and design ops.",
		"Work Experience",
		"Senior Designer at Acme Corp",
		"01/2021 - Present",
		"- Led redesign of onboarding",
		"Technologies: Figma, React",
		"Education",
		"Bachelor of Design",
		"Design Academy",
		"2016 - 2020",
		"Skills",
		"Figma, Sketch, UX Research, Prototyping"
	];

	public static readonly string[] SlovakBasicLines =
	[
		"Peter Novak",
		"Softverovy inzinier",
		"peter.novak@example.com",
		"Profil",
		"Skuseny backend developer so zameranim na .NET.",
		"Pracovne skusenosti",
		"Backend Developer at Tech SK",
		"2020 - 2024",
		"Vyvoj REST API a integracii.",
		"Vzdelanie",
		"Ing. informatika",
		"STU Bratislava",
		"Jazyky",
		"Slovak - Native",
		"English - Fluent"
	];

	public static readonly string[] EnglishMessyLines =
	[
		"Alex Smith",
		"Engineer | Example Labs",
		"alex@example.com",
		"Work Experience",
		"Developer at Example Labs 2020 - Present",
		"- Built internal tools",
		"- Improved deployment pipeline",
		"Extra notes without clear section header should remain useful."
	];
}
