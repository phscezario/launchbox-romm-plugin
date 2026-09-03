namespace RommPlugin.Services
{
    /// <summary>
    /// Defines the contract for creating timestamped backups of LaunchBox XML data files.
    /// </summary>
    public interface IRommBackupService
    {
        /// <summary>
        /// Creates a timestamped backup of the specified LaunchBox XML file.
        /// Automatically rotates old backups when the maximum count is reached.
        /// </summary>
        /// <param name="fileName">The name of the XML file to back up (e.g., "Platforms.xml").</param>
        void BackupXml(string fileName);
    }
}
