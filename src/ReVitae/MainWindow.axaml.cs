using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.Media;
using ReVitae.Core.Cv;
using ReVitae.Core.Validation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReVitae;

public partial class MainWindow : Window
{
    private readonly FieldValidator _validator = MainPersonalInformationSchema.CreateValidator();
    private CvTemplateId _selectedTemplate = CvTemplateId.CleanTopHeader;

    private enum CvTemplateId
    {
        ClassicSidebar,
        ModernSidebar,
        CleanTopHeader,
        DarkSidebarAccent
    }

    private sealed record CvTemplateData(
        string FirstName,
        string LastName,
        string ProfessionalTitle,
        string Email,
        string Phone,
        string Location,
        string LinkedInUrl,
        string PortfolioUrl,
        string GitHubUrl,
        string? ShortSummary,
        string? PhotoPath)
    {
        public string FullName => $"{FirstName} {LastName}".Trim();
    }

    public MainWindow()
    {
        InitializeComponent();
        UpdateTemplateSelectionState();
        UpdatePreview();
        UpdateValidationState();
    }

    private void OnFormTextChanged(object? sender, TextChangedEventArgs e)
    {
        UpdatePreview();
        UpdateValidationState();
        ExportStatusTextBlock.Text = string.Empty;
    }

    private void OnOpenSetupClicked(object? sender, RoutedEventArgs e)
    {
        SetSetupModalVisible(true);
    }

    private void OnOpenTemplatesClicked(object? sender, RoutedEventArgs e)
    {
        UpdateTemplateSelectionState();
        SetTemplatesModalVisible(true);
    }

    private void OnCloseSetupClicked(object? sender, RoutedEventArgs e)
    {
        SetSetupModalVisible(false);
    }

    private void OnCloseTemplatesClicked(object? sender, RoutedEventArgs e)
    {
        SetTemplatesModalVisible(false);
    }

    private void OnSelectClassicSidebarTemplateClicked(object? sender, RoutedEventArgs e)
    {
        SelectTemplate(CvTemplateId.ClassicSidebar);
    }

    private void OnSelectModernSidebarTemplateClicked(object? sender, RoutedEventArgs e)
    {
        SelectTemplate(CvTemplateId.ModernSidebar);
    }

    private void OnSelectCleanTopHeaderTemplateClicked(object? sender, RoutedEventArgs e)
    {
        SelectTemplate(CvTemplateId.CleanTopHeader);
    }

    private void OnSelectDarkSidebarTemplateClicked(object? sender, RoutedEventArgs e)
    {
        SelectTemplate(CvTemplateId.DarkSidebarAccent);
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        if (TemplatesModalOverlay.IsVisible)
        {
            SetTemplatesModalVisible(false);
        }
        else if (SetupModalOverlay.IsVisible)
        {
            SetSetupModalVisible(false);
        }
        else
        {
            return;
        }

        e.Handled = true;
    }

