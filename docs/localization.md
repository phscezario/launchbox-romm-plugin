# Localization

The plugin supports multiple languages through a JSON-based localization system.

## Supported Languages

| Language | Code | File | Status |
|----------|------|------|--------|
| English | `en` | `RommPlugin.Core/Locales/en.json` | Default |
| Portuguese (Brazil) | `pt-BR` | `RommPlugin.Core/Locales/pt-BR.json` | Complete |

## How It Works

### Locale Manager

The `LocaleManager` class (`RommPlugin.Core/Locale/LocaleManager.cs`) handles:

1. Loading JSON locale files from the `Locales/` folder
2. Providing string lookup by key
3. Falling back to English if a key is missing in the current language

### Usage in Code

```csharp
// Get a translated string
string message = LocaleManager.GetString("sync_complete");

// With formatting
string progress = LocaleManager.GetString("sync_progress", platformCount, gameCount);
```

### Language Selection

Users can change the language in the plugin Settings UI. The selected language is stored in `settings.json`:

```json
{
  "Language": "pt-BR"
}
```

## Locale File Structure

Each locale file is a flat JSON object with string keys:

```json
{
  "sync_complete": "Sync completed successfully",
  "sync_in_progress": "Syncing...",
  "settings_title": "RomM Plugin Settings",
  "error_connection_failed": "Failed to connect to RomM server",
  "game_install_confirm": "Are you sure you want to install {0}?"
}
```

### Key Naming Convention

Keys use `snake_case` and are organized by feature:

| Prefix | Feature | Example |
|--------|---------|---------|
| `sync_` | Synchronization | `sync_complete`, `sync_in_progress` |
| `settings_` | Settings form | `settings_title`, `settings_save` |
| `install_` | Game installation | `install_confirm`, `install_complete` |
| `uninstall_` | Game uninstallation | `uninstall_confirm` |
| `download_` | Download queue | `download_progress`, `download_failed` |
| `error_` | Error messages | `error_connection`, `error_auth` |
| `menu_` | Menu items | `menu_sync`, `menu_settings` |
| `platform_` | Platform selector | `platform_select`, `platform_all` |

## Adding a New Language

### 1. Create the Locale File

Create a new JSON file in `RommPlugin.Core/Locales/`:

```
RommPlugin.Core/Locales/
├── en.json
├── pt-BR.json
└── {language-code}.json
```

### 2. Copy English Strings

Copy `en.json` as a starting point:

```bash
cp RommPlugin.Core/Locales/en.json RommPlugin.Core/Locales/{language-code}.json
```

### 3. Translate the Strings

Translate each value while keeping the keys identical:

```json
{
  "sync_complete": "Sincronização concluída com sucesso",
  "sync_in_progress": "Sincronizando..."
}
```

### 4. Add to the Project

The locale file is automatically included in the build via the `.csproj` glob:

```xml
<Content Include="Locales\**\*.json">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</Content>
```

### 5. Test

1. Build the solution
2. Set `"Language": "{language-code}"` in `settings.json`
3. Restart LaunchBox
4. Verify all strings display correctly

## Fallback Behavior

If a key is missing in the current language:

1. `LocaleManager` falls back to the English (`en`) locale
2. If the key is missing in English too, the key itself is returned
3. This prevents crashes from missing translations

## Adding New Strings

When adding new UI strings:

1. Add the key to `en.json` with the English text
2. Add the key to `pt-BR.json` with the Portuguese translation
3. Use `LocaleManager.GetString("key")` in code

### Example

```json
// en.json
{
  "new_feature_enabled": "New feature is enabled"
}

// pt-BR.json
{
  "new_feature_enabled": "Nova funcionalidade habilitada"
}
```

```csharp
// In code
var message = LocaleManager.GetString("new_feature_enabled");
```
