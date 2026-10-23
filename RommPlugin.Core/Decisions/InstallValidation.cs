using System.IO;

namespace RommPlugin.Core.Decisions
{
    /// <summary>
    /// Pure decision logic for folder-type detection. The RomM sync stamps
    /// <c>romm_isFolder_game</c> from the API only on game creation and never
    /// repairs it, so single-zip multi-file games (e.g. PS3) can carry a stale
    /// <c>False</c>. Disk evidence (folder + _launchbox.json) overrides it.
    /// </summary>
    public static class InstallValidation
    {
        /// <summary>
        /// Whether a folder game install is usable: the folder exists and
        /// carries _launchbox.json, which only a completed extraction (or a
        /// deliberate folder sync) produces. Failed attempts leave .part
        /// staging behind, never a valid folder.
        /// </summary>
        public static bool IsFolderInstallValid(string installedPath)
        {
            if (string.IsNullOrWhiteSpace(installedPath)) return false;
            if (!Directory.Exists(installedPath)) return false;
            return File.Exists(Path.Combine(installedPath, Constants.RommConstants.LaunchboxConfigFile));
        }

        /// <summary>Whether a file game install is usable: the file exists.</summary>
        public static bool IsFileInstallValid(string installedPath)
        {
            if (string.IsNullOrWhiteSpace(installedPath)) return false;
            return File.Exists(installedPath);
        }

        /// <summary>
        /// Effective folder-type: the sync flag wins when true, otherwise disk
        /// evidence decides (extensionless name + folder carrying
        /// _launchbox.json). Never throws.
        /// </summary>
        public static bool IsFolderGameEffective(bool isFolderFlag, string romsPath, string remotePath, string fileName)
        {
            if (isFolderFlag) return true;
            if (string.IsNullOrWhiteSpace(fileName) || Path.HasExtension(fileName)) return false;

            try
            {
                var folder = ResolveInstalledPath(romsPath, remotePath, fileName, true);
                return Directory.Exists(folder)
                    && File.Exists(Path.Combine(folder, Constants.RommConstants.LaunchboxConfigFile));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Resolves where a game must be installed, mirroring the install flow
        /// (folder games point at the extracted folder, file games at the file
        /// or its .zip). Pure path math, except the file-game branch prefers an
        /// existing file when present.
        /// </summary>
        public static string ResolveInstalledPath(string romsPath, string remotePath, string fileName, bool isFolderGame)
        {
            var localFile = Path.Combine(
                romsPath ?? "",
                Constants.RommConstants.RomsSubfolder,
                (remotePath ?? "").Replace("/", "\\"),
                fileName ?? "");

            if (isFolderGame)
            {
                return Path.Combine(
                    Path.GetDirectoryName(localFile),
                    Path.GetFileNameWithoutExtension(localFile));
            }

            if (File.Exists(localFile))
                return localFile;
            if (File.Exists(localFile + ".zip"))
                return localFile + ".zip";
            return localFile;
        }
    }
}
