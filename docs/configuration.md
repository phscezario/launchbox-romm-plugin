# Configuration Reference

All plugin settings are stored in `settings.json` inside the plugin folder. A template is provided in `settings.example.json`.

## Settings Fields

### Connection

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `RommBaseUrl` | string | `""` | RomM server URL (e.g., `http://192.168.1.100:9000`) |
| `Username` | string | `""` | RomM username |
| `Password` | string | `""` | RomM password (encrypted at rest via DPAPI) |
| `ClientApiToken` | string | `""` | RomM API token (`rmm_...`). If set, takes priority over username/password |

### Storage

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `RomsPath` | string | `""` | Local folder where games will be installed |

### Sync Behavior

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `KeepLocalData` | bool | `false` | `true` = only fill empty fields, preserve existing data. `false` = overwrite all fields with server data |
| `IsAdmin` | bool | `false` | `true` = bidirectional sync (pull + push). `false` = pull only (screenshots always bidirectional) |
| `ForcePushToServer` | bool | `false` | Admin only. Push all local metadata, artwork, and screenshots to server, overwriting remote data |
| `ForceFullResync` | bool | `false` | Clear resume state and reprocess all platforms on next sync (non-destructive) |
| `AutoSyncEnabled` | bool | `false` | Enable automatic sync on startup |
| `AutoSyncIntervalDays` | int | `0` | Auto sync interval: `-1` = disabled, `0` = every startup, `N` = every N days |
| `SaveBatchSize` | int | `50` | Games per save batch during sync |

### Gameplay Tracking

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `UpdateStatsOnGameLaunch` | bool | `false` | Sync play count/time on game launch/exit |
| `PublicScreenshots` | bool | `true` | Uploaded screenshots visible to all RomM users |

### Installation

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `ProcessPendingOnStartup` | bool | `true` | Auto-process pending install/uninstall events on LaunchBox startup |

### Logging

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `SaveLogs` | bool | `false` | Enable file logging |
| `DetailedSyncLogs` | bool | `false` | Log detailed timing for each sync operation |
| `LogRetentionDays` | int | `7` | Days to keep log files before automatic deletion |

### UI

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `Language` | string | `"en"` | UI language code (`"en"` or `"pt-BR"`) |

### Auto-Update

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `AutoUpdateEnabled` | bool | `true` | Check for GitHub updates on startup |

### Internal (Auto-Managed)

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `LastSelectedPlatformIds` | List\<int\> | `[]` | Remembered platform selection for sync |
| `LastAutoSyncAt` | DateTime? | `null` | Timestamp of last auto sync |
| `CurrentPlatforms` | List | `[]` | Known RomM platforms |
| `SuppressNeedsProcess` | bool | `false` | Suppress pending process notification |
| `LoginFormUseConfiguredAccount` | bool | `false` | Use configured account in login form |
| `LoginFormSaveAdminAccount` | bool | `false` | Save admin account from login form |

## Authentication

### Client API Token (Recommended)

More secure than username/password. Generate in RomM under **Administration → Client API Tokens**.

- Format: `rmm_` + 64 hex characters
- Sent as: `Authorization: Bearer rmm_...`
- **Takes priority** over username/password when both are provided

### Username/Password

- Sent as: `Authorization: Basic base64(username:password)`
- Encrypted at rest using Windows DPAPI (machine + user specific)

## Settings File Location

The plugin looks for `settings.json` in:

```
LaunchBox/Plugins/RomM LaunchBox Integration/settings.json
```

The path is resolved by `RommPaths.SettingsFile` (`RommPlugin.Core/Storage/RommPaths.cs`).

## Encryption

Credentials (`Password`, `ClientApiToken`) are encrypted when saved via the Settings UI using `SecureCredentialStorage` (`RommPlugin.Core/Helpers/SecureCredentialStorage.cs`). This uses Windows DPAPI:

- Encryption is **machine-specific** - settings cannot be copied between machines
- Encryption is **user-specific** - different Windows users cannot read each other's credentials
- If `settings.json` is copied manually, credentials will be unreadable

## Example Settings

```json
{
  "RommBaseUrl": "http://192.168.1.100:9000",
  "Username": "admin",
  "Password": "",
  "ClientApiToken": "rmm_abc123...",
  "RomsPath": "D:\\Roms",
  "KeepLocalData": false,
  "IsAdmin": true,
  "AutoSyncEnabled": true,
  "AutoSyncIntervalDays": 0,
  "UpdateStatsOnGameLaunch": true,
  "PublicScreenshots": true,
  "Language": "en",
  "SaveLogs": true,
  "DetailedSyncLogs": false,
  "LogRetentionDays": 14,
  "ProcessPendingOnStartup": true,
  "ForceFullResync": false,
  "ForcePushToServer": false,
  "AutoUpdateEnabled": true
}
```

## Sync State File

Sync progress is stored separately in `sync_information.json`:

| Field | Type | Description |
|-------|------|-------------|
| `SyncInProgress` | bool | `true` when a sync was interrupted |
| `CompletedPlatformIds` | List\<int\> | Platform IDs fully processed |
| `CompletedGameIdsByPlatform` | Dictionary | Per-platform game IDs already processed |
| `UnselectedPlatformIds` | List\<int\> | Platform IDs deselected by user |
| `CurrentPlatforms` | List | All known RomM platforms |

This file is managed automatically. Do not edit manually unless you understand the implications.

## Installed Games File

Installation state is stored in `installed-games.json`:

- Tracks which games are installed locally
- Survives LaunchBox data resets
- Used for uninstall actions and recovery scanning
