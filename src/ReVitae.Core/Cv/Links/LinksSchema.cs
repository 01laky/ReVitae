using ReVitae.Core.Localization;
using ReVitae.Core.Validation;

namespace ReVitae.Core.Cv.Links;

public static class LinksSchema
{
    public const int LabelMaxLength = 80;
    public const int UrlMaxLength = 240;
    public const int NoteMaxLength = 120;

    public static readonly IReadOnlyList<FieldSchema> EntryFields = Array.AsReadOnly(
        new[]
        {
            RequiredText(
                LinksFieldKeys.Label,
                "Label",
                LabelMaxLength,
                TranslationKeys.ValidationLinksLabelRequired,
                TranslationKeys.ValidationLinksLabelMax),
            RequiredUrl(
                LinksFieldKeys.Url,
                "URL",
                UrlMaxLength,
                TranslationKeys.ValidationLinksUrlRequired,
                TranslationKeys.ValidationLinksUrlMax,
                TranslationKeys.ValidationLinksUrlFormat),
            OptionalText(
                LinksFieldKeys.Note,
                "Note",
                NoteMaxLength,
                TranslationKeys.ValidationLinksNoteMax)
        });

    public static FieldValidator CreateEntryValidator()
    {
        return new FieldValidator(EntryFields);
    }

    public static IReadOnlyList<FieldSchema> CreateSchemasForEntry(string entryId)
    {
        return EntryFields
            .Select(field => field with { Key = LinksFieldKeys.Build(entryId, field.Key) })
            .ToArray();
    }

    private static FieldSchema RequiredText(
        string key,
        string label,
        int maximumLength,
        string requiredMessageKey,
        string maximumLengthMessageKey)
    {
        return new FieldSchema(
            key,
            label,
            IsRequired: true,
            maximumLength,
            FieldFormat.Text,
            RequiredMessage: requiredMessageKey,
            MaximumLengthMessage: maximumLengthMessageKey);
    }

    private static FieldSchema OptionalText(string key, string label, int maximumLength, string maximumLengthMessageKey)
    {
        return new FieldSchema(
            key,
            label,
            IsRequired: false,
            maximumLength,
            FieldFormat.Text,
            RequiredMessage: string.Empty,
            MaximumLengthMessage: maximumLengthMessageKey);
    }

    private static FieldSchema RequiredUrl(
        string key,
        string label,
        int maximumLength,
        string requiredMessageKey,
        string maximumLengthMessageKey,
        string formatMessageKey)
    {
        return new FieldSchema(
            key,
            label,
            IsRequired: true,
            maximumLength,
            FieldFormat.Url,
            RequiredMessage: requiredMessageKey,
            MaximumLengthMessage: maximumLengthMessageKey,
            FormatMessage: formatMessageKey);
    }
}
