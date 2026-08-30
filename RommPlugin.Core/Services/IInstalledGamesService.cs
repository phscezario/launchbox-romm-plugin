using System.Collections.Generic;
using RommPlugin.Core.Models;

namespace RommPlugin.Core.Services
{
    public interface IInstalledGamesService
    {
        IReadOnlyList<InstalledGameRecord> GetAll();
        InstalledGameRecord GetByGameId(int rommGameId);
        bool IsInstalled(int rommGameId);
        void MarkInstalled(InstalledGameRecord record);
        void MarkUninstalled(int rommGameId);
        void RemoveUninstalled();
    }
}
