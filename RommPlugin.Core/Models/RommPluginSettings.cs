using System.Collections.Generic;

namespace RommPlugin.Core.Models
{
    public class RommPluginSettings
    {
        public string RommBaseUrl { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string RomsPath { get; set; }

        public bool KeepLocalData { get; set; }

        public List<RommCurrentPlatform> CurrentPlatforms { get; set; } = new List<RommCurrentPlatform>();
    }

    public class RommCurrentPlatform
    {
        public string Name { get; set; }

        public int Id { get; set; }
    }
}
