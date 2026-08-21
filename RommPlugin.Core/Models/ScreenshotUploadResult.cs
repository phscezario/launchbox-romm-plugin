using Newtonsoft.Json;

namespace RommPlugin.Core.Models
{
    public class ScreenshotUploadResult
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("is_public")]
        public bool IsPublic { get; set; }

        [JsonProperty("rom_id")]
        public int RomId { get; set; }

        [JsonProperty("user_id")]
        public int UserId { get; set; }
    }
}
