namespace ReVitae.Core.Import.Ocr;

/// <summary>Sorts OCR lines by bounding boxes for stable top-to-bottom reading order.</summary>
public static class OcrLayoutNormalizer
{
    public static string Normalize(OcrRecognitionResult recognition, string? logSection = null)
    {
        if (recognition.Lines is not { Count: > 0 } lines)
        {
            if (logSection is not null)
            {
                CvImportDiagnosticsLogger.LogStep(logSection, "Layout normalize: no bounding boxes, using raw OCR text");
            }

            return recognition.Text.Trim();
        }

        var ordered = lines
            .Where(line => !string.IsNullOrWhiteSpace(line.Text))
            .OrderBy(line => line.Top)
            .ThenBy(line => line.Left)
            .ToArray();

        if (ordered.Length == 0)
        {
            if (logSection is not null)
            {
                CvImportDiagnosticsLogger.LogStep(logSection, "Layout normalize: all lines empty, using raw OCR text");
            }

            return recognition.Text.Trim();
        }

        var medianHeight = ordered
            .Select(line => Math.Max(1, line.Bottom - line.Top))
            .OrderBy(height => height)
            .ElementAt(ordered.Length / 2);

        var rowTolerance = (int)Math.Ceiling(medianHeight * 1.5);
        var rows = new List<List<OcrTextLine>>();
        List<OcrTextLine>? currentRow = null;
        var currentRowTop = 0;

        foreach (var line in ordered)
        {
            if (currentRow is null || Math.Abs(line.Top - currentRowTop) > rowTolerance)
            {
                currentRow = [line];
                rows.Add(currentRow);
                currentRowTop = line.Top;
                continue;
            }

            currentRow.Add(line);
        }

        var builder = new List<string>(rows.Count);
        var previousBottom = int.MinValue;

        foreach (var row in rows)
        {
            var rowTop = row.Min(line => line.Top);
            if (previousBottom != int.MinValue && rowTop - previousBottom > rowTolerance * 2)
            {
                builder.Add(string.Empty);
            }

            builder.Add(string.Join(' ', row.OrderBy(line => line.Left).Select(line => line.Text.Trim())));
            previousBottom = row.Max(line => line.Bottom);
        }

        var normalized = string.Join('\n', builder.Where(line => line.Length > 0)).Trim();

        if (logSection is not null)
        {
            CvImportDiagnosticsLogger.LogStep(
                logSection,
                $"Layout normalized: {ordered.Length} lines → {rows.Count} rows, " +
                $"rowTolerance={rowTolerance}px, output={normalized.Length} chars");
        }

        return normalized;
    }
}
