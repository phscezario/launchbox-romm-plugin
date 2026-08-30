using RommPlugin.Core.Models;
using RommPlugin.Core.Models.Statics;
using Unbroken.LaunchBox.Plugins.Data;

namespace RommPlugin.Services
{
    public interface IRommMetadataMapper
    {
        void ApplyServerMetadata(IGame game, RommGame rommGame, RommPluginSettings settings);
        void ApplyReleaseDate(IGame game, LaunchBoxMetadataModel lb, SsMetadata ss, IgdbMetadata igdb, RommGameMeta meta, bool overwrite);
        void ApplyMaxPlayers(IGame game, LaunchBoxMetadataModel lb, SsMetadata ss, bool overwrite);
        void ApplyPlayMode(IGame game, LaunchBoxMetadataModel lb, bool overwrite);
        void ApplyVideoUrl(IGame game, LaunchBoxMetadataModel lb, IgdbMetadata igdb, bool overwrite);
        void ApplyCommunityRating(IGame game, LaunchBoxMetadataModel lb, IgdbMetadata igdb, RommGameMeta meta, bool overwrite);
    }
}
