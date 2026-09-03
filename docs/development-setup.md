# Development Setup

This guide covers everything you need to set up a development environment for the LaunchBox RomM Plugin.

## Prerequisites

| Requirement | Version | Notes |
|-------------|---------|-------|
| **Windows** | 10/11 | Required (LaunchBox is Windows-only) |
| **Visual Studio** | 2022+ | Community edition works |
| **.NET Framework 4.8 SDK** | - | Required for all projects |
| **.NET 8 SDK** | 8.0+ | Required for running tests via `dotnet test` |
| **NuGet CLI** | Latest | For package restore |
| **Git** | Latest | For version control |

### Visual Studio Workloads

Ensure these workloads are installed:

- **.NET desktop development** (Windows Forms support)
- **.NET Framework 4.8 targeting pack**

## Cloning the Repository

```bash
git clone https://github.com/phscezario/launchbox-romm-plugin.git
cd launchbox-romm-plugin
```

## Opening in Visual Studio

1. Open `LaunchBoxRommPlugin.slnx` in Visual Studio 2022+
2. Visual Studio will automatically detect the 5 projects
3. NuGet restore may run automatically; if not, right-click solution → Restore NuGet Packages

## Building

### From Visual Studio

- Set configuration to **Debug** or **Release** in the toolbar
- Build → Build Solution (Ctrl+Shift+B)

### From Command Line

```bash
# Restore packages
nuget restore LaunchBoxRommPlugin.slnx

# Build (Release)
msbuild LaunchBoxRommPlugin.slnx /p:Configuration=Release

# Build (Debug)
msbuild LaunchBoxRommPlugin.slnx /p:Configuration=Debug
```

### Build Output

All projects output to a shared `build/` directory:

```
build/
├── Debug/
│   ├── RommPlugin.dll
│   ├── RommPlugin.Core.dll
│   ├── RommPlugin.UI.dll
│   ├── RommPlugin.CLI.exe
│   ├── Newtonsoft.Json.dll
│   ├── Locales/
│   │   ├── en.json
│   │   └── pt-BR.json
│   └── ...
└── Release/
    └── (same structure)
```

The `Directory.Build.props` file configures all projects to output to `build\$(Configuration)\` with `AppendTargetFrameworkToOutputPath=false` (no `net48` subfolder).

## Project Configuration

### Versioning

The version is read from the `VERSION` file at build time via the `SetGitVersion` MSBuild target in `Directory.Build.props`. The version is injected into assembly attributes (`AssemblyVersion`, `AssemblyFileVersion`, `AssemblyInformationalVersion`).

### LaunchBox Plugin SDK

The `Unbroken.LaunchBox.Plugins.dll` is vendored in `lib/LaunchBox/`. This is the official plugin SDK provided by LaunchBox. It's referenced directly (not via NuGet) since LaunchBox doesn't publish a NuGet package.

### Assembly Binding Redirects

Some projects include `app.config` files with assembly binding redirects (e.g., for `System.Drawing.Common`). These are important for runtime compatibility.

## Running the Plugin Locally

To test the plugin locally:

1. Build the solution in Debug mode
2. Copy the following from `build/Debug/` to your LaunchBox installation:
   ```
   LaunchBox/Plugins/RomM LaunchBox Integration/
   ├── RommPlugin.dll
   ├── RommPlugin.dll.config
   ├── RommPlugin.Core.dll
   ├── RommPlugin.UI.dll
   ├── RommPlugin.CLI.exe
   ├── RommPlugin.CLI.exe.config
   ├── Newtonsoft.Json.dll
   ├── Images/           (copy from source)
   └── Locales/          (from build output)
   ```
3. Create a `settings.json` in the plugin folder (copy from `settings.example.json`)
4. Configure your RomM server URL and credentials
5. Start LaunchBox normally

## Debugging

### From Visual Studio

1. Set `RommPlugin` as the startup project
2. Configure launch profile to launch LaunchBox:
   - Set **Start Action** to "Start external program"
   - Path: `C:\Path\To\LaunchBox\LaunchBox.exe`
3. Set breakpoints as needed
4. Press F5 to start debugging

### Debug Logging

Enable detailed logging in `settings.json`:

```json
{
  "SaveLogs": true,
  "DetailedSyncLogs": true,
  "LogRetentionDays": 30
}
```

Logs are written to `Logs/romm-YYYY-MM-DD.log` inside the plugin folder.

## Code Style

The project follows the `.editorconfig` rules:

| Rule | Value |
|------|-------|
| Indentation | 4 spaces (C#), 2 spaces (JSON/YAML/XML) |
| Line endings | LF |
| Charset | UTF-8 |
| Braces | Allman style (new line before opening brace) |
| `this.` qualifier | Never (warning) |

## Useful Scripts

### Sync-LaunchBoxMetadata.ps1

A PowerShell utility at the repository root that syncs metadata from a backup LaunchBox data folder to the current one. Useful for recovering metadata after a data reset.

```powershell
.\Sync-LaunchBoxMetadata.ps1 `
  -BackupDataPath "D:\Jogos\LaunchBox\Data - Copia\Platforms" `
  -CurrentDataPath "D:\Jogos\LaunchBox\Data\Platforms" `
  -BackupImagesPath "D:\Jogos\LaunchBox\Data - Copia\Images" `
  -CurrentImagesPath "D:\Jogos\LaunchBox\Images"
```

## Troubleshooting

### Build fails with "Unbroken.LaunchBox.Plugins.dll not found"

Ensure the `lib/LaunchBox/` folder exists and contains the DLL. It's checked into the repository.

### Tests fail to run

Make sure you have .NET 8 SDK installed alongside .NET Framework 4.8. The test runner uses `dotnet test` which requires the .NET SDK.

### Plugin doesn't appear in LaunchBox

- Verify the plugin folder is directly under `LaunchBox/Plugins/` (not nested)
- Check that `RommPlugin.dll` is in the plugin folder
- Check LaunchBox logs for plugin loading errors
