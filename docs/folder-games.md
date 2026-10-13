# Folder Games (`_launchbox.json`)

Per-game LaunchBox integration for folder-based ROMs. The plugin reads
`_launchbox.json` from the extracted game folder during install
(`RommProcessInstallUninstallService.ConfigureLaunchBoxGame`) and writes plain
LaunchBox data. Interpreting that data (emulator routing, wrappers) is the job
of emulator-side tooling, not the sync.

## Fields

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `DefaultFileName` | string | — | File launched by default, relative to the game folder (e.g. `shortcuts/Game.lnk`). **Empty means the folder itself is the game reference**: `ApplicationPath` points at the folder (e.g. PS3 disc folders booted in place by an emulator wrapper). |
| `HasDLC` | bool | `false` | `true` = every file under `_DLCs/` becomes an additional application (`DLC 1`, `DLC 2`, …), launched directly. |
| `DlcUseEmulator` | bool | `false` | Hint for emulator wrapper tooling: when `true`, `_DLCs` entries and config `AdditionalApplications` should be routed through the game's emulator instead of running files directly (e.g. PS3 `.pkg` files, which RPCS3 refuses to install in `--no-gui` mode). The sync itself always writes direct entries. |
| `AdditionalApplications` | list | `[]` | `{ "Name", "Path" (relative to folder), "CommandLine" }` — game content entries, written as-is. |
| `PreLoaders` / `PosLoaders` | list | `[]` | Same schema plus `WaitForExit`, `FromLaunchBoxRoot`. Run before/after the game. |

## PS3 example

```json
{
    "DefaultFileName": "",
    "HasDLC": true,
    "DlcUseEmulator": true,
    "AdditionalApplications": []
}
```
