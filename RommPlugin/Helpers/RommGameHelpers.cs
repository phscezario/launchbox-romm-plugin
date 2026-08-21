using System.Linq;
using RommPlugin.Core.Models;
using Unbroken.LaunchBox.Plugins.Data;

namespace RommPlugin.Helpers
{
    public static class RommGameHelpers
    {
        public static bool TryGetRommId(IGame game, out int rommId)
        {
            rommId = 0;

            var value = game.GetAllCustomFields()
                .FirstOrDefault(f => f.Name == GameCustomFields.GameId)?.Value;

            return int.TryParse(value, out rommId);
        }

        public static int GetRommId(IGame game)
        {
            var value = game.GetAllCustomFields()
                .FirstOrDefault(f => f.Name == GameCustomFields.GameId)?.Value;

            return int.TryParse(value, out var id) ? id : 0;
        }
    }
}
