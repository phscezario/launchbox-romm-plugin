using System;
using System.Collections.Generic;
using System.IO;

namespace RommPlugin.Core.Decisions
{
    /// <summary>
    /// One group of probe paths sharing a single "Trying ..." log line, so the
    /// Game Manager's attempt order (and log interleaving) is preserved.
    /// </summary>
    public sealed class DeleteCandidateGroup
    {
        public DeleteCandidateGroup(string label, string describedPath, IReadOnlyList<string> paths)
        {
            Label = label;
            DescribedPath = describedPath;
            Paths = paths;
        }

        /// <summary>Log label suffix, e.g. "InstalledPath" or "constructed path".</summary>
        public string Label { get; }

        /// <summary>Path echoed in the group's "Trying {Label}: {path}" line.</summary>
        public string DescribedPath { get; }

        /// <summary>Paths to probe, in the Game Manager's order.</summary>
        public IReadOnlyList<string> Paths { get; }
    }

    /// <summary>
    /// Pure candidate-path enumeration for game file deletion, in the exact
    /// order the uninstall flow attempts: the installed path first, then the
    /// constructed local file, its .zip variant and the without-.zip variant.
    /// Conditional probes use an injected existence check so the enumeration
    /// stays side-effect free and testable.
    /// </summary>
    public static class DeleteCandidateListBuilder
    {
        public static IReadOnlyList<DeleteCandidateGroup> Build(
            string installedPath,
            string romsPath,
            string remotePath,
            string fileName,
            Func<string, bool> pathExists)
        {
            var groups = new List<DeleteCandidateGroup>();

            if (!string.IsNullOrEmpty(installedPath))
            {
                groups.Add(new DeleteCandidateGroup(
                    "InstalledPath",
                    installedPath,
                    new List<string> { installedPath }));
            }

            if (string.IsNullOrEmpty(remotePath) || string.IsNullOrEmpty(fileName))
                return groups;

            var localFile = Path.Combine(
                romsPath ?? "",
                Constants.RommConstants.RomsSubfolder,
                remotePath.Replace("/", "\\"),
                fileName);

            var constructed = new List<string> { localFile };

            var zipped = localFile + ".zip";
            if (Exists(pathExists, zipped))
                constructed.Add(zipped);

            if (fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                var withoutZip = localFile.Substring(0, localFile.Length - 4);
                if (Exists(pathExists, withoutZip))
                    constructed.Add(withoutZip);
            }

            groups.Add(new DeleteCandidateGroup("constructed path", localFile, constructed));

            return groups;
        }

        private static bool Exists(Func<string, bool> pathExists, string path)
        {
            return pathExists != null && pathExists(path);
        }
    }
}