    private void OnWindowSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        UpdateModalSizes();
    }

    private async void OnExportPdfClicked(object? sender, RoutedEventArgs e)
    {
        var validationResult = ValidateForm();
        if (!validationResult.IsValid)
        {
            UpdateValidationState(validationResult);
            ExportStatusTextBlock.Text = "Fix validation errors before exporting PDF.";
            return;
        }

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
        PreviewContentControl.Content = BuildTemplatePreview();
    }

    private void SelectTemplate(CvTemplateId templateId)
    {
        _selectedTemplate = templateId;
        UpdateTemplateSelectionState();
        UpdatePreview();
        SetTemplatesModalVisible(false);
    }

    private void UpdateTemplateSelectionState()
    {
        ClassicSidebarSelectedTextBlock.IsVisible = _selectedTemplate == CvTemplateId.ClassicSidebar;
        ModernSidebarSelectedTextBlock.IsVisible = _selectedTemplate == CvTemplateId.ModernSidebar;
        CleanTopHeaderSelectedTextBlock.IsVisible = _selectedTemplate == CvTemplateId.CleanTopHeader;
        DarkSidebarSelectedTextBlock.IsVisible = _selectedTemplate == CvTemplateId.DarkSidebarAccent;
    }

    private void SetSetupModalVisible(bool isVisible)
    {
        SetupModalOverlay.IsVisible = isVisible;
        if (isVisible)
        {
            SetTemplatesModalVisible(false);
        }

        UpdateModalSizes();
    }

    private void SetTemplatesModalVisible(bool isVisible)
    {
        TemplatesModalOverlay.IsVisible = isVisible;
        if (isVisible)
        {
            SetSetupModalVisible(false);
        }

        UpdateModalSizes();
    }

    private void UpdateModalSizes()
    {
        SetupModalPanel.Width = Math.Max(SetupModalPanel.MinWidth, RootGrid.Bounds.Width * 0.8);
        SetupModalPanel.Height = Math.Max(SetupModalPanel.MinHeight, RootGrid.Bounds.Height * 0.8);
        TemplatesModalPanel.Width = Math.Max(TemplatesModalPanel.MinWidth, RootGrid.Bounds.Width * 0.8);
        TemplatesModalPanel.Height = Math.Max(TemplatesModalPanel.MinHeight, RootGrid.Bounds.Height * 0.8);
    }

    private void UpdateValidationState(FieldValidationResult? validationResult = null)
    {
        validationResult ??= ValidateForm();

        ExportPdfButton.IsEnabled = validationResult.IsValid;
        UpdateFieldErrorMessages(validationResult);
        ValidationSummaryTextBlock.Text = validationResult.IsValid
            ? string.Empty
            : string.Join(Environment.NewLine, validationResult.Errors.Select(error => error.Message));
    }

    private void UpdateFieldErrorMessages(FieldValidationResult validationResult)
    {
        var errorsByField = validationResult.Errors
            .GroupBy(error => error.FieldKey)
            .ToDictionary(
                group => group.Key,
                group => string.Join(Environment.NewLine, group.Select(error => error.Message)),
                StringComparer.Ordinal);

        FirstNameErrorTextBlock.Text = GetFieldError(errorsByField, MainPersonalInformationFieldKeys.FirstName);
        LastNameErrorTextBlock.Text = GetFieldError(errorsByField, MainPersonalInformationFieldKeys.LastName);
        ProfessionalTitleErrorTextBlock.Text = GetFieldError(errorsByField, MainPersonalInformationFieldKeys.ProfessionalTitle);
        EmailErrorTextBlock.Text = GetFieldError(errorsByField, MainPersonalInformationFieldKeys.Email);
        PhoneErrorTextBlock.Text = GetFieldError(errorsByField, MainPersonalInformationFieldKeys.Phone);
        LocationErrorTextBlock.Text = GetFieldError(errorsByField, MainPersonalInformationFieldKeys.Location);
        LinkedInUrlErrorTextBlock.Text = GetFieldError(errorsByField, MainPersonalInformationFieldKeys.LinkedInUrl);
        PortfolioUrlErrorTextBlock.Text = GetFieldError(errorsByField, MainPersonalInformationFieldKeys.PortfolioUrl);
        GitHubUrlErrorTextBlock.Text = GetFieldError(errorsByField, MainPersonalInformationFieldKeys.GitHubUrl);
        ShortSummaryErrorTextBlock.Text = GetFieldError(errorsByField, MainPersonalInformationFieldKeys.ShortSummary);
    }

    private static string GetFieldError(IReadOnlyDictionary<string, string> errorsByField, string fieldKey)
    {
        return errorsByField.TryGetValue(fieldKey, out var error) ? error : string.Empty;
    }

    private FieldValidationResult ValidateForm()
    {
        return _validator.Validate(BuildFieldValues());
    }

    private IReadOnlyDictionary<string, string?> BuildFieldValues()
    {
        return new Dictionary<string, string?>
        {
            [MainPersonalInformationFieldKeys.FirstName] = FirstNameTextBox.Text,
            [MainPersonalInformationFieldKeys.LastName] = LastNameTextBox.Text,
            [MainPersonalInformationFieldKeys.ProfessionalTitle] = ProfessionalTitleTextBox.Text,
            [MainPersonalInformationFieldKeys.Email] = EmailTextBox.Text,
            [MainPersonalInformationFieldKeys.Phone] = PhoneTextBox.Text,
            [MainPersonalInformationFieldKeys.Location] = LocationTextBox.Text,
            [MainPersonalInformationFieldKeys.LinkedInUrl] = LinkedInUrlTextBox.Text,
            [MainPersonalInformationFieldKeys.PortfolioUrl] = PortfolioUrlTextBox.Text,
            [MainPersonalInformationFieldKeys.GitHubUrl] = GitHubUrlTextBox.Text,
            [MainPersonalInformationFieldKeys.ShortSummary] = ShortSummaryTextBox.Text
        };
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

    private Control BuildTemplatePreview()
    {
        var data = BuildTemplateData();

        return _selectedTemplate switch
        {
            CvTemplateId.ClassicSidebar => BuildClassicSidebarTemplate(data),
            CvTemplateId.ModernSidebar => BuildModernSidebarTemplate(data),
            CvTemplateId.CleanTopHeader => BuildCleanTopHeaderTemplate(data),
            CvTemplateId.DarkSidebarAccent => BuildDarkSidebarAccentTemplate(data),
            _ => throw new ArgumentOutOfRangeException(nameof(_selectedTemplate))
        };
    }

    private CvTemplateData BuildTemplateData()
    {
        return new CvTemplateData(
            NormalizeValue(FirstNameTextBox.Text),
            NormalizeValue(LastNameTextBox.Text),
            NormalizeValue(ProfessionalTitleTextBox.Text),
            NormalizeValue(EmailTextBox.Text),
            NormalizeValue(PhoneTextBox.Text),
            NormalizeValue(LocationTextBox.Text),
            NormalizeValue(LinkedInUrlTextBox.Text),
            NormalizeValue(PortfolioUrlTextBox.Text),
            NormalizeValue(GitHubUrlTextBox.Text),
            ShortSummaryTextBox.Text?.Trim(),
            PhotoPath: null);
    }

    private static Control BuildClassicSidebarTemplate(CvTemplateData data)
    {
        var root = CreatePreviewRoot();
        root.ColumnDefinitions = new ColumnDefinitions("0.36*,0.64*");

        var sidebar = new StackPanel
        {
            Spacing = 14,
            Background = Brush.Parse("#D8D8D8"),
            Margin = new Thickness(18)
        };
        sidebar.Children.Add(CreateNameBlock(data.FirstName, data.LastName, "#F47C2C", stacked: true));
        sidebar.Children.Add(CreateContactSection(data));

        var content = CreateContentPanel();
        content.Children.Add(CreateSection("Summary", GetSummary(data)));
        content.Children.Add(CreateSection("Contact Links", BuildLines("LinkedIn", data.LinkedInUrl, "Portfolio", data.PortfolioUrl, "GitHub", data.GitHubUrl)));

        root.Children.Add(sidebar);
        Grid.SetColumn(content, 1);
        root.Children.Add(content);

        return root;
    }

    private static Control BuildModernSidebarTemplate(CvTemplateData data)
    {
        var root = CreatePreviewRoot();
        root.ColumnDefinitions = new ColumnDefinitions("0.34*,0.66*");

        var sidebar = new StackPanel
        {
            Spacing = 14,
            Background = Brush.Parse("#D7D7D7"),
            Margin = new Thickness(18)
        };
        sidebar.Children.Add(CreateSection("Contact", BuildLines("Phone", data.Phone, "Email", data.Email, "LinkedIn", data.LinkedInUrl)));

        var content = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*"),
            Background = Brushes.White
        };
        content.Children.Add(
            new Border
            {
                Background = Brush.Parse("#4A4A4A"),
                Padding = new Thickness(18, 12),
                Child = CreateText(data.FullName, 26, Brushes.White, FontWeight.Bold)
            });

        var body = CreateContentPanel();
        body.Children.Add(CreateSection("Profile", GetSummary(data)));
        body.Children.Add(CreateSection("Digital", BuildLines("Portfolio", data.PortfolioUrl, "GitHub", data.GitHubUrl)));
        Grid.SetRow(body, 1);
        content.Children.Add(body);

        root.Children.Add(sidebar);
        Grid.SetColumn(content, 1);
        root.Children.Add(content);

        return root;
    }

    private static Control BuildCleanTopHeaderTemplate(CvTemplateData data)
    {
        var root = new StackPanel
        {
            Background = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("0.55*,0.45*")
        };

        var namePanel = new StackPanel { Spacing = 6 };
        namePanel.Children.Add(CreateText(data.FullName, 30, Brushes.White, FontWeight.Bold));
        namePanel.Children.Add(CreateText(data.ProfessionalTitle, 14, Brushes.White, FontWeight.SemiBold));
        header.Children.Add(namePanel);

        var contact = new StackPanel { Spacing = 3 };
        contact.Children.Add(CreateText($"Email: {data.Email}", 11, Brushes.White, FontWeight.SemiBold));
        contact.Children.Add(CreateText($"Phone: {data.Phone}", 11, Brushes.White, FontWeight.SemiBold));
        contact.Children.Add(CreateText($"Location: {data.Location}", 11, Brushes.White, FontWeight.SemiBold));
        Grid.SetColumn(contact, 1);
        header.Children.Add(contact);

        root.Children.Add(
            new Border
            {
                Background = Brush.Parse("#5A9BD5"),
                Padding = new Thickness(28),
                Child = header
            });

        var body = CreateContentPanel();
        body.Children.Add(CreateSection("Summary", GetSummary(data)));
        body.Children.Add(CreateSection("Links", BuildLines("LinkedIn", data.LinkedInUrl, "Portfolio", data.PortfolioUrl, "GitHub", data.GitHubUrl)));
        root.Children.Add(body);

        return root;
    }

    private static Control BuildDarkSidebarAccentTemplate(CvTemplateData data)
    {
        var root = CreatePreviewRoot();
        root.ColumnDefinitions = new ColumnDefinitions("0.34*,0.66*");

        var sidebar = new StackPanel
        {
            Spacing = 16,
            Background = Brush.Parse("#2F3A45"),
            Margin = new Thickness(18)
        };
        sidebar.Children.Add(CreateText("CONTACT", 16, Brushes.White, FontWeight.Bold));
        sidebar.Children.Add(CreateText(BuildLines("Email", data.Email, "Phone", data.Phone, "Location", data.Location), 11, Brushes.White, FontWeight.Normal));

        var content = new StackPanel { Background = Brush.Parse("#F2F2F2") };
        content.Children.Add(
            new Border
            {
                Background = Brush.Parse("#5B9BB0"),
                Padding = new Thickness(20),
                Child = new StackPanel
                {
                    Children =
                    {
                        CreateText(data.FullName.ToUpperInvariant(), 28, Brushes.White, FontWeight.Bold),
                        CreateText(data.ProfessionalTitle.ToUpperInvariant(), 14, Brushes.White, FontWeight.SemiBold)
                    }
                }
            });

        var body = CreateContentPanel();
        body.Background = Brush.Parse("#F2F2F2");
        body.Children.Add(CreateSection("Objective", GetSummary(data)));
        body.Children.Add(CreateSection("Online", BuildLines("LinkedIn", data.LinkedInUrl, "Portfolio", data.PortfolioUrl, "GitHub", data.GitHubUrl)));
        content.Children.Add(body);

        root.Children.Add(sidebar);
        Grid.SetColumn(content, 1);
        root.Children.Add(content);

        return root;
    }

    private static Grid CreatePreviewRoot()
    {
        return new Grid
        {
            Background = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
    }

    private static StackPanel CreateContentPanel()
    {
        return new StackPanel
        {
            Spacing = 18,
            Background = Brushes.White,
            Margin = new Thickness(18)
        };
    }

    private static Control CreateNameBlock(string firstName, string lastName, string accentColor, bool stacked)
    {
        var panel = new StackPanel { Spacing = 4 };
        panel.Children.Add(CreateText(firstName, 24, Brushes.Black, FontWeight.Bold));
        panel.Children.Add(CreateText(lastName, 24, Brush.Parse(accentColor), FontWeight.Bold));
        return panel;
    }

    private static Control CreateContactSection(CvTemplateData data)
    {
        return CreateSection("Contact", BuildLines("Email", data.Email, "Phone", data.Phone, "Location", data.Location));
    }

    private static Control CreateSection(string title, string content)
    {
        var panel = new StackPanel { Spacing = 6 };
        panel.Children.Add(CreateText(title, 18, Brushes.Black, FontWeight.Bold));
        panel.Children.Add(
            new Border
            {
                Height = 1,
                Background = Brush.Parse("#B8B8B8")
            });
        panel.Children.Add(CreateText(content, 12, Brushes.Black, FontWeight.Normal));
        return panel;
    }

    private static TextBlock CreateText(string text, double fontSize, IBrush foreground, FontWeight fontWeight)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            Foreground = foreground,
            FontWeight = fontWeight,
            TextWrapping = TextWrapping.Wrap,
            TextTrimming = TextTrimming.None
        };
    }

    private static string BuildLines(params string[] labelValuePairs)
    {
        var lines = new List<string>();
        for (var index = 0; index < labelValuePairs.Length; index += 2)
        {
            var label = labelValuePairs[index];
            var value = labelValuePairs[index + 1];
            if (!string.IsNullOrWhiteSpace(value) && value != "-")
            {
                lines.Add($"{label}: {value}");
            }
        }

        return lines.Count == 0 ? "-" : string.Join(Environment.NewLine, lines);
    }

    private static string GetSummary(CvTemplateData data)
    {
        return string.IsNullOrWhiteSpace(data.ShortSummary) ? "-" : data.ShortSummary;
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