# Building the LaunchBox RomM Plugin

This document describes how to build the plugin from source. The build mirrors the
automated release pipeline in [`.github/workflows/release.yml`](.github/workflows/release.yml).

## Prerequisites

- **Windows** (the plugin targets .NET Framework and references LaunchBox/WinForms assemblies).
- **.NET Framework 4.8 Developer Pack** — all projects target `v4.8`.
- **MSBuild** — via one of:
  - Visual Studio 2022 (with the ".NET desktop development" workload), or
  - Build Tools for Visual Studio 2022 (`microsoft/setup-msbuild` in CI).
- **NuGet CLI** (`nuget.exe`) for package restore of the classic `packages.config` projects.

> The project uses the newer `.slnx` solution format, so use a recent MSBuild/Visual Studio 2022
> that understands `.slnx`.

### Install prerequisites (winget)

From an elevated PowerShell (Build Tools require admin):

```powershell
# .NET Framework 4.8 Developer Pack
winget install -e --id Microsoft.DotNet.Framework.DeveloperPack_4 --version 4.8

# MSBuild and .NET desktop build tools (headless; matches CI)
winget install -e --id Microsoft.VisualStudio.2022.BuildTools `
  --override "--passive --wait --add Microsoft.VisualStudio.Workload.ManagedDesktopBuildTools --includeRecommended"

# NuGet CLI for packages.config restore
winget install -e --id Microsoft.NuGet
```

Restart your terminal after installing. Visual Studio and Build Tools install MSBuild but do **not** add it to your regular terminal PATH.

### Configure your shell (PATH)

Run this once per terminal session before building:

```powershell
$msbuild = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" `
  -latest -requires Microsoft.Component.MSBuild `
  -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1
$env:Path = "$(Split-Path -Parent $msbuild);$env:Path"

msbuild -version
```

Alternatively, open **Developer PowerShell for VS 2022** from the Start menu (PATH is preconfigured there).

If `nuget` is also not recognized, ensure `%LOCALAPPDATA%\Microsoft\WinGet\Links` is on your user PATH (winget adds this on install) and restart the terminal, or invoke it directly:

```powershell
& "$env:LOCALAPPDATA\Microsoft\WinGet\Links\nuget.exe" help
```

Alternatively, install Visual Studio 2022 Community with the **".NET desktop development"** workload instead of Build Tools:

```powershell
winget install -e --id Microsoft.VisualStudio.2022.Community `
  --override "--passive --wait --add Microsoft.VisualStudio.Workload.ManagedDesktop --includeRecommended"
winget install -e --id Microsoft.NuGet
```

## Solution layout

| Project | Output | Purpose |
|---|---|---|
| [`RommPlugin.Core`](RommPlugin.Core/RommPlugin.Core.csproj) | Library | Shared models, storage, logging, and services (e.g. `RommConnectionTester`). |
| [`RommPlugin.UI`](RommPlugin.UI/RommPlugin.UI.csproj) | Library | WinForms UI (settings, login, progress, platform selector). References `Core`. |
| [`RommPlugin`](RommPlugin/RommPlugin.csproj) | Library | The LaunchBox plugin itself (menu items, API client, sync services). References `Core` and `UI`. |
| [`RommPlugin.CLI`](RommPlugin.CLI/RommPlugin.CLI.csproj) | WinExe | Headless installer/uninstaller invoked by the plugin. References all of the above. |

Reference direction: `RommPlugin` -> `RommPlugin.UI` -> `RommPlugin.Core`. Because of this,
`RommPlugin.UI` cannot reference `RommPlugin` (which is why connection-testing lives in `Core`).

The LaunchBox SDK is referenced from a checked-in assembly at
[`lib/LaunchBox/Unbroken.LaunchBox.Plugins.dll`](lib/LaunchBox/Unbroken.LaunchBox.Plugins.dll).

## Build (command line)

From the repository root, configure PATH (see [Configure your shell](#configure-your-shell-path)), then:

```powershell
nuget restore LaunchBoxRommPlugin.slnx
msbuild LaunchBoxRommPlugin.slnx /p:Configuration=Release
```

For a debug build, use `/p:Configuration=Debug`.

Building `RommPlugin.CLI` transitively builds every other project, so all output DLLs land in
`RommPlugin.CLI\bin\Release\` (or `bin\Debug\`).

## Build (Visual Studio)

1. Open `LaunchBoxRommPlugin.slnx` in Visual Studio 2022.
2. Restore NuGet packages (automatic on first build, or right-click the solution -> Restore).
3. Select the `Release` configuration and build the solution (Ctrl+Shift+B).

## Packaging (matching the release workflow)

The release job assembles a `RomM LaunchBox Integration` folder from the CLI output and zips it.
Reproduce locally after a Release build:

```bat
mkdir "package\RomM LaunchBox Integration"

copy RommPlugin.CLI\bin\Release\Newtonsoft.Json.*  "package\RomM LaunchBox Integration\"
copy RommPlugin.CLI\bin\Release\RommPlugin.CLI.*    "package\RomM LaunchBox Integration\"
copy RommPlugin.CLI\bin\Release\RommPlugin.Core.*   "package\RomM LaunchBox Integration\"
copy RommPlugin.CLI\bin\Release\RommPlugin.*        "package\RomM LaunchBox Integration\"
copy RommPlugin.CLI\bin\Release\RommPlugin.UI.*     "package\RomM LaunchBox Integration\"

xcopy Images "package\RomM LaunchBox Integration\Images\" /E /I /Y
```

Then compress `package\*` into a zip (the CI uses PowerShell `Compress-Archive`).

## Installing a local build for testing

Copy the packaged `RomM LaunchBox Integration` folder into your LaunchBox `Plugins` directory:

```text
LaunchBox
 └── Plugins
      └── RomM LaunchBox Integration
```

Then start LaunchBox/BigBox; the RomM menu items appear automatically. Configure the server via
`RomM: Configurations` (or by editing `settings.json` in the plugin folder). See
[`settings.json`](settings.json) for the available keys, including `ClientApiToken`.

## Notes

- The projects use classic `packages.config` NuGet restore (not PackageReference), so a NuGet
  restore step is required before the first MSBuild.
- This repository builds only on Windows; macOS/Linux lack the required .NET Framework and LaunchBox
  assemblies.
