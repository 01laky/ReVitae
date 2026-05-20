# Prompt 007 - Internationalization

Add the first internationalization layer for ReVitae.

## Goal

Make the app UI translatable and add language selection to the Setup modal.

The app should automatically detect the user's system language on startup. If
the detected language is supported, ReVitae should use it by default. If the
detected language is not supported, the app should fall back to English.

The user should also be able to change the language manually from the Setup
modal.

## Supported Languages

Add support for the top 10 broadly useful languages plus Slovak and Czech.

Initial supported languages:

- English (`en`),
- Spanish (`es`),
- French (`fr`),
- German (`de`),
- Portuguese (`pt`),
- Italian (`it`),
- Dutch (`nl`),
- Polish (`pl`),
- Ukrainian (`uk`),
- Chinese Simplified (`zh-Hans`),
- Slovak (`sk`),
- Czech (`cs`).

English should be the fallback language.

## Language Detection

On app startup:

1. Detect the current OS/system UI culture.
2. Try to map it to one of the supported languages.
3. If an exact culture is not supported, try matching by the parent language.
4. If no match exists, use English.

Examples:

- `sk-SK` should select Slovak.
- `cs-CZ` should select Czech.
- `en-US` should select English.
- `es-MX` should select Spanish.
- unsupported languages should fall back to English.

## Setup Modal Language Selector

Add a language selector to the Setup modal.

The selector should:

- show all supported languages,
- show language names clearly,
- display the currently selected language,
- allow the user to choose another language,
- immediately apply the selected language to the app UI.

It is acceptable in this step to keep language selection in memory only. Persistent
language preferences can be added later.

## Text Coverage

All current app UI text should use the translation system.

This includes:

- window/header texts,
- form labels,
- placeholders where practical,
- validation messages,
- button labels,
- modal titles,
- modal placeholder texts,
- template names and descriptions,
- preview section labels,
- export status messages.

Do not leave hardcoded user-facing strings in the UI unless they are sample data
or intentionally not localizable.

## Validation Messages

Validation errors should also be localized.

The validation schema should either:

- use translation keys for messages, or
- generate messages through a localization service.

Avoid permanently hardcoding English validation messages inside reusable schema
definitions.

## Preview and Templates

Template layout labels should localize where they are part of the UI.

Examples:

- `Summary`,
- `Contact`,
- `Links`,
- `Profile`,
- `Templates`,
- `Selected`.

User-provided form data should not be translated.

## Tests

Add unit tests for the localization layer.

Tests should cover:

- system culture mapping for supported languages,
- fallback to English for unsupported languages,
- parent-language matching such as `es-MX` to `es`,
- Slovak and Czech detection,
- all supported language codes being available,
- required translation keys existing in every supported language.

The tests should run through the existing C# lint/test flow.

## Out of Scope

Do not implement these features yet:

- persistence of selected language,
- cloud-synced language settings,
- AI translation of user CV content,
- translating user-entered form values,
- right-to-left layout support,
- pluralization framework beyond what current UI needs,
- region-specific variants beyond language fallback.

## Expected Result

The app should start in the detected system language when supported, fall back to
English otherwise, and allow the user to change language from the Setup modal.
All current user-facing app text should come from the localization layer, and
localization behavior should be covered by unit tests.
