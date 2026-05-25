using ReVitae.Core.Export;
using ReVitae.Core.Export.Fixtures;
using ReVitae.Core.Export.Pdf;
using ReVitae.Core.Import.Pdf;

var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var fixturePath = Path.Combine(
    repoRoot,
    "tests",
    "ReVitae.Tests",
    "Import",
    "Fixtures",
    "JohnDoeStressCv.pdf");
var localPath = Path.Combine(repoRoot, "John Doe.pdf");

var document = JohnDoeStressCvDataset.CreateDocument();
var pdfBytes = new QuestPdfCvExporter().Export(document);
await File.WriteAllBytesAsync(fixturePath, pdfBytes);
await File.WriteAllBytesAsync(localPath, pdfBytes);

Console.WriteLine($"Generated fixture: {fixturePath}");
Console.WriteLine($"Generated local: {localPath}");
Console.WriteLine($"Size: {pdfBytes.Length:N0} bytes");
Console.WriteLine($"Work: {document.WorkExperienceEntries.Count}");
Console.WriteLine($"Education: {document.EducationEntries.Count}");
Console.WriteLine($"Skill groups: {document.SkillsGroups.Count} ({document.SkillsGroups.Sum(g => g.Skills.Count)} skills)");
