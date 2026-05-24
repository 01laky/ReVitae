using ReVitae.Core.Export;
using ReVitae.Core.Export.Fixtures;
using ReVitae.Core.Export.Pdf;
using ReVitae.Core.Import;

var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var baseName = "John Doe (minimal architect)";
var pdfPath = Path.Combine(repoRoot, $"{baseName}.pdf");
var txtPath = Path.Combine(repoRoot, $"{baseName}.txt");

var document = JohnDoeMinimalArchitectCvDataset.CreateDocument();
var emptySource = CvExportSourceDataFactory.Create(
    new PersonalInformationImport(),
    [],
    [],
    [],
    [],
    [],
    [],
    [],
    null);

var pdfBytes = new QuestPdfCvExporter().Export(document);
await File.WriteAllBytesAsync(pdfPath, pdfBytes);

await using (var txtStream = File.Create(txtPath))
{
    CvDocumentExporter.Export(document, emptySource, CvExportFormat.Txt, txtStream);
}

Console.WriteLine($"Generated: {pdfPath}");
Console.WriteLine($"Generated: {txtPath}");
Console.WriteLine($"PDF size: {pdfBytes.Length:N0} bytes");
Console.WriteLine($"Work: {document.WorkExperienceEntries.Count}");
Console.WriteLine($"Education: {document.EducationEntries.Count}");
Console.WriteLine($"Skill groups: {document.SkillsGroups.Count}");
Console.WriteLine($"Languages: {document.LanguageEntries.Count}");
Console.WriteLine($"Certificates: {document.CertificateEntries.Count}");
Console.WriteLine($"Projects: {document.ProjectEntries.Count}");
Console.WriteLine($"Links: {document.CustomLinkLines.Count}");
