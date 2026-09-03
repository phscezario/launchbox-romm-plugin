# Architecture

This document describes the system architecture, project structure, and design patterns used in the LaunchBox RomM Plugin.

## Solution Structure

```
LaunchBoxRommPlugin.slnx
│
├── RommPlugin/                  # Main plugin DLL (net48)
│   ├── RommPlugin.cs            # Entry point: RommMenuPlugin class
│   ├── ApiClient/               # HTTP client for RomM REST API
│   ├── Helpers/                 # Game helpers, metadata comparison
│   ├── MenuItems/               # LaunchBox menu item implementations
│   │   └── Buttons/             # Concrete menu items (Sync, Settings, etc.)
│   └── Services/                # Sync engine, metadata mapping, images, stats
│
├── RommPlugin.Core/             # Shared core library (net48)
│   ├── Constants/               # All constants (prefixes, filenames, limits)
│   ├── Helpers/                 # Auth, file writing, credential storage
│   ├── Interfaces/              # IProgressReporter
│   ├── Locale/                  # i18n locale manager
│   ├── Locales/                 # JSON locale files (en, pt-BR)
│   ├── Logging/                 # File-based logger with auto-cleanup
│   ├── Models/                  # All data models (21 files)
│   │   └── Statics/             # Static constants (custom fields, extensions)
│   ├── Services/                # Download queue, installed games, updates
│   └── Storage/                 # Settings, paths, sync state persistence
│
├── RommPlugin.UI/               # Windows Forms UI library (net48)
│   ├── Forms/                   # Settings, Platform Selector, Game Manager, etc.
│   ├── Helpers/                 # Icon, progress rendering, file watcher
│   └── Reporters/               # IProgressReporter implementation for UI
│
├── RommPlugin.CLI/              # Console tool (net48, Exe)
│   └── Program.cs               # Parents.xml hierarchy fix, remove-all cleanup
│
├── RommPlugin.Tests/            # Unit tests (net48)
│   ├── Helpers/                 # Auth, game helpers, file writer, credential tests
│   ├── Models/                  # Settings, stats, screenshot, download tests
│   └── Services/                # API client, metadata mapper, sync stats tests
│
├── lib/LaunchBox/               # LaunchBox plugin SDK (vendored DLL)
├── Images/                      # Plugin icons (ico.ico, ico.png, Installed.png)
├── .github/workflows/           # CI/CD pipelines (5 workflows)
├── Directory.Build.props        # Shared MSBuild properties
├── VERSION                      # Current version: 1.0.3
└── settings.example.json        # Template settings file
```

## Dependency Graph

```
RommPlugin (main DLL)
  ├── depends on → RommPlugin.Core
  ├── depends on → RommPlugin.UI
  └── references → Unbroken.LaunchBox.Plugins.dll (vendored)

RommPlugin.UI
  └── depends on → RommPlugin.Core

RommPlugin.CLI
  └── depends on → RommPlugin.Core

RommPlugin.Tests
  ├── depends on → RommPlugin
  └── depends on → RommPlugin.Core
```

All projects target **.NET Framework 4.8** (`net48`).

## Entry Point

The plugin is loaded by LaunchBox via the `RommMenuPlugin` class in `RommPlugin/RommPlugin.cs`. This class implements:

- **`ISystemEventsPlugin`** - Receives system events (startup, shutdown)
- **`IGameLaunchingPlugin`** - Receives game launch/exit events

### Startup Flow

```
LaunchBox startup
  └─→ RommMenuPlugin.OnEventRaised("LaunchBoxStartupCompleted")
       ├── Load settings from settings.json
       ├── Initialize logger
       ├── Initialize DI container (ServiceLocator.Initialize)
       ├── Initialize locale system
       ├── Check for pending updates (GitHub)
       ├── Process pending install/uninstall events
       └── Auto-sync (if interval criteria met)
```

### Sync Flow

```
User clicks "RomM: Sync" (or auto-sync)
  └─→ RommSyncService.SyncAsync()
       ├── Connect to RomM server
       ├── Fetch platforms → PlatformSelectorForm
       ├── For each platform:
       │   ├── Fetch games (paginated, 1000 per page)
       │   ├── Hash-based comparison (skip unchanged)
       │   ├── Pull/push metadata (admin mode)
       │   ├── Download cover art (if missing)
       │   ├── Sync screenshots (bidirectional)
       │   └── Sync play stats (bidirectional)
       ├── Update LaunchBox platform XML files
       ├── Invoke CLI for Parents.xml hierarchy fix
       └── Save resume state
```

## Design Patterns

### Dependency Injection

The project uses `Microsoft.Extensions.DependencyInjection` wrapped in a static `ServiceLocator` class (`RommPlugin.Core/ServiceLocator.cs`).

