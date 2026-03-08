using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RommPlugin.Core.Models
{
    public class LaunchBoxMetadataModel
    {
        [JsonProperty("first_release_date")]
        public long? FirstReleaseDate { get; set; }

        [JsonProperty("max_players")]
        public int? MaxPlayers { get; set; }

        [JsonProperty("release_type")]
        public string ReleaseType { get; set; }

        [JsonProperty("cooperative")]
        public bool? Cooperative { get; set; }

        [JsonProperty("youtube_video_id")]
        public string YoutubeVideoId { get; set; }

        [JsonProperty("community_rating")]
        public float CommunityRating { get; set; }

        [JsonProperty("community_rating_count")]
        public int CommunityRatingCount { get; set; }

        [JsonProperty("wikipedia_url")]
        public string WikipediaUrl { get; set; }

        [JsonProperty("esrb")]
        public string Esrb { get; set; }

        [JsonProperty("genres")]
        public List<string> Genres { get; set; } = new List<string>();

        [JsonProperty("companies")]
        public List<string> Companies { get; set; } = new List<string>();

        [JsonProperty("images")]
        public List<LaunchBoxImage> Images { get; set; } = new List<LaunchBoxImage>();
    }

    public class LaunchBoxImage
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("region")]
        public string Region { get; set; }
    }
}