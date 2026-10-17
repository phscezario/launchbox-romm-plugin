using System;
using System.Linq;
using RommPlugin.Core.Constants;
using RommPlugin.Core.Models;
using RommPlugin.Core.Services;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace RommPlugin.Services
{
    /// <summary>
    /// Single LaunchBox-coupled implementation of <see cref="ILaunchBoxGameGateway"/>.
    /// Mirrors GameManagerForm behavior verbatim (platform-prefix filter,
    /// first-match custom-field lookup); orchestration must not depend on more.
    /// </summary>
    public sealed class LaunchBoxGameGateway : ILaunchBoxGameGateway
    {
        public GameFieldSnapshot FindByRommId(int rommGameId)
        {
            var game = FindGame(rommGameId);
            if (game == null) return null;

            var fields = game.GetAllCustomFields()
                .GroupBy(f => f.Name)
                .ToDictionary(g => g.Key, g => g.Last().Value);

            fields.TryGetValue(GameCustomFields.RemotePath, out var remotePath);
            fields.TryGetValue(GameCustomFields.FileName, out var fileName);
            fields.TryGetValue(GameCustomFields.IsFolderGame, out var folderFlag);

            return new GameFieldSnapshot
            {
                RommGameId = rommGameId,
                Title = game.Title,
                Platform = game.Platform,
                Genre = game.Genres?.FirstOrDefault(),
                RemotePath = remotePath,
                FileName = fileName,
                FolderFlagValue = folderFlag,
            };
        }

        public void ClearForUninstall(int rommGameId, string installedPath)
        {
            var game = FindGame(rommGameId);
            if (game == null) return;

            RommPlugin.Helpers.RommGameHelpers.ClearGameAdditionalApplications(game, installedPath);
            game.ApplicationPath = null;
            game.Installed = false;
        }

        public void BackfillFolderFlag(int rommGameId)
        {
            var game = FindGame(rommGameId);
            if (game == null) return;

            var field = game.GetAllCustomFields()
                .FirstOrDefault(f => f.Name == GameCustomFields.IsFolderGame);
            if (field != null && field.Value == bool.TrueString) return;

            if (field == null)
            {
                field = game.AddNewCustomField();
                field.Name = GameCustomFields.IsFolderGame;
            }

            field.Value = bool.TrueString;
        }

        public void Save()
        {
            PluginHelper.DataManager.Save();
        }

        private static IGame FindGame(int rommGameId)
        {
            var dataManager = PluginHelper.DataManager;
            var rommGames = dataManager.GetAllGames()
                .Where(g => g.Platform != null && g.Platform.StartsWith(RommConstants.PlatformPrefix))
                .ToList();

            foreach (var game in rommGames)
            {
                var value = game.GetAllCustomFields()
                    .FirstOrDefault(f => f.Name == GameCustomFields.GameId)?.Value;
                if (int.TryParse(value, out var id) && id == rommGameId)
                {
                    return game;
                }
            }

            return null;
        }
    }
}
