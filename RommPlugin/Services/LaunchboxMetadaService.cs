using System;
using System.Collections.Generic;
using System.Linq;
using RommPlugin.Core.Models;
using Unbroken.LaunchBox.Plugins.Data;

namespace RommPlugin.Services
{
    public static class LaunchboxMetadaService
    {
        public static LaunchBoxMetadataModel BuildLaunchboxMetadata(IGame game)
        {
            var metadata = new LaunchBoxMetadataModel
            {
                FirstReleaseDate = game.ReleaseDate != null
                    ? new DateTimeOffset(game.ReleaseDate.Value).ToUnixTimeSeconds()
                    : (long?)null,
                MaxPlayers = game.MaxPlayers ?? 1,
                ReleaseType = string.IsNullOrEmpty(game.ReleaseType) ? "Released" : game.ReleaseType,
                Cooperative = game.PlayMode == "Cooperative",
                YoutubeVideoId = ExtractYoutubeId(game.VideoUrl),
                CommunityRating = game.CommunityStarRating,
                CommunityRatingCount = game.CommunityStarRatingTotalVotes,
                WikipediaUrl = game.WikipediaUrl,
                Esrb = string.IsNullOrEmpty(game.Rating) ? "Not Rated" : game.Rating,
                Genres = game.Genres?.ToList() ?? new List<string>(),
                Companies = game.Developers?.ToList() ?? new List<string>(),
                Images = new List<LaunchBoxImage>()
            };

            return metadata;
        }

        private static string ExtractYoutubeId(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return "";
            }

            var uri = new Uri(url);
            var query = uri.Query;

            var match = System.Text.RegularExpressions.Regex.Match(query, @"v=([^&]+)");
            return match.Success ? match.Groups[1].Value : "";
        }
    }
}
