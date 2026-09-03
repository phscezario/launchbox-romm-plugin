# Deployment

This guide covers building the plugin for distribution, package structure, and installation methods.

## Building for Release

### Automated (Recommended)

The release process is fully automated via GitHub Actions:

1. Create a PR with a conventional commit title (e.g., `feat: add new feature`)
2. Merge the PR to `master`
3. Auto-tag creates a version tag based on PR title
4. Release workflow builds, packages, and creates a GitHub Release

See [Release Process](release-process.md) for details.

### Manual Build

```bash
# Restore packages
nuget restore LaunchBoxRommPlugin.slnx

# Build Release
msbuild LaunchBoxRommPlugin.slnx /p:Configuration=Release
```

Output is in `build/Release/`.

## Package Structure

The release ZIP contains:

```
launchbox-romm-plugin-{version}.zip
└── RomM LaunchBox Integration/
    ├── RommPlugin.dll              # Main plugin (loaded by LaunchBox)
    ├── RommPlugin.dll.config       # Assembly binding redirects
    ├── RommPlugin.Core.dll         # Shared core library
    ├── RommPlugin.UI.dll           # Windows Forms UI
    ├── RommPlugin.CLI.exe          # Command-line tool
    ├── RommPlugin.CLI.exe.config   # CLI binding redirects
    ├── Newtonsoft.Json.dll         # JSON dependency
    ├── Images/
    │   ├── ico.ico                 # Plugin icon (ICO)
    │   ├── ico.png                 # Plugin icon (PNG, used in menus)
    │   ├── Installed.png           # Installed game icon
    │   └── Installed Games.png     # Installed games playlist icon
    └── Locales/
        ├── en.json                 # English strings
        └── pt-BR.json              # Portuguese (Brazil) strings
```

## Installation

### Method 1: GitHub Release (Recommended)

1. Download the latest ZIP from [GitHub Releases](https://github.com/phscezario/launchbox-romm-plugin/releases)
2. Extract the ZIP
3. Copy the `RomM LaunchBox Integration` folder into `LaunchBox/Plugins/`
4. Start LaunchBox

### Method 2: Manual Installation

1. Build the solution (see above)
2. Create `RomM LaunchBox Integration` folder in `LaunchBox/Plugins/`
3. Copy from `build/Release/`:
   - `RommPlugin.dll` + `RommPlugin.dll.config`
   - `RommPlugin.Core.dll`
   - `RommPlugin.UI.dll`
   - `RommPlugin.CLI.exe` + `RommPlugin.CLI.exe.config`
   - `Newtonsoft.Json.dll`
4. Copy `Images/` from source root
5. Copy `Locales/` from `build/Release/Locales/`
6. Create `settings.json` (copy from `settings.example.json`)
7. Configure your RomM server URL and credentials
8. Start LaunchBox

### Expected Directory Structure

```
LaunchBox/
├── Data/
├── Images/
├── Plugins/
│   └── RomM LaunchBox Integration/
│       ├── RommPlugin.dll
│       ├── RommPlugin.Core.dll
│       ├── RommPlugin.UI.dll
│       ├── RommPlugin.CLI.exe
│       ├── Newtonsoft.Json.dll
│       ├── settings.json
│       ├── Images/
│       ├── Locales/
│       ├── Logs/
│       ├── installed-games.json
│       ├── download-state.json
│       └── sync_information.json
└── LaunchBox.exe
```

## Auto-Update

The plugin includes a built-in auto-updater:

1. On startup, checks GitHub Releases for the latest version
2. If a newer version is available, downloads the ZIP
3. Extracts files to a temporary location
4. Creates a batch script to copy files
5. Restarts LaunchBox to apply the update

**Settings:**
- `AutoUpdateEnabled` - Enable/disable auto-update checks (default: `true`)

**How it works:**
- Uses `GitHubUpdateService` to query `api.github.com/repos/phscezario/launchbox-romm-plugin/releases/latest`
- Compares versions using semantic versioning
- Downloads ZIP using `DownloadQueueService`
- Applies update via `UpdateInstaller` (batch script + restart)

## Runtime Data Files

After installation, the plugin creates these files in the plugin folder:

| File | Purpose | Created When |
|------|---------|-------------|
| `settings.json` | User configuration | First run / manual creation |
| `installed-games.json` | Installed games tracking | First sync |
| `download-state.json` | Download queue state | First download |
| `sync_information.json` | Sync resume state | First sync |
| `pending_hierarchy.json` | Pending Parents.xml fixes | After sync |
| `Logs/romm-YYYY-MM-DD.log` | Daily log files | When `SaveLogs` enabled |

## Uninstalling

1. Close LaunchBox
2. Delete the `RomM LaunchBox Integration` folder from `LaunchBox/Plugins/`
3. Optionally delete runtime data files if you want a clean slate

**Note:** Uninstalling does not remove games installed via the plugin. Games are installed to the path specified in `RomsPath`.

## Requirements

| Requirement | Details |
|-------------|---------|
| LaunchBox / BigBox | Latest version recommended |
| RomM Server | Self-hosted, accessible via HTTP/HTTPS |
| Windows | 10/11 (required for .NET Framework 4.8) |
| Network | Access to RomM server |
| Disk Space | Depends on game library size |
