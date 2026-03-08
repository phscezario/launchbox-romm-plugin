using System.Collections.Generic;

namespace RommPlugin.Core.Models
{
    public class RommUpdateGameRequest
    {
        public string Name { get; set; }

        public string FsName { get; set; }

        public string Summary { get; set; }

        public int? LaunchboxId { get; set; }

        public LaunchBoxMetadataModel RawLaunchboxMetadata { get; set; }

        public string ArtworkPath { get; set; }
    }
}
