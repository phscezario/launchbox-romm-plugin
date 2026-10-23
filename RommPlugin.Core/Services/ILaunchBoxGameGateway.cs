namespace RommPlugin.Core.Services
{
    /// <summary>
    /// Snapshot of the LaunchBox-side fields the plugin manages, without
    /// leaking LaunchBox types into callers.
    /// </summary>
    public sealed class GameFieldSnapshot
    {
        public int RommGameId { get; set; }
        public string Title { get; set; }
        public string Platform { get; set; }
        public string Genre { get; set; }
        public string RemotePath { get; set; }
        public string FileName { get; set; }
        public string FolderFlagValue { get; set; }
    }

    /// <summary>
    /// LB-coupled game access behind DTOs. Implemented once (LaunchBox API);
    /// consumed by orchestration without LaunchBox types.
    /// </summary>
    public interface ILaunchBoxGameGateway
    {
        /// <summary>Finds the game by RomM id, or null when absent.</summary>
        GameFieldSnapshot FindByRommId(int rommGameId);

        /// <summary>
        /// Removes the additional applications the plugin created for a game
        /// (DLC and loader entries). These are recorded in a config file inside
        /// the installed folder, so this must run while that folder still
        /// exists, i.e. before the files are deleted.
        /// </summary>
        void ClearAdditionalApplications(int rommGameId, string installedPath);

        /// <summary>
        /// Detaches the LaunchBox entry (ApplicationPath/Installed) for an
        /// uninstall. Runs only after the files are gone.
        /// </summary>
        void DetachForUninstall(int rommGameId);

        /// <summary>
        /// Writes back a true folder flag when disk evidence overrode a stale
        /// sync flag. Idempotent.
        /// </summary>
        void BackfillFolderFlag(int rommGameId);

        /// <summary>Persists LaunchBox-side changes.</summary>
        void Save();
    }
}
