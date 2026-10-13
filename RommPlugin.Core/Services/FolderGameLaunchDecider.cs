using System.Collections.Generic;
using System.IO;
using System.Linq;
using RommPlugin.Core.Constants;
using RommPlugin.Core.Models;

namespace RommPlugin.Core.Services
{
    /// <summary>
    /// Planned LaunchBox-side launch configuration for a folder game.
    /// Pure data so it can be unit tested without LaunchBox types.
    /// </summary>
    public sealed class FolderGameLaunchPlan
    {
        /// <summary>ApplicationPath the game must point to (null = leave untouched).</summary>
        public string ApplicationPath { get; set; }

        /// <summary>Desired DLC entries (one per file found under _DLCs).</summary>
        public List<PlannedDlcEntry> DlcEntries { get; set; } = new List<PlannedDlcEntry>();
    }

    /// <summary>Desired state of a single _DLCs additional application.</summary>
    public sealed class PlannedDlcEntry
    {
        public string ApplicationPath { get; set; }
        public string Name { get; set; }
        public bool UseEmulator { get; set; }
        public string EmulatorId { get; set; }
        public bool WaitForExit { get; set; }
    }

    /// <summary>
    /// Pure decision logic mapping a folder game's _launchbox.json config to the
    /// LaunchBox fields the plugin must write. UI-agnostic and free of LaunchBox
    /// types so it can be unit tested.
    /// </summary>
    public static class FolderGameLaunchDecider
    {
        /// <param name="baseFolder">Extracted game folder (folder reference).</param>
        /// <param name="config">Parsed _launchbox.json (may be null).</param>
        /// <param name="gameEmulatorId">The game's assigned emulator id (may be null).</param>
        /// <param name="dlcFiles">Files found under the _DLCs folder (may be null).</param>
        public static FolderGameLaunchPlan Decide(
            string baseFolder,
            LaunchBoxFolderGameConfig config,
            string gameEmulatorId,
            IEnumerable<string> dlcFiles)
        {
            var plan = new FolderGameLaunchPlan();

            if (config == null || string.IsNullOrWhiteSpace(baseFolder))
                return plan;

            // Empty DefaultFileName means the folder itself is the game reference
            // (e.g. PS3 disc folders booted in place by an emulator wrapper).
            plan.ApplicationPath = string.IsNullOrWhiteSpace(config.DefaultFileName)
                ? Path.GetFullPath(baseFolder)
                : Path.GetFullPath(Path.Combine(baseFolder, config.DefaultFileName));

            if (config.HasDLC != true || dlcFiles == null)
                return plan;

            // Route DLCs through the game's emulator only on explicit opt-in:
            // booting arbitrary DLC files as games is wrong for most platforms.
            bool viaEmulator = config.DlcUseEmulator == true && !string.IsNullOrWhiteSpace(gameEmulatorId);

            int index = 1;
            foreach (var file in dlcFiles.OrderBy(f => f))
            {
                plan.DlcEntries.Add(new PlannedDlcEntry
                {
                    ApplicationPath = file,
                    Name = string.Format("{0}DLC {1}", RommConstants.PlatformPrefix, index),
                    UseEmulator = viaEmulator,
                    EmulatorId = viaEmulator ? gameEmulatorId : null,
                    WaitForExit = viaEmulator,
                });
                index++;
            }

            return plan;
        }
    }
}
