using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ReVitae;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        UpdatePreview();
    }

    private void OnFormTextChanged(object? sender, TextChangedEventArgs e)
    {
        UpdatePreview();
        ExportStatusTextBlock.Text = string.Empty;
    }

    private async void OnExportPdfClicked(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            ExportStatusTextBlock.Text = "Unable to open the file picker.";
            return;
        }

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "Export PDF",
                SuggestedFileName = "revitae-basic-cv.pdf",
                DefaultExtension = "pdf",
                FileTypeChoices =
                [
                    new FilePickerFileType("PDF")
                    {
                        Patterns = ["*.pdf"],
                        MimeTypes = ["application/pdf"]
                    }
                ]
            });

        if (file is null)
        {
            return;
        }

        await using var stream = await file.OpenWriteAsync();
        var pdfBytes = CreatePdfBytes(BuildPreviewLines());
        await stream.WriteAsync(pdfBytes);

        ExportStatusTextBlock.Text = $"Exported PDF to {file.Name}.";
    }

    private void UpdatePreview()
    {
        PreviewTextBlock.Text = string.Join(Environment.NewLine, BuildPreviewLines());
    }

    private string[] BuildPreviewLines()
    {
        var lines = new List<string>
        {
            BuildFullName(),
            NormalizeValue(ProfessionalTitleTextBox.Text),
            string.Empty,
            $"Email: {NormalizeValue(EmailTextBox.Text)}",
            $"Phone: {NormalizeValue(PhoneTextBox.Text)}",
            $"Location: {NormalizeValue(LocationTextBox.Text)}",
            $"LinkedIn: {NormalizeValue(LinkedInUrlTextBox.Text)}",
            $"Portfolio: {NormalizeValue(PortfolioUrlTextBox.Text)}",
            $"GitHub: {NormalizeValue(GitHubUrlTextBox.Text)}",
            string.Empty,
            "Summary:"
        };

        lines.AddRange(BuildSummaryLines());

        return lines.ToArray();
    }

    private string BuildFullName()
    {
        var nameParts = new[]
        {
            FirstNameTextBox.Text?.Trim(),
            LastNameTextBox.Text?.Trim()
        };

        var fullName = string.Join(
            " ",
            Array.FindAll(nameParts, part => !string.IsNullOrWhiteSpace(part)));

        return string.IsNullOrWhiteSpace(fullName) ? "-" : fullName;
    }

    private string[] BuildSummaryLines()
    {
        var summary = ShortSummaryTextBox.Text;
        if (string.IsNullOrWhiteSpace(summary))
        {
            return new[] { "-" };
        }

        return summary
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.None);
    }

    private static string NormalizeValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    }

    private static byte[] CreatePdfBytes(IReadOnlyList<string> lines)
    {
        var content = new StringBuilder();
        content.AppendLine("BT");
        content.AppendLine("/F1 14 Tf");
        content.AppendLine("72 760 Td");

        for (var index = 0; index < lines.Count; index++)
        {
            if (index > 0)
            {
                content.AppendLine("0 -24 Td");
            }

            content.Append('(');
            content.Append(EscapePdfText(lines[index]));
            content.AppendLine(") Tj");
        }

        content.AppendLine("ET");

        var contentText = content.ToString();
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(contentText)} >>\nstream\n{contentText}endstream"
        };

        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true);
        var offsets = new List<long> { 0 };

        writer.WriteLine("%PDF-1.4");

        for (var index = 0; index < objects.Length; index++)
        {
            writer.Flush();
            offsets.Add(stream.Position);
            writer.WriteLine($"{index + 1} 0 obj");
            writer.WriteLine(objects[index]);
            writer.WriteLine("endobj");
        }

        writer.Flush();
        var xrefOffset = stream.Position;

        writer.WriteLine("xref");
        writer.WriteLine($"0 {objects.Length + 1}");
        writer.WriteLine("0000000000 65535 f ");

        for (var index = 1; index < offsets.Count; index++)
        {
            writer.WriteLine($"{offsets[index]:D10} 00000 n ");
        }

        writer.WriteLine("trailer");
        writer.WriteLine($"<< /Size {objects.Length + 1} /Root 1 0 R >>");
        writer.WriteLine("startxref");
        writer.WriteLine(xrefOffset);
        writer.WriteLine("%%EOF");
        writer.Flush();

        return stream.ToArray();
    }

    private static string EscapePdfText(string text)
    {
        return text
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
    }
}