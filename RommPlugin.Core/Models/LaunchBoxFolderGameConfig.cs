using System.Collections.Generic;

namespace RommPlugin.Core.Models
{
    public class LaunchBoxFolderGameConfig
    {
        public string DefaultFileName { get; set; }

        public bool? HasDLC { get; set; } = null;

        public List<AdditionalApplications> AdditionalApplications { get; set; } = null;

        public List<AdditionalApplications> PreLoaders { get; set; } = null;

        public List<AdditionalApplications> PosLoaders { get; set; } = null;
    }

    public class AdditionalApplications
    {
        public string Name { get; set; }

        public string Path { get; set; }

        public string CommandLine { get; set; } = null;

        public bool? WaitForExit { get; set; } = null;
    }
}
