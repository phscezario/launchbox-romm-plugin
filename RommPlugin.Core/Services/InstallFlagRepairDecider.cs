using System.IO;
using RommPlugin.Core.Models;

namespace RommPlugin.Core.Services
{
    /// <summary>
    /// Decision of the installed-flags repair for a single record.
    /// </summary>
    public sealed class InstallFlagRepairDecision
    {
        /// <summary>Whether the LaunchBox game fields need to be rewritten.</summary>
        public bool RepairFields { get; set; }

        /// <summary>Whether ApplicationPath must be reset to the record's InstalledPath.</summary>
        public bool UpdateApplicationPath { get; set; }

        /// <summary>Whether the game files are gone and a re-download must be queued.</summary>
        public bool RequeueDownload { get; set; }

        /// <summary>Whether any action is required at all.</summary>
        public bool NeedsAction => RepairFields || RequeueDownload;
    }

    /// <summary>
    /// Pure decision logic for reconciling an installed-games record with the
    /// LaunchBox-side fields (ApplicationPath / Installed flag). UI-agnostic and
    /// free of LaunchBox types so it can be unit tested.
    /// </summary>
    public static class InstallFlagRepairDecider
    {
        /// <summary>
        /// Decides what (if anything) must be done for <paramref name="record"/>.
        /// </summary>
        /// <param name="record">The installed-games record (active ones only are actionable).</param>
        /// <param name="currentAppPath">The LaunchBox game's current ApplicationPath (may be null).</param>
        /// <param name="currentInstalled">The LaunchBox game's current Installed flag.</param>
        public static InstallFlagRepairDecision Decide(
            InstalledGameRecord record,
            string currentAppPath,
            bool? currentInstalled)
        {
            var decision = new InstallFlagRepairDecision();

            if (record == null || record.UninstalledAt.HasValue)
                return decision;

            if (!InstalledPathExists(record.InstalledPath))
            {
                decision.RequeueDownload = true;
                return decision;
            }

            var installedFlag = currentInstalled == true;
            var appPathOk = !string.IsNullOrWhiteSpace(currentAppPath)
                && (File.Exists(currentAppPath) || Directory.Exists(currentAppPath));

            if (!installedFlag || !appPathOk)
            {
                decision.RepairFields = true;
                decision.UpdateApplicationPath = !appPathOk;
            }

            return decision;
        }

        private static bool InstalledPathExists(string installedPath)
        {
            if (string.IsNullOrWhiteSpace(installedPath))
                return false;
            return File.Exists(installedPath) || Directory.Exists(installedPath);
        }
    }
}
