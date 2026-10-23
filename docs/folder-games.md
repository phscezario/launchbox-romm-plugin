# Folder Games (`_launchbox.json`)

Per-game LaunchBox integration for folder-based ROMs. The plugin reads
`_launchbox.json` from the extracted game folder during install
(`RommProcessInstallUninstallService.ConfigureLaunchBoxGame`). Installing
DLC content itself is the emulator's job; the plugin only wires how
entries launch. On uninstall, DLC entries are removed along with the
config-listed ones, even after the `_DLCs` folder is gone.

## Fields

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `DefaultFileName` | string | — | File launched by default, relative to the game folder (e.g. `shortcuts/Game.lnk`). **Empty means the folder itself is the game reference**: `ApplicationPath` points at the folder (e.g. PS3 disc folders booted in place by an emulator wrapper). |
| `HasDLC` | bool | `false` | `true` = every file under `_DLCs/` becomes an additional application (`DLC 1`, `DLC 2`, …). |
| `DlcUseEmulator` | bool | `false` | When `true`, `_DLCs` entries and config `AdditionalApplications` launch through the game's emulator (`UseEmulator` + `EmulatorId` + `WaitForExit`) instead of running files directly. The emulator (or its wrapper) owns DLC installation — e.g. PS3 `.pkg` files, which RPCS3 refuses to install in `--no-gui` mode, so clicking an entry boots the game and the wrapper installs everything at once. Pre/post loaders always launch directly. |
| `AdditionalApplications` | list | `[]` | `{ "Name", "Path" (relative to folder), "CommandLine" }` — game content entries. |
| `PreLoaders` / `PosLoaders` | list | `[]` | Same schema plus `WaitForExit`, `FromLaunchBoxRoot`. Run before/after the game; never routed via emulator. |

## PS3 example

```json
{
    "DefaultFileName": "",
    "HasDLC": true,
    "DlcUseEmulator": true,
    "AdditionalApplications": []
}
```

With `DlcUseEmulator: true`, clicking any entry falls through to the emulator
(or its wrapper), which resolves the file to the game root — the
`Cannot perform installation in no-gui mode!` dialog can no longer trigger.
Entries created direct by older versions are aligned to the routed form on
the next install/sync. On uninstall, DLC entries are removed along with the
config-listed ones, even after the `_DLCs` folder is gone (this includes
manually added entries pointing under `_DLCs`). The startup repair
also creates missing records for valid installs found on disk, so a game is
never stuck showing uninstalled while its files are in place.
