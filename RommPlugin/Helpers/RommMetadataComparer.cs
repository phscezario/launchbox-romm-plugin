using System;
using System.Collections.Generic;
using System.Linq;
using RommPlugin.Core.Models;
using Unbroken.LaunchBox.Plugins.Data;

namespace RommPlugin.Helpers
{
    public enum SyncDirection
    {
        None,
        PushToRomm,
        PullFromRomm
    }

    public class MetadataComparisonResult
    {
        public SyncDirection Direction { get; set; }
        public List<string> ChangedFields { get; set; } = new List<string>();
        public bool MetadataChanged { get; set; }
        public bool ArtworkChanged { get; set; }
        public bool ScreenshotsChanged { get; set; }
        public bool HasLocalArtwork { get; set; }
        public bool HasRemoteArtwork { get; set; }
    }

    public static class RommMetadataComparer
    {
        public static MetadataComparisonResult Compare(
            IGame game,
            RommGame remote,
            DateTime? lastSyncedAt,
            int localScreenshotCount,
            int remoteScreenshotCount,
            bool hasLocalArtwork,
            bool hasRemoteArtwork)
        {
            var result = new MetadataComparisonResult();

            var lbSnapshot = BuildLaunchBoxSnapshot(game);
            var remoteSnapshot = BuildRommSnapshot(remote);

            CompareString(result, "Name", lbSnapshot.Name, remoteSnapshot.Name);
            CompareString(result, "Notes", lbSnapshot.Notes, remoteSnapshot.Notes);
            CompareLong(result, "ReleaseDate", lbSnapshot.ReleaseDate, remoteSnapshot.ReleaseDate);
            CompareInt(result, "MaxPlayers", lbSnapshot.MaxPlayers, remoteSnapshot.MaxPlayers);
            CompareString(result, "ReleaseType", lbSnapshot.ReleaseType, remoteSnapshot.ReleaseType);
            CompareString(result, "PlayMode", lbSnapshot.PlayMode, remoteSnapshot.PlayMode);
            CompareString(result, "VideoUrl", lbSnapshot.VideoUrl, remoteSnapshot.VideoUrl);
            CompareFloat(result, "CommunityRating", lbSnapshot.CommunityRating, remoteSnapshot.CommunityRating);
            CompareInt(result, "CommunityStarRatingTotalVotes", lbSnapshot.CommunityStarRatingTotalVotes, remoteSnapshot.CommunityStarRatingTotalVotes);
            CompareString(result, "WikipediaUrl", lbSnapshot.WikipediaUrl, remoteSnapshot.WikipediaUrl);
            CompareString(result, "Rating", lbSnapshot.Rating, remoteSnapshot.Rating);
            CompareStringList(result, "Genres", lbSnapshot.Genres, remoteSnapshot.Genres);
            CompareStringList(result, "Developers", lbSnapshot.Developers, remoteSnapshot.Developers);
            CompareStringList(result, "Publishers", lbSnapshot.Publishers, remoteSnapshot.Publishers);

            result.MetadataChanged = result.ChangedFields.Count > 0;
            result.ScreenshotsChanged = localScreenshotCount != remoteScreenshotCount;
            result.HasLocalArtwork = hasLocalArtwork;
            result.HasRemoteArtwork = hasRemoteArtwork;
            result.ArtworkChanged = hasLocalArtwork != hasRemoteArtwork;

            if (result.MetadataChanged)
            {
                if (remote.UpdatedAt != null && lastSyncedAt != null && remote.UpdatedAt > lastSyncedAt)
                {
                    result.Direction = SyncDirection.PullFromRomm;
                }
                else
                {
                    result.Direction = SyncDirection.PushToRomm;
                }
            }
            else if (result.ArtworkChanged)
            {
                if (hasRemoteArtwork && !hasLocalArtwork)
                {
                    result.Direction = SyncDirection.PullFromRomm;
                }
                else
                {
                    result.Direction = SyncDirection.PushToRomm;
                }
            }
            else if (result.ScreenshotsChanged)
            {
                result.Direction = SyncDirection.PushToRomm;
            }
            else
            {
                result.Direction = SyncDirection.None;
            }

            return result;
        }

