using Newtonsoft.Json;

namespace RommPlugin.Core.Models
{
    public class RommAuthResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("expires")]
        public int Expires { get; set; }
    }
}