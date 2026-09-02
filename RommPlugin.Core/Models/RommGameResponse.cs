using System.Collections.Generic;
using Newtonsoft.Json;

namespace RommPlugin.Core.Models
{
    /// <summary>
    /// Represents a paginated response from the RomM games API.
    /// </summary>
    public class RommGameResponse
    {
        /// <summary>
        /// Gets or sets the list of games returned in the response.
        /// </summary>
        [JsonProperty("items")]
        public List<RommGame> Items { get; set; }

        /// <summary>
        /// Gets or sets the total number of games matching the query.
        /// </summary>
        [JsonProperty("total")]
        public int Total { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of items per page.
        /// </summary>
        [JsonProperty("limit")]
        public int Limit { get; set; }

        /// <summary>
        /// Gets or sets the offset of the current page.
        /// </summary>
        [JsonProperty("offset")]
        public int Offset { get; set; }
    }
}