        private static void CompareString(MetadataComparisonResult result, string fieldName, string local, string remote)
        {
            var l = local ?? "";
            var r = remote ?? "";
            if (!string.Equals(l, r, StringComparison.OrdinalIgnoreCase))
            {
                result.ChangedFields.Add(fieldName);
            }
        }

        private static void CompareLong(MetadataComparisonResult result, string fieldName, long? local, long? remote)
        {
            if (local != remote)
            {
                result.ChangedFields.Add(fieldName);
            }
        }

        private static void CompareInt(MetadataComparisonResult result, string fieldName, int? local, int? remote)
        {
            if (local != remote)
            {
                result.ChangedFields.Add(fieldName);
            }
        }

        private static void CompareFloat(MetadataComparisonResult result, string fieldName, float local, float remote)
        {
            if (Math.Abs(local - remote) > 0.001f)
            {
                result.ChangedFields.Add(fieldName);
            }
        }

        private static void CompareStringList(MetadataComparisonResult result, string fieldName, List<string> local, List<string> remote)
        {
            var l = local ?? new List<string>();
            var r = remote ?? new List<string>();
            if (l.Count != r.Count || !l.SequenceEqual(r))
            {
                result.ChangedFields.Add(fieldName);
            }
        }

        public static MetadataSnapshot BuildLaunchBoxSnapshot(IGame game)
        {
            var genres = new List<string>();
            if (!string.IsNullOrEmpty(game.GenresString))
            {
                genres = game.GenresString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(g => g.Trim()).ToList();
            }

            var developers = new List<string>();
            if (!string.IsNullOrEmpty(game.Developer))
            {
                developers = game.Developer.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(d => d.Trim()).ToList();
            }

            var publishers = new List<string>();
            if (!string.IsNullOrEmpty(game.Publisher))
            {
                publishers = game.Publisher.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim()).ToList();
            }

            return new MetadataSnapshot
            {
                Name = game.Title ?? "",
                Notes = game.Notes ?? "",
                ReleaseDate = game.ReleaseDate.HasValue
                    ? new DateTimeOffset(game.ReleaseDate.Value).ToUnixTimeSeconds()
                    : (long?)null,
                MaxPlayers = game.MaxPlayers,
                ReleaseType = game.ReleaseType ?? "",
                PlayMode = game.PlayMode ?? "",
                VideoUrl = game.VideoUrl ?? "",
                CommunityRating = game.CommunityStarRating,
                CommunityStarRatingTotalVotes = game.CommunityStarRatingTotalVotes > 0
                    ? (int?)game.CommunityStarRatingTotalVotes : null,
                WikipediaUrl = game.WikipediaUrl ?? "",
                Rating = game.Rating ?? "",
                Genres = genres,
                Developers = developers,
                Publishers = publishers
            };
        }

