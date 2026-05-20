using System.Globalization;

namespace ReVitae.Core.Localization;

public sealed class AppLocalizer
{
    public const string FallbackLanguageCode = "en";

    public static readonly IReadOnlyList<SupportedLanguage> SupportedLanguages =
    [
        new("en", "English", "English", "🇬🇧"),
        new("es", "Spanish", "Español", "🇪🇸"),
        new("fr", "French", "Français", "🇫🇷"),
        new("de", "German", "Deutsch", "🇩🇪"),
        new("pt", "Portuguese", "Português", "🇵🇹"),
        new("it", "Italian", "Italiano", "🇮🇹"),
        new("nl", "Dutch", "Nederlands", "🇳🇱"),
        new("pl", "Polish", "Polski", "🇵🇱"),
        new("uk", "Ukrainian", "Українська", "🇺🇦"),
        new("zh-Hans", "Chinese Simplified", "简体中文", "🇨🇳"),
        new("sk", "Slovak", "Slovenčina", "🇸🇰"),
        new("cs", "Czech", "Čeština", "🇨🇿")
    ];

    private static readonly IReadOnlyDictionary<string, string> EnglishTranslations = new Dictionary<string, string>
    {
        [TranslationKeys.HeaderSubtitle] = "Create a simple CV preview and export a plain PDF.",
        [TranslationKeys.OpenSetup] = "Open setup",
        [TranslationKeys.OpenTemplates] = "Open templates",
        [TranslationKeys.MainPersonalInformation] = "Main / Personal information",
        [TranslationKeys.FirstName] = "First name",
        [TranslationKeys.LastName] = "Last name",
        [TranslationKeys.ProfessionalTitle] = "Professional title",
        [TranslationKeys.Email] = "Email",
        [TranslationKeys.Phone] = "Phone",
        [TranslationKeys.Location] = "Location",
        [TranslationKeys.LinkedInUrl] = "LinkedIn URL",
        [TranslationKeys.PortfolioUrl] = "Portfolio / website URL",
        [TranslationKeys.GitHubUrl] = "GitHub URL",
        [TranslationKeys.ShortSummary] = "Short summary",
        [TranslationKeys.ShortSummaryPlaceholder] = "Write two to four sentences about your professional profile.",
        [TranslationKeys.ExportPdf] = "Export PDF",
        [TranslationKeys.Preview] = "Preview",
        [TranslationKeys.Setup] = "Setup",
        [TranslationKeys.SetupPlaceholder] = "Setup options will be added in a future step.",
        [TranslationKeys.Templates] = "Templates",
        [TranslationKeys.Close] = "Close",
        [TranslationKeys.Language] = "Language",
        [TranslationKeys.Selected] = "Selected",
        [TranslationKeys.ClassicSidebar] = "Classic Sidebar",
        [TranslationKeys.ClassicSidebarDescription] = "Light sidebar with contact details and a clean content area.",
        [TranslationKeys.ModernSidebar] = "Modern Sidebar",
        [TranslationKeys.ModernSidebarDescription] = "Compact sidebar with a dark name band and focused content.",
        [TranslationKeys.CleanTopHeader] = "Clean Top Header",
        [TranslationKeys.CleanTopHeaderDescription] = "Safe default with a full-width color header and simple sections.",
        [TranslationKeys.DarkSidebarAccent] = "Dark Sidebar Accent",
        [TranslationKeys.DarkSidebarAccentDescription] = "Dark sidebar, colored header, and stronger visual personality.",
        [TranslationKeys.Summary] = "Summary",
        [TranslationKeys.Contact] = "Contact",
        [TranslationKeys.ContactLinks] = "Contact Links",
        [TranslationKeys.Profile] = "Profile",
        [TranslationKeys.Digital] = "Digital",
        [TranslationKeys.Links] = "Links",
        [TranslationKeys.Objective] = "Objective",
        [TranslationKeys.Online] = "Online",
        [TranslationKeys.ExportFixValidation] = "Fix validation errors before exporting PDF.",
        [TranslationKeys.ExportFilePickerUnavailable] = "Unable to open the file picker.",
        [TranslationKeys.ExportedPdfTo] = "Exported PDF to {0}.",
        [TranslationKeys.ValidationFirstNameRequired] = "First name is required.",
        [TranslationKeys.ValidationLastNameRequired] = "Last name is required.",
        [TranslationKeys.ValidationEmailRequired] = "Email is required.",
        [TranslationKeys.ValidationFirstNameMax] = "First name must be 80 characters or fewer.",
        [TranslationKeys.ValidationLastNameMax] = "Last name must be 80 characters or fewer.",
        [TranslationKeys.ValidationProfessionalTitleMax] = "Professional title must be 120 characters or fewer.",
        [TranslationKeys.ValidationEmailMax] = "Email must be 160 characters or fewer.",
        [TranslationKeys.ValidationPhoneMax] = "Phone must be 40 characters or fewer.",
        [TranslationKeys.ValidationLocationMax] = "Location must be 120 characters or fewer.",
        [TranslationKeys.ValidationLinkedInUrlMax] = "LinkedIn URL must be 240 characters or fewer.",
        [TranslationKeys.ValidationPortfolioUrlMax] = "Portfolio / website URL must be 240 characters or fewer.",
        [TranslationKeys.ValidationGitHubUrlMax] = "GitHub URL must be 240 characters or fewer.",
        [TranslationKeys.ValidationShortSummaryMax] = "Short summary must be 800 characters or fewer.",
        [TranslationKeys.ValidationEmailFormat] = "Email must be a valid email address.",
        [TranslationKeys.ValidationLinkedInUrlFormat] = "LinkedIn URL must be a valid http or https URL.",
        [TranslationKeys.ValidationPortfolioUrlFormat] = "Portfolio / website URL must be a valid http or https URL.",
        [TranslationKeys.ValidationGitHubUrlFormat] = "GitHub URL must be a valid http or https URL."
    };

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> TranslationOverlays =
        new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["sk"] = Overlay("Vytvorte jednoduchý náhľad CV a exportujte plain PDF.", "Nastavenia", "Šablóny", "Jazyk", "Zavrieť", "Meno", "Priezvisko", "Profesijný titul", "Telefón", "Lokalita", "Krátke zhrnutie", "Náhľad", "Vybrané"),
            ["cs"] = Overlay("Vytvořte jednoduchý náhled CV a exportujte plain PDF.", "Nastavení", "Šablony", "Jazyk", "Zavřít", "Jméno", "Příjmení", "Profesní titul", "Telefon", "Lokalita", "Krátké shrnutí", "Náhled", "Vybráno"),
            ["es"] = Overlay("Crea una vista previa simple del CV y exporta un PDF básico.", "Configuración", "Plantillas", "Idioma", "Cerrar", "Nombre", "Apellido", "Título profesional", "Teléfono", "Ubicación", "Resumen breve", "Vista previa", "Seleccionado"),
            ["fr"] = Overlay("Créez un aperçu simple du CV et exportez un PDF léger.", "Configuration", "Modèles", "Langue", "Fermer", "Prénom", "Nom", "Titre professionnel", "Téléphone", "Lieu", "Résumé court", "Aperçu", "Sélectionné"),
            ["de"] = Overlay("Erstellen Sie eine einfache CV-Vorschau und exportieren Sie ein schlichtes PDF.", "Einrichtung", "Vorlagen", "Sprache", "Schließen", "Vorname", "Nachname", "Berufsbezeichnung", "Telefon", "Standort", "Kurzprofil", "Vorschau", "Ausgewählt"),
            ["pt"] = Overlay("Crie uma pré-visualização simples do CV e exporte um PDF básico.", "Configuração", "Modelos", "Idioma", "Fechar", "Nome", "Sobrenome", "Título profissional", "Telefone", "Localização", "Resumo curto", "Pré-visualização", "Selecionado"),
            ["it"] = Overlay("Crea un'anteprima semplice del CV ed esporta un PDF leggero.", "Configurazione", "Modelli", "Lingua", "Chiudi", "Nome", "Cognome", "Titolo professionale", "Telefono", "Località", "Breve riepilogo", "Anteprima", "Selezionato"),
            ["nl"] = Overlay("Maak een eenvoudige cv-preview en exporteer een eenvoudige PDF.", "Instellingen", "Sjablonen", "Taal", "Sluiten", "Voornaam", "Achternaam", "Functietitel", "Telefoon", "Locatie", "Korte samenvatting", "Voorbeeld", "Geselecteerd"),
            ["pl"] = Overlay("Utwórz prosty podgląd CV i wyeksportuj lekki PDF.", "Ustawienia", "Szablony", "Język", "Zamknij", "Imię", "Nazwisko", "Tytuł zawodowy", "Telefon", "Lokalizacja", "Krótkie podsumowanie", "Podgląd", "Wybrano"),
            ["uk"] = Overlay("Створіть простий попередній перегляд CV та експортуйте простий PDF.", "Налаштування", "Шаблони", "Мова", "Закрити", "Ім'я", "Прізвище", "Професійний заголовок", "Телефон", "Місцезнаходження", "Короткий опис", "Перегляд", "Вибрано"),
            ["zh-Hans"] = Overlay("创建简单的简历预览并导出基础 PDF。", "设置", "模板", "语言", "关闭", "名", "姓", "职业标题", "电话", "位置", "简短摘要", "预览", "已选择")
        };

    private readonly IReadOnlyDictionary<string, string> _translations;

    public AppLocalizer(string languageCode)
    {
        LanguageCode = DetectSupportedLanguage(languageCode);
        _translations = BuildTranslations(LanguageCode);
    }

    public string LanguageCode { get; }

    public static AppLocalizer FromSystemCulture()
    {
        return new AppLocalizer(CultureInfo.CurrentUICulture.Name);
    }

    public static string DetectSupportedLanguage(CultureInfo culture)
    {
        return DetectSupportedLanguage(culture.Name);
    }

    public static string DetectSupportedLanguage(string cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
        {
            return FallbackLanguageCode;
        }

        var normalized = cultureName.Equals("zh", StringComparison.OrdinalIgnoreCase)
            ? "zh-Hans"
            : cultureName;

        if (SupportedLanguages.Any(language => language.Code.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
        {
            return SupportedLanguages.First(language => language.Code.Equals(normalized, StringComparison.OrdinalIgnoreCase)).Code;
        }

        var parent = normalized.Split('-', StringSplitOptions.RemoveEmptyEntries)[0];
        if (parent.Equals("zh", StringComparison.OrdinalIgnoreCase))
        {
            return "zh-Hans";
        }

        var parentMatch = SupportedLanguages.FirstOrDefault(language => language.Code.Equals(parent, StringComparison.OrdinalIgnoreCase));
        return parentMatch?.Code ?? FallbackLanguageCode;
    }

    public string Get(string key)
    {
        return _translations.TryGetValue(key, out var value) ? value : key;
    }

    public string Format(string key, params object[] values)
    {
        return string.Format(CultureInfo.CurrentCulture, Get(key), values);
    }

    public static IReadOnlyDictionary<string, string> GetTranslations(string languageCode)
    {
        return BuildTranslations(DetectSupportedLanguage(languageCode));
    }

    private static IReadOnlyDictionary<string, string> BuildTranslations(string languageCode)
    {
        var translations = new Dictionary<string, string>(EnglishTranslations, StringComparer.Ordinal);
        if (TranslationOverlays.TryGetValue(languageCode, out var overlay))
        {
            foreach (var (key, value) in overlay)
            {
                translations[key] = value;
            }
        }

        return translations;
    }

    private static IReadOnlyDictionary<string, string> Overlay(
        string subtitle,
        string setup,
        string templates,
        string language,
        string close,
        string firstName,
        string lastName,
        string professionalTitle,
        string phone,
        string location,
        string shortSummary,
        string preview,
        string selected)
    {
        return new Dictionary<string, string>
        {
            [TranslationKeys.HeaderSubtitle] = subtitle,
            [TranslationKeys.OpenSetup] = setup,
            [TranslationKeys.OpenTemplates] = templates,
            [TranslationKeys.Setup] = setup,
            [TranslationKeys.Templates] = templates,
            [TranslationKeys.Language] = language,
            [TranslationKeys.Close] = close,
            [TranslationKeys.FirstName] = firstName,
            [TranslationKeys.LastName] = lastName,
            [TranslationKeys.ProfessionalTitle] = professionalTitle,
            [TranslationKeys.Phone] = phone,
            [TranslationKeys.Location] = location,
            [TranslationKeys.ShortSummary] = shortSummary,
            [TranslationKeys.Preview] = preview,
            [TranslationKeys.Selected] = selected
        };
    }
}
