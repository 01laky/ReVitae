using ReVitae.Core.Export.Fixtures;
using ReVitae.Core.Export.Pdf;

var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var outputPath = Path.Combine(repoRoot, "John Doe.pdf");

var document = JohnDoeStressCvDataset.CreateDocument();
var pdfBytes = new QuestPdfCvExporter().Export(document);
await File.WriteAllBytesAsync(outputPath, pdfBytes);

Console.WriteLine($"Generated: {outputPath}");
Console.WriteLine($"Size: {pdfBytes.Length:N0} bytes");
Console.WriteLine($"Work: {document.WorkExperienceEntries.Count}");
Console.WriteLine($"Education: {document.EducationEntries.Count}");
Console.WriteLine($"Skill groups: {document.SkillsGroups.Count} ({document.SkillsGroups.Sum(g => g.Skills.Count)} skills)");
Console.WriteLine($"Languages: {document.LanguageEntries.Count}");
Console.WriteLine($"Certificates: {document.CertificateEntries.Count}");
Console.WriteLine($"Projects: {document.ProjectEntries.Count}");
Console.WriteLine($"Links: {document.CustomLinkLines.Count}");
