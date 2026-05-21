using Avalonia.Controls;
using Avalonia.Interactivity;
using ReVitae.Core.Import;
using System;
using System.Collections.Generic;

namespace ReVitae.Ui;

public static class ImportConfidenceHelper
{
    public static void Apply(TextBox textBox, CvImportConfidence confidence)
    {
        if (confidence == CvImportConfidence.Low)
        {
            if (!textBox.Classes.Contains(UiClasses.ImportHint))
            {
                textBox.Classes.Add(UiClasses.ImportHint);
            }
        }
        else
        {
            textBox.Classes.Remove(UiClasses.ImportHint);
        }
    }

    public static void ClearOnEdit(TextBox textBox, EventHandler<RoutedEventArgs> existingHandler)
    {
        void Handler(object? sender, RoutedEventArgs args)
        {
            textBox.Classes.Remove(UiClasses.ImportHint);
        }

        textBox.TextChanged += Handler;
    }

    public static void ApplyToFields(
        IReadOnlyDictionary<string, TextBox> fieldsByKey,
        IReadOnlyList<ImportedFieldConfidence> confidences)
    {
        foreach (var confidence in confidences)
        {
            if (fieldsByKey.TryGetValue(confidence.FieldKey, out var textBox))
            {
                Apply(textBox, confidence.Confidence);
            }
        }
    }
}
