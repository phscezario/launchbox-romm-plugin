using System;
using System.Collections.Generic;
using System.Linq;

namespace RommPlugin.Core.Services
{
    /// <summary>
    /// Pure set logic for making plugin updates idempotent: computes which files
    /// deployed in the plugin folder are neither part of the update package nor
    /// protected runtime data, and therefore safe to remove. UI- and IO-free so it
    /// can be unit tested; the CLI performs the actual deletion.
    /// </summary>
    public static class UpdateReconcile
    {
        /// <summary>Runtime files inside the plugin folder that are never deleted.</summary>
        public static readonly string[] ProtectedFiles =
        {
            "settings.json",
            "download-state.json",
            "installed-games.json",
            "sync_information.json",
            "pending_hierarchy.json"
        };

        /// <summary>Runtime directories (relative) whose contents are never deleted.</summary>
        public static readonly string[] ProtectedDirs =
        {
            "Logs"
        };

        /// <summary>
        /// Returns the deployed relative paths that can be removed.
        /// A path is obsolete when it is not in the package set and not protected
        /// (either listed as a protected file or located under a protected dir).
        /// Comparison is case-insensitive; separators are normalized.
        /// </summary>
        public static List<string> ComputeObsoleteFiles(
            IEnumerable<string> deployedRelativePaths,
            IEnumerable<string> packageRelativePaths,
            IEnumerable<string> protectedFiles = null,
            IEnumerable<string> protectedDirs = null)
        {
            var package = new HashSet<string>(
                (packageRelativePaths ?? Enumerable.Empty<string>())
                    .Select(Normalize)
                    .Where(p => p != null),
                StringComparer.OrdinalIgnoreCase);

            var protectedFileSet = new HashSet<string>(
                (protectedFiles ?? ProtectedFiles)
                    .Select(Normalize)
                    .Where(p => p != null),
                StringComparer.OrdinalIgnoreCase);

            var protectedDirList = (protectedDirs ?? ProtectedDirs)
                .Select(Normalize)
                .Where(p => p != null)
                .Select(p => p + "\\")
                .ToList();

            var obsolete = new List<string>();

            foreach (var raw in deployedRelativePaths ?? Enumerable.Empty<string>())
            {
                var path = Normalize(raw);
                if (path == null)
                    continue;
                if (package.Contains(path))
                    continue;
                if (protectedFileSet.Contains(path))
                    continue;
                if (protectedDirList.Any(d => path.StartsWith(d, StringComparison.OrdinalIgnoreCase)))
                    continue;
                obsolete.Add(path);
            }

            return obsolete;
        }

        private static string Normalize(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;
            return path.Replace('/', '\\').Trim().Trim('\\');
        }
    }
}
