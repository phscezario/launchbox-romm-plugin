using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Newtonsoft.Json;
using RommPlugin.Core.Constants;
using RommPlugin.Core.Helpers;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Storage;
using Unbroken.LaunchBox.Plugins.Data;

namespace RommPlugin.Services
{
    public class RommHierarchyCli : IRommHierarchyCli
    {
        public void LaunchHierarchyCli(List<IPlatform> platforms, List<IGame> rommGamesOnly, Dictionary<string, string> platformCategoryMap, bool restartLaunchBox = false)
        {
            var baseDir = RommPaths.PluginFolder;
            var platformsDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "Data", "Platforms"));

            var platformsWithGames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var categoryPlatforms = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            if (Directory.Exists(platformsDir))
            {
                foreach (var file in Directory.GetFiles(platformsDir, "RomM _ *.xml"))
                {
                    try
                    {
                        var platformName = RommConstants.PlatformPrefix + Path.GetFileNameWithoutExtension(file)
                            .Substring(RommConstants.PlaylistPrefix.Length);

                        var doc = XDocument.Load(file);
                        var gameCount = doc.Root?.Elements("Game").Count() ?? 0;

                        if (gameCount > 0)
                            platformsWithGames.Add(platformName);

                        if (platformCategoryMap.TryGetValue(platformName, out var category))
                        {
                            if (!categoryPlatforms.ContainsKey(category))
                                categoryPlatforms[category] = new List<string>();
                            categoryPlatforms[category].Add(platformName);
                        }
                    }
                    catch (Exception ex)
                    {
                        RommLogger.Log($"Hierarchy CLI: failed to read platform file '{Path.GetFileName(file)}': {ex.Message}");
                    }
                }
            }

            if (categoryPlatforms.Count == 0)
            {
                return;
            }

            var allCategories = platformCategoryMap.Values.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var categoriesWithGames = categoryPlatforms
                .Where(kv => kv.Value.Any(p => platformsWithGames.Contains(p)))
                .Select(kv => kv.Key)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var launchBoxExe = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "LaunchBox.exe"));
            var request = new
            {
                PlatformCategoryMap = platformCategoryMap,
                AllCategories = allCategories,
                CategoriesWithGames = categoriesWithGames,
                CategoryPlatforms = categoryPlatforms,
                RestartLaunchBox = restartLaunchBox,
                LaunchBoxExe = launchBoxExe
            };

            var json = JsonConvert.SerializeObject(request, Formatting.Indented);
            var pendingPath = Path.Combine(baseDir, RommConstants.PendingHierarchyFile);

            File.WriteAllText(pendingPath, json);
            RommLogger.Log($"Hierarchy CLI: wrote pending file with {allCategories.Count} allCategories, {categoriesWithGames.Count} categoriesWithGames, {categoryPlatforms.Values.Sum(l => l.Count)} platforms, restart={restartLaunchBox}");

            var cliPath = Path.Combine(baseDir, RommConstants.CliExecutable);
            if (!File.Exists(cliPath))
            {
                RommLogger.LogError($"CLI not found at {cliPath}");
                return;
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = cliPath,
                    Arguments = $"\"{pendingPath}\"",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;

                var proc = Process.Start(psi);

                var stdout = proc.StandardOutput.ReadToEnd();
                var stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                foreach (var line in stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    RommLogger.Log($"CLI: {line}");

                if (!string.IsNullOrEmpty(stderr))
                    RommLogger.LogError($"CLI errors: {stderr}");

                if (proc.ExitCode != 0)
                    RommLogger.LogError($"CLI exited with code {proc.ExitCode}");

                RommLogger.Log($"Hierarchy CLI completed (restart={restartLaunchBox})");
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"Failed to launch hierarchy CLI: {ex.Message}");
            }
        }
    }
}