```csharp
// Initialization (in RommPlugin.cs)
ServiceLocator.Initialize(services =>
{
    services.AddSingleton<IRommApiClient>(apiClient);
    services.AddSingleton<IRommSyncService, RommSyncService>();
    services.AddSingleton<IRommImageService, RommImageService>();
    // ... more registrations
});

// Usage
var syncService = ServiceLocator.GetService<IRommSyncService>();
```

### Interface-Based Design

All major services have interface + implementation pairs:

| Interface | Implementation | Purpose |
|-----------|---------------|---------|
| `IRommApiClient` | `RommApiClient` | HTTP client for RomM API |
| `IRommSyncService` | `RommSyncService` | Core sync engine |
| `IRommImageService` | `RommImageService` | Cover art download |
| `IRommMetadataMapper` | `RommMetadataMapper` | RomM → LaunchBox field mapping |
| `IRommScreenshotSync` | `RommScreenshotSync` | Bidirectional screenshot sync |
| `IRommStatsService` | `RommStatsService` | Play count/time sync |
| `IRommBackupService` | `RommBackupService` | XML backup management |
| `IRommHierarchyCli` | `RommHierarchyCli` | Parents.xml manipulation |
| `IRommResetServerService` | `RommResetServerService` | Delete server metadata |
| `IDownloadQueueService` | `DownloadQueueService` | Download queue management |
| `IInstalledGamesService` | `InstalledGamesService` | Installed games tracking |

### Hash-Based Sync Optimization

Each game's remote metadata is hashed. During sync, if the hash hasn't changed since the last sync, the game is skipped entirely (zero API calls). This reduces API calls from ~24,000 to ~51 for a library of 8,000 games with 50 changes.

### Atomic File Writes

The `SafeFileWriter` helper (`RommPlugin.Core/Helpers/SafeFileWriter.cs`) ensures data integrity by:
1. Writing to a temporary file
2. Copying the temp file to the target location
3. Deleting the temp file

This prevents data corruption if the process crashes mid-write.

### Credential Encryption

Passwords and API tokens are encrypted at rest using Windows DPAPI via `SecureCredentialStorage` (`RommPlugin.Core/Helpers/SecureCredentialStorage.cs`). The encryption is machine-specific and user-specific.

## Key Data Files

| File | Purpose | Managed By |
|------|---------|-----------|
| `settings.json` | Plugin configuration (URL, credentials, behavior) | User / SettingsForm |
| `sync_information.json` | Sync resume state (completed platforms/games) | RommSyncService |
| `installed-games.json` | Persistent install state | InstalledGamesService |
| `download-state.json` | Download queue state (resume after restart) | DownloadQueueService |
| `pending_hierarchy.json` | Pending Parents.xml fixes | RommHierarchyCli |
| `installed-games.xml` | LaunchBox installed games playlist | RommSyncService |

## Image Types

| Constant | Value | Usage |
|----------|-------|-------|
| `ImageTypeBoxFront` | `"Box - Front"` | Primary cover art |
| `ImageTypeFanartBoxFront` | `"Fanart - Box - Front"` | Fan art variant |
| `ImageTypeAdvertisementFlyerFront` | `"Advertisement Flyer - Front"` | Promotional art |
| `ImageTypeScreenshot` | `"Screenshot"` | Game screenshots |

## CLI Tool

`RommPlugin.CLI.exe` is a separate console application invoked by the main plugin for operations that require process isolation:

1. **Parents.xml hierarchy fix** - Manipulates LaunchBox's `Parents.xml` to set correct parent categories for RomM platforms (the LaunchBox API doesn't support this programmatically)
2. **Remove all RomM data** - Cleanup command to remove all synchronized data from LaunchBox

```bash
# Fix hierarchy
RommPlugin.CLI.exe pending_hierarchy.json

# Remove all RomM data
RommPlugin.CLI.exe --remove-all "C:\LaunchBox\Data" [--restart]
```

## Key Constants

Defined in `RommPlugin.Core/Constants/RommConstants.cs`:

| Constant | Value | Description |
|----------|-------|-------------|
| `PlatformPrefix` | `"RomM \| "` | Prefix for RomM platform names |
| `RootCategoryName` | `"RomM"` | Root category in LaunchBox hierarchy |
| `MaxConcurrentDownloads` | `5` | Max simultaneous downloads |
| `ApiPageSize` | `1000` | Games per API page |
| `MaxRetryAttempts` | `5` | Download retry limit |
| `HttpTimeoutSeconds` | `120` | HTTP request timeout |
| `UploadTimeoutSeconds` | `300` | Upload timeout |
| `MaxXmlBackups` | `5` | Max XML backup files |
