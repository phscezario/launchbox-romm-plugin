# Folder Games (`_launchbox.json`)

Per-game LaunchBox integration for folder-based ROMs. The plugin reads
`_launchbox.json` from the extracted game folder during install
(`RommProcessInstallUninstallService.ConfigureLaunchBoxGame`) and applies it.
Re-syncs converge to the same state: DLC entries are upserted by path, so
flag flips and re-runs never accumulate stale entries.

## Fields

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `DefaultFileName` | string | — | File launched by default, relative to the game folder (e.g. `shortcuts/Game.lnk`). **Empty means the folder itself is the game reference**: `ApplicationPath` points at the folder (e.g. PS3 disc folders booted in place by an emulator wrapper). |
| `HasDLC` | bool | `false` | `true` = every file under `_DLCs/` becomes an additional application (`DLC 1`, `DLC 2`, …). |
| `DlcUseEmulator` | bool | `false` | Opt-in: route `_DLCs` entries **and** config `AdditionalApplications` through the game's emulator (`UseEmulator` + `EmulatorId` + `WaitForExit`) instead of launching files directly. Required when the emulator (or a wrapper) must resolve the file to the game folder first — e.g. PS3 `.pkg` files, which RPCS3 refuses to install in `--no-gui` mode. Leave `false` for platforms whose DLC files must not boot as games. Pre/post loaders always launch directly. |
| `AdditionalApplications` | list | `[]` | `{ "Name", "Path" (relative to folder), "CommandLine" }` — game content entries. |
| `PreLoaders` / `PosLoaders` | list | `[]` | Same schema plus `WaitForExit`, `FromLaunchBoxRoot`. Run before/after the game; never routed via emulator. |

## Shortcuts cleanup

When `DefaultFileName` is empty and no config entry points under `shortcuts/`,
the obsolete `shortcuts/` folder is deleted after install (those `.lnk` files
are unreferenced and some ship with broken encoding). If anything still
references `shortcuts/`, the folder is kept.

## PS3 example (disc + PSN)

```json
{
    "DefaultFileName": "",
    "HasDLC": true,
    "DlcUseEmulator": true,
    "AdditionalApplications": []
}
```

```json
{
    "DefaultFileName": "",
    "HasDLC": false,
    "DlcUseEmulator": true,
    "AdditionalApplications": [
        { "Name": "Game", "Path": "Games for Installations\\Tekken Revolution\\Tekken Revolution.pkg" },
        { "Name": "Game complement", "Path": "Games for Installations\\Tekken Revolution\\UP0700-NPUB31250_00-TEKKENREVOLUTION.rap" }
    ]
}
```

With `DlcUseEmulator: true`, clicking any entry falls through to the emulator
(or its wrapper), which resolves the file to the game root and boots the
installed/disc `EBOOT.BIN` — the `Cannot perform installation in no-gui mode!`
dialog can no longer trigger.
