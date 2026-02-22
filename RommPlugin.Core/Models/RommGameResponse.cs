using System.Collections.Generic;
using Newtonsoft.Json;

namespace RommPlugin.Core.Models
{
    public class RommGameResponse
    {
        [JsonProperty("items")]
        public List<RommGame> Items { get; set; }

        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("limit")]
        public int Limit { get; set; }

        [JsonProperty("offset")]
        public int Offset { get; set; }
    }
}