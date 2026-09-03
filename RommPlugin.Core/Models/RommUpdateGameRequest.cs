namespace RommPlugin.Core.Models
{
    /// <summary>
    /// Represents a request to update a game's metadata on the RomM server.
    /// </summary>
    public class RommUpdateGameRequest
    {
        /// <summary>
        /// Gets or sets the game name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the game summary or description.
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// Gets or sets the LaunchBox identifier.
        /// </summary>
        public int? LaunchboxId { get; set; }

        /// <summary>
        /// Gets or sets the raw LaunchBox metadata.
        /// </summary>
        public LaunchBoxMetadataModel RawLaunchboxMetadata { get; set; }

        /// <summary>
        /// Gets or sets the path to the artwork file.
        /// </summary>
        public string ArtworkPath { get; set; }

        /// <summary>
        /// Gets or sets the hash of the artwork file for change detection.
        /// </summary>
        public string ArtworkHash { get; set; }
    }
}