        public static MetadataSnapshot BuildRommSnapshot(RommGame remote)
        {
            var lb = remote.LaunchBoxMetadata;
            var ss = remote.SsMetadata;
            var igdb = remote.IgdbMetadata;
            var meta = remote.Metadatum;

            long? releaseDate = null;
            if (lb?.FirstReleaseDate != null) releaseDate = lb.FirstReleaseDate;
            else if (igdb?.FirstReleaseDate != null) releaseDate = igdb.FirstReleaseDate;
            else if (meta?.FirstReleaseDate != null) releaseDate = meta.FirstReleaseDate;

            int? maxPlayers = null;
            if (lb?.MaxPlayers != null) maxPlayers = lb.MaxPlayers;
            else if (ss?.Players != null && int.TryParse(ss.Players, out var p)) maxPlayers = p;

            var playMode = "";
            if (lb?.Cooperative == true) playMode = "Cooperative";

            var videoId = lb?.YoutubeVideoId ?? igdb?.YoutubeVideoId;
            var videoUrl = !string.IsNullOrEmpty(videoId) ? $"https://www.youtube.com/watch?v={videoId}" : "";

            float communityRating = 0;
            if (lb != null && lb.CommunityRating > 0) communityRating = lb.CommunityRating;
            else if (igdb != null && igdb.TotalRating.HasValue) communityRating = (float)igdb.TotalRating.Value;
            else if (meta != null && meta.AverageRating.HasValue) communityRating = (float)meta.AverageRating.Value;

            int? communityRatingCount = null;
            if (lb != null && lb.CommunityRatingCount > 0) communityRatingCount = lb.CommunityRatingCount;

            var releaseType = lb?.ReleaseType ?? "";
            var wikipediaUrl = lb?.WikipediaUrl ?? "";
            var esrb = lb?.Esrb ?? "";

            var genres = new List<string>();
            if (lb?.Genres != null && lb.Genres.Count > 0) genres = lb.Genres;
            else if (ss?.Genres != null && ss.Genres.Count > 0) genres = ss.Genres;
            else if (igdb?.Genres != null && igdb.Genres.Count > 0) genres = igdb.Genres;
            else if (meta?.Genres != null && meta.Genres.Count > 0) genres = meta.Genres;

            var developers = new List<string>();
            if (lb?.Companies != null && lb.Companies.Count > 0) developers = lb.Companies;
            else if (igdb?.Companies != null && igdb.Companies.Count > 0) developers = igdb.Companies;
            else if (meta?.Companies != null && meta.Companies.Count > 0) developers = meta.Companies;

            var publishers = new List<string>();
            if (ss?.Editeur != null && !string.IsNullOrEmpty(ss.Editeur)) publishers.Add(ss.Editeur);
            else if (ss?.Publisher != null && !string.IsNullOrEmpty(ss.Publisher)) publishers.Add(ss.Publisher);
            else if (developers.Count > 0) publishers.AddRange(developers);

            var summary = ss?.Synopsis ?? ss?.Description ?? remote.Summary ?? "";

            return new MetadataSnapshot
            {
                Name = remote.Name ?? "",
                Notes = summary,
                ReleaseDate = releaseDate,
                MaxPlayers = maxPlayers,
                ReleaseType = releaseType,
                PlayMode = playMode,
                VideoUrl = videoUrl,
                CommunityRating = communityRating,
                CommunityStarRatingTotalVotes = communityRatingCount,
                WikipediaUrl = wikipediaUrl,
                Rating = esrb,
                Genres = genres,
                Developers = developers,
                Publishers = publishers
            };
        }

        public static string ComputeRemoteMetadataHash(RommGame remote)
        {
            var pathCover = StripQueryString(remote.PathCoverSmall ?? "");
            var urlCover = StripQueryString(remote.UrlCover ?? "");

            var payload = string.Join("|",
                remote.Name ?? "",
                remote.Summary ?? "",
                pathCover,
                urlCover);

            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(payload);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        private static string StripQueryString(string url)
        {
            var idx = url.IndexOf('?');
            return idx >= 0 ? url.Substring(0, idx) : url;
        }

        public static string ComputeLocalMetadataHash(IGame game, string screenshotFingerprints = "")
        {
            var snapshot = BuildLaunchBoxSnapshot(game);

            var payload = string.Join("|",
                snapshot.Name ?? "",
                snapshot.Notes ?? "",
                snapshot.ReleaseDate?.ToString() ?? "",
                snapshot.MaxPlayers?.ToString() ?? "",
                snapshot.ReleaseType ?? "",
                snapshot.PlayMode ?? "",
                snapshot.VideoUrl ?? "",
                snapshot.CommunityRating.ToString("F2"),
                snapshot.CommunityStarRatingTotalVotes?.ToString() ?? "",
                snapshot.WikipediaUrl ?? "",
                snapshot.Rating ?? "",
                string.Join(",", snapshot.Genres ?? new List<string>()),
                string.Join(",", snapshot.Developers ?? new List<string>()),
                string.Join(",", snapshot.Publishers ?? new List<string>()),
                screenshotFingerprints ?? "");

            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(payload);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }

    public class MetadataSnapshot
    {
        public string Name { get; set; }
        public string Notes { get; set; }
        public long? ReleaseDate { get; set; }
        public int? MaxPlayers { get; set; }
        public string ReleaseType { get; set; }
        public string PlayMode { get; set; }
        public string VideoUrl { get; set; }
        public float CommunityRating { get; set; }
        public int? CommunityStarRatingTotalVotes { get; set; }
        public string WikipediaUrl { get; set; }
        public string Rating { get; set; }
        public List<string> Genres { get; set; }
        public List<string> Developers { get; set; }
        public List<string> Publishers { get; set; }
    }
}
