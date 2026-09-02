using System;
using RommPlugin.Core.Models;
using RommPlugin.Core.Models.Statics;
using Unbroken.LaunchBox.Plugins.Data;

namespace RommPlugin.Services
{
    /// <summary>
    /// Maps metadata from RomM server game objects to LaunchBox game properties, with priority fallback across multiple metadata sources.
    /// </summary>
    public class RommMetadataMapper : IRommMetadataMapper
    {
        /// <inheritdoc/>
        public void ApplyServerMetadata(IGame game, RommGame rommGame, RommPluginSettings settings)
        {
            var shouldOverwrite = !settings.KeepLocalData;

            var launchboxMeta = rommGame.LaunchBoxMetadata;
            var ssMeta = rommGame.SsMetadata;
            var igdbMeta = rommGame.IgdbMetadata;
            var meta = rommGame.Metadatum;

            ApplyReleaseDate(game, launchboxMeta, ssMeta, igdbMeta, meta, shouldOverwrite);
            ApplyMaxPlayers(game, launchboxMeta, ssMeta, shouldOverwrite);
            ApplyStringField(game.ReleaseType, v => game.ReleaseType = v,
                launchboxMeta?.ReleaseType, null, null, null, shouldOverwrite);
            ApplyPlayMode(game, launchboxMeta, shouldOverwrite);
            ApplyVideoUrl(game, launchboxMeta, igdbMeta, shouldOverwrite);
            ApplyCommunityRating(game, launchboxMeta, igdbMeta, meta, shouldOverwrite);
            ApplyIntField(() => game.CommunityStarRatingTotalVotes, v => game.CommunityStarRatingTotalVotes = v,
                launchboxMeta?.CommunityRatingCount, null, null, null, shouldOverwrite);
            ApplyStringField(game.WikipediaUrl, v => game.WikipediaUrl = v,
                launchboxMeta?.WikipediaUrl, null, null, null, shouldOverwrite);
            ApplyStringField(game.Rating, v => game.Rating = v,
                launchboxMeta?.Esrb, null, null, null, shouldOverwrite);

            if (shouldOverwrite || string.IsNullOrEmpty(game.Notes))
            {
                game.Notes = ssMeta?.Synopsis ?? ssMeta?.Description ?? rommGame.Summary ?? game.Notes;
            }

            if (rommGame.LaunchboxId != null && rommGame.LaunchboxId > 0)
            {
                game.LaunchBoxDbId = rommGame.LaunchboxId;
            }
        }

        /// <inheritdoc/>
        public void ApplyReleaseDate(IGame game, LaunchBoxMetadataModel lb, SsMetadata ss, IgdbMetadata igdb, RommGameMeta meta, bool overwrite)
        {
            if (overwrite || game.ReleaseDate == null)
            {
                DateTime? date = null;

                if (lb?.FirstReleaseDate != null)
                    date = UnixToDateTime(lb.FirstReleaseDate.Value);
                else if (ss?.ReleaseDate != null && DateTime.TryParse(ss.ReleaseDate, out var ssDate))
                    date = ssDate;
                else if (igdb?.FirstReleaseDate != null)
                    date = UnixToDateTime(igdb.FirstReleaseDate.Value);
                else if (meta?.FirstReleaseDate != null)
                    date = UnixToDateTime(meta.FirstReleaseDate.Value);

                if (date != null)
                    game.ReleaseDate = date.Value;
            }
        }

        /// <inheritdoc/>
        public void ApplyMaxPlayers(IGame game, LaunchBoxMetadataModel lb, SsMetadata ss, bool overwrite)
        {
            if (overwrite || game.MaxPlayers == null || game.MaxPlayers == 0)
            {
                if (lb?.MaxPlayers != null)
                    game.MaxPlayers = lb.MaxPlayers.Value;
                else if (ss?.Players != null && int.TryParse(ss.Players, out var players))
                    game.MaxPlayers = players;
            }
        }

        /// <inheritdoc/>
        public void ApplyPlayMode(IGame game, LaunchBoxMetadataModel lb, bool overwrite)
        {
            if (overwrite || string.IsNullOrEmpty(game.PlayMode))
            {
                if (lb?.Cooperative == true)
                    game.PlayMode = "Cooperative";
            }
        }

        /// <inheritdoc/>
        public void ApplyVideoUrl(IGame game, LaunchBoxMetadataModel lb, IgdbMetadata igdb, bool overwrite)
        {
            if (overwrite || string.IsNullOrEmpty(game.VideoUrl))
            {
                var videoId = lb?.YoutubeVideoId ?? igdb?.YoutubeVideoId;

                if (!string.IsNullOrEmpty(videoId))
                    game.VideoUrl = $"https://www.youtube.com/watch?v={videoId}";
            }
        }

        /// <inheritdoc/>
        public void ApplyCommunityRating(IGame game, LaunchBoxMetadataModel lb, IgdbMetadata igdb, RommGameMeta meta, bool overwrite)
        {
            if (overwrite || game.CommunityStarRating == 0)
            {
                if (lb?.CommunityRating > 0)
                    game.CommunityStarRating = lb.CommunityRating;
                else if (igdb?.TotalRating != null)
                    game.CommunityStarRating = (float)igdb.TotalRating.Value;
                else if (meta?.AverageRating != null)
                    game.CommunityStarRating = (float)meta.AverageRating.Value;
            }

            if (overwrite || game.CommunityStarRatingTotalVotes == 0)
            {
                if (lb?.CommunityRatingCount > 0)
                    game.CommunityStarRatingTotalVotes = lb.CommunityRatingCount;
            }
        }

        private void ApplyStringField(string currentValue, Action<string> setter,
            string lbValue, string ssValue, string igdbValue, string metaValue,
            bool shouldOverwrite)
        {
            if (shouldOverwrite || string.IsNullOrEmpty(currentValue))
            {
                var value = lbValue ?? ssValue ?? igdbValue ?? metaValue;

                if (!string.IsNullOrEmpty(value))
                    setter(value);
            }
        }

        private void ApplyIntField(Func<int> getter, Action<int> setter,
            int? lbValue, int? ssValue, int? igdbValue, int? metaValue,
            bool shouldOverwrite)
        {
            if (shouldOverwrite || getter() == 0)
            {
                var value = lbValue ?? ssValue ?? igdbValue ?? metaValue;

                if (value != null && value.Value > 0)
                    setter(value.Value);
            }
        }

        private static DateTime UnixToDateTime(long value)
        {
            var dto = value > 100_000_000_000L
                ? DateTimeOffset.FromUnixTimeMilliseconds(value)
                : DateTimeOffset.FromUnixTimeSeconds(value);
            return dto.DateTime;
        }
    }
}
