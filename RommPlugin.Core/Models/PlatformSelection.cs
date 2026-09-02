namespace RommPlugin.Core.Models
{
    /// <summary>
    /// Represents a platform selection item for the UI.
    /// </summary>
    public class PlatformSelection
    {
        /// <summary>
        /// Gets or sets the platform identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the platform display name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets whether the platform is selected for synchronization.
        /// </summary>
        public bool Selected { get; set; }
    }
}
