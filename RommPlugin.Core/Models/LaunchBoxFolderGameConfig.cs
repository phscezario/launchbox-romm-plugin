using System.Collections.Generic;

namespace RommPlugin.Core.Models
{
    /// <summary>
    /// Represents the LaunchBox configuration for a folder-based game.
    /// </summary>
    public class LaunchBoxFolderGameConfig
    {
        /// <summary>
        /// Gets or sets the default filename to launch.
        /// </summary>
        public string DefaultFileName { get; set; }

        /// <summary>
        /// Gets or sets whether the game has downloadable content (DLC).
        /// </summary>
        public bool? HasDLC { get; set; } = null;

        /// <summary>
        /// Gets or sets the list of additional applications (e.g., configuration tools).
        /// </summary>
        public List<AdditionalApplications> AdditionalApplications { get; set; } = null;

        /// <summary>
        /// Gets or sets the list of pre-loader applications that run before the game.
        /// </summary>
        public List<AdditionalApplications> PreLoaders { get; set; } = null;

        /// <summary>
        /// Gets or sets the list of post-loader applications that run after the game.
        /// </summary>
        public List<AdditionalApplications> PosLoaders { get; set; } = null;
    }

    /// <summary>
    /// Represents an additional application associated with a game.
    /// </summary>
    public class AdditionalApplications
    {
        /// <summary>
        /// Gets or sets the display name of the application.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the file path to the application.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the command line arguments for the application.
        /// </summary>
        public string CommandLine { get; set; } = null;

        /// <summary>
        /// Gets or sets whether to wait for the application to exit before continuing.
        /// </summary>
        public bool? WaitForExit { get; set; } = null;

        /// <summary>
        /// Gets or sets whether the path is relative to the LaunchBox root directory.
        /// </summary>
        public bool? FromLaunchBoxRoot { get; set; } = false;
    }
}
