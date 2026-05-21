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
            FieldSchemaFactory.RequiredText(
                LinksFieldKeys.Label,
                "Label",
                LabelMaxLength,
                TranslationKeys.ValidationLinksLabelRequired,
                TranslationKeys.ValidationLinksLabelMax),
            FieldSchemaFactory.RequiredUrl(
                LinksFieldKeys.Url,
                "URL",
                UrlMaxLength,
                TranslationKeys.ValidationLinksUrlRequired,
                TranslationKeys.ValidationLinksUrlMax,
                TranslationKeys.ValidationLinksUrlFormat),
            FieldSchemaFactory.OptionalText(
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
}
