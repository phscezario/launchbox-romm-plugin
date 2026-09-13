# Game Manager

The Game Manager (`RommPlugin.UI/Forms/GameManagerForm.cs`) is the dialog that tracks game downloads, installations, and uninstalls. It shows **exactly one row per game**, merging the download queue (`download-state.json`) with the installed-games registry (`installed-games.json`) via `GameManagerRow.Merge()` (`RommPlugin.Core/Models/GameManagerRow.cs`).

## Buttons

| Button | Action |
|--------|--------|
| `Retry` | Retries failed/cancelled downloads. If the local `.zip` exists but is unreadable, it is discarded and a full re-download starts instead of reprocessing corrupt bytes. Re-enqueues uninstalled games. |
| `Cancel` / `Cancel All` | Cancels active or pending downloads. |
| `Uninstall` / `Uninstall All` | Deletes the game files, clears LaunchBox metadata, and marks the record uninstalled. Leftover staging artifacts of that game are cleaned up automatically. |
| `Clear` | **Always enabled.** Clears completed/failed/cancelled queue entries, purges uninstalled records, and runs the full orphan sweep (below). No confirmation dialog: it only removes plugin-owned leftovers, never user data. |

## Installed-flags repair (automatic on startup and on open)

Records are reconciled with the LaunchBox-side fields (`ApplicationPath` / `Installed`) at LaunchBox startup (`RommMenuPlugin.OnEventRaised`, before auto-sync) and every time the Game Manager opens. Both call the shared `InstallFlagRepairRunner.RepairAll()` (`RommPlugin.UI/Helpers/InstallFlagRepairRunner.cs`), decided per record by `InstallFlagRepairDecider` (`RommPlugin.Core/Services/InstallFlagRepairDecider.cs`). Silent, no setting, never blocks startup:

- record active and files present on disk, but LaunchBox fields wrong or empty → fields are rewritten from the record (silent, logged as `[Repair]`);
- record active but files gone from disk → the game is re-queued for download using the record's own location data;
- game no longer in LaunchBox → logged and skipped.

This heals "phantom installs" (records marked installed whose install actually failed before the LaunchBox fields were written).

## Orphan cleanup

Failed or partial install/uninstall attempts must never leave trash behind (stray extracted files, half-created folders, stale temp dirs). Cleanup runs in two modes, both implemented by `RommOrphanCleanupService` (`RommPlugin.Core/Services/RommOrphanCleanupService.cs`, helpers in `RommPlugin.Core/Helpers/RommArchiveHelper.cs`):

### Scoped mode (per game, automatic and silent)

Runs on **every install** (success and failure paths in `RommProcessInstallUninstallService` and `GameManagerForm.AutoInstallAsync`) and on **every uninstall** (all four uninstall flows in `GameManagerForm`). Only artifacts attributable to that game are removed:

- files copied by the failed attempt (tracked during flatten extraction),
- the sibling extract folder, but only when the attempt created it,
- that game's `.part` file,
- stale `_temp_*` staging dirs next to the game's files.

### Full mode (Clear button)

`BtnClear_Click` runs `CleanFull()` after the list cleanup. Conservative on purpose — only unmistakable plugin-owned patterns:

- `_temp_<32 hex>` staging dirs under the ROMs root (strict name match, older than 15 minutes),
- `*.part` files with no matching queue entry (older than 15 minutes),
- abandoned `download-state.*.tmp` files in the plugin folder.

Game folders and ROMs without an owner are never deleted automatically.

### Safety rules (both modes)

- Everything is contained under the ROMs root (`RommArchiveHelper.IsUnderRoot`) — cleanup can never escape it.
- A file that belongs to another game (`InstalledPath`/`FilePath`) is never touched.
- Every pass logs what it removed (`[Cleanup] ... removed N leftover(s): ...`).

## Corrupt archives

A truncated download produces a zip whose central directory cannot be read (`InvalidDataException: End of Central Directory record could not be found`). Reprocessing the same bytes can never succeed, so:

1. The unreadable zip is deleted together with the attempt's leftovers.
2. The item goes back to `Pending` for an automatic re-download, up to `MaxCorruptArchiveRedownloads` (`3`, see `RommPlugin.Core/Constants/RommConstants.cs`).
3. After the cap, the item is marked `Failed` with the message `Archive corrupt after 3 downloads`.
4. Manual retries follow the same rule (`GameManagerForm.RetrySmart`): an unreadable local `.zip` is discarded and re-downloaded instead of reprocessed.

The failure is logged with game context (`GameId`, title, zip path), e.g. `[Install] Game 1110 '...' zip='...': archive is unreadable (corrupt/truncated)`.

## Atomic extraction

- Folder games (`UnzipAndDelete`) extract into a `<target>._tmp_<guid>` staging dir and are published with an atomic `Directory.Move` on success; the staging dir is removed on failure.
- Single-file games (`UnzipAndFlatten`) track every copied file and remove them if anything fails before the source zip is deleted. Zip deletion retries a few times to survive transient antivirus/Explorer locks.

## Related files

| File | Purpose |
|------|---------|
| `download-state.json` | Queue state (statuses: `Pending`, `Downloading`, `WaitingInstall`, `Installed`, `Failed`, `Cancelled`, `Completed`, `WaitingUninstall`) |
| `installed-games.json` | Installed-games registry (source of truth for the installed list) |
| `CorruptArchiveAttempts` | Per-item counter of corrupt-archive re-downloads (reset on fresh enqueue) |
