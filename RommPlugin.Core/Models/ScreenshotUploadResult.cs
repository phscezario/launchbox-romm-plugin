using Newtonsoft.Json;

namespace RommPlugin.Core.Models
{
    /// <summary>
    /// Represents the result of a screenshot upload operation.
    /// </summary>
    public class ScreenshotUploadResult
    {
        /// <summary>
        /// Gets or sets the unique identifier of the uploaded screenshot.
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets whether the screenshot is publicly visible.
        /// </summary>
        [JsonProperty("is_public")]
        public bool IsPublic { get; set; }

        /// <summary>
        /// Gets or sets the ROM identifier the screenshot belongs to.
        /// </summary>
        [JsonProperty("rom_id")]
        public int RomId { get; set; }

        /// <summary>
        /// Gets or sets the user identifier who uploaded the screenshot.
        /// </summary>
        [JsonProperty("user_id")]
        public int UserId { get; set; }
    }
}
