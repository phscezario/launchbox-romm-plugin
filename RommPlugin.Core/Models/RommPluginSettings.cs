using System.Collections.Generic;

namespace RommPlugin.Core.Models
{
    public class RommPluginSettings
    {
        public string RommBaseUrl { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string ClientApiToken { get; set; }

        public string RomsPath { get; set; }

        public bool KeepLocalData { get; set; }

        public bool SaveLogs { get; set; }

        public bool ProcessPendingOnStartup { get; set; } = true;

        public bool LoginFormUseConfiguredAccount { get; set; }

        public bool LoginFormSaveAdminAccount { get; set; }

        public List<RommCurrentPlatform> CurrentPlatforms { get; set; } = new List<RommCurrentPlatform>();
    }

    public class RommCurrentPlatform
    {
        public string Name { get; set; }

        public int Id { get; set; }
    }
}
