using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RommPlugin.Core.Models
{
    /// <summary>
    /// Represents LaunchBox metadata for a game.
    /// </summary>
    public class LaunchBoxMetadataModel
    {
        /// <summary>
        /// Gets or sets the first release date as a Unix timestamp.
        /// </summary>
        [JsonProperty("first_release_date")]
        public long? FirstReleaseDate { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of players.
        /// </summary>
        [JsonProperty("max_players")]
        public int? MaxPlayers { get; set; }

        /// <summary>
        /// Gets or sets the release type (e.g., "Original", "Remake").
        /// </summary>
        [JsonProperty("release_type")]
        public string ReleaseType { get; set; }

        /// <summary>
        /// Gets or sets whether the game supports cooperative play.
        /// </summary>
        [JsonProperty("cooperative")]
        public bool? Cooperative { get; set; }

        /// <summary>
        /// Gets or sets the YouTube video ID for the game trailer.
        /// </summary>
        [JsonProperty("youtube_video_id")]
        public string YoutubeVideoId { get; set; }

        /// <summary>
        /// Gets or sets the community rating score.
        /// </summary>
        [JsonProperty("community_rating")]
        public float CommunityRating { get; set; }

        /// <summary>
        /// Gets or sets the number of community ratings.
        /// </summary>
        [JsonProperty("community_rating_count")]
        public int CommunityRatingCount { get; set; }

        /// <summary>
        /// Gets or sets the Wikipedia page URL.
        /// </summary>
        [JsonProperty("wikipedia_url")]
        public string WikipediaUrl { get; set; }

        /// <summary>
        /// Gets or sets the ESRB rating.
        /// </summary>
        [JsonProperty("esrb")]
        public string Esrb { get; set; }

        /// <summary>
        /// Gets or sets the list of genres.
        /// </summary>
        [JsonProperty("genres")]
        public List<string> Genres { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the list of companies.
        /// </summary>
        [JsonProperty("companies")]
        public List<string> Companies { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the list of game images.
        /// </summary>
        [JsonProperty("images")]
        public List<LaunchBoxImage> Images { get; set; } = new List<LaunchBoxImage>();
    }

    /// <summary>
    /// Represents an image associated with a game in LaunchBox.
    /// </summary>
    public class LaunchBoxImage
    {
        /// <summary>
        /// Gets or sets the URL to the image.
        /// </summary>
        [JsonProperty("url")]
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the image type (e.g., "Box", "Screenshot").
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the region the image is from.
        /// </summary>
        [JsonProperty("region")]
        public string Region { get; set; }
    }
}
