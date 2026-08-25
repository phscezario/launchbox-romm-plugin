using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace RommPlugin.CLI
{
    class Program
    {
        private static string _logDir;

        static void LogToFile(string message)
        {
            try
            {
                if (string.IsNullOrEmpty(_logDir))
                    _logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                Directory.CreateDirectory(_logDir);
                var filePath = Path.Combine(_logDir, $"cli-{DateTime.Now:yyyy-MM-dd}.log");
                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                File.AppendAllText(filePath, line + Environment.NewLine);
            }
            catch { }
        }

        static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine("Usage: RommPlugin.CLI.exe <pending_hierarchy.json>");
                Console.Error.WriteLine("       RommPlugin.CLI.exe --remove-all <dataDir>");
                return 1;
            }

            if (args[0] == "--remove-all" && args.Length >= 2)
            {
                return RemoveAllRomm(args[1]);
            }

            var jsonPath = args[0];
            if (!File.Exists(jsonPath))
            {
                Console.Error.WriteLine($"JSON file not found: {jsonPath}");
                return 2;
            }

            try
            {
                var json = File.ReadAllText(jsonPath);
                var request = JsonConvert.DeserializeObject<SyncRequest>(json);
                if (request == null)
                {
                    Console.Error.WriteLine("Failed to parse JSON request");
                    return 3;
                }

                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var dataDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "Data"));
                var launchBoxExe = request.LaunchBoxExe ?? Path.GetFullPath(Path.Combine(baseDir, "..", "..", "LaunchBox.exe"));

                if (!Directory.Exists(dataDir))
                {
                    Console.Error.WriteLine($"Data directory not found: {dataDir}");
                    return 4;
                }

                var parentsXmlPath = Path.Combine(dataDir, "Parents.xml");
                if (!File.Exists(parentsXmlPath))
                {
                    Console.Error.WriteLine($"Parents.xml not found: {parentsXmlPath}");
                    return 5;
                }

                Console.WriteLine($"DataDir: {dataDir}");
                Console.WriteLine($"Parents.xml: {parentsXmlPath}");
                Console.WriteLine($"RestartLaunchBox: {request.RestartLaunchBox}");
                Console.WriteLine($"AllCategories: {request.AllCategories?.Count ?? 0} [{string.Join(", ", request.AllCategories ?? new List<string>())}]");
                Console.WriteLine($"CategoriesWithGames: {request.CategoriesWithGames?.Count ?? 0} [{string.Join(", ", request.CategoriesWithGames ?? new List<string>())}]");
                Console.WriteLine($"CategoryPlatforms keys: {request.CategoryPlatforms?.Keys?.Count ?? 0} [{string.Join(", ", request.CategoryPlatforms?.Keys?.Where(k => !string.IsNullOrEmpty(k)) ?? Enumerable.Empty<string>())}]");
                LogToFile($"CLI started: restart={request.RestartLaunchBox}, categories={request.CategoriesWithGames?.Count ?? 0}, platforms={request.CategoryPlatforms?.Values?.Sum(l => l.Count) ?? 0}");

                BackupXml(parentsXmlPath, dataDir);

                var doc = XDocument.Load(parentsXmlPath);
                var root = doc.Root;
                if (root == null)
                {
                    Console.Error.WriteLine("Parents.xml has no root element");
                    return 6;
                }

                var changed = false;

                changed |= EnsureRootRomM(root);
                changed |= FixCategories(root, request.AllCategories);
                changed |= FixPlatforms(root, request.CategoryPlatforms);
                changed |= DeduplicatePlatforms(root);
                changed |= EnsurePlaylists(dataDir, request.CategoriesWithGames, request.CategoryPlatforms, root);
                changed |= FixPlaylistLinks(root, request.CategoriesWithGames);
                changed |= RemoveOrphans(root, request.CategoriesWithGames);
                changed |= RemoveOrphanPlaylistLinks(root, request.CategoriesWithGames);
                changed |= RemoveOrphanPlaylists(dataDir, request.CategoriesWithGames);
                ValidatePlaylistFiles(dataDir, root);

                if (changed)
                {
                    doc.Save(parentsXmlPath);
                    Console.WriteLine("Parents.xml saved successfully");
                }
                else
                {
                    Console.WriteLine("No changes needed");
                }

                if (request.RestartLaunchBox)
                {
                    LogToFile($"Restart path: launchBoxExe={launchBoxExe}, exists={File.Exists(launchBoxExe)}");
                    if (File.Exists(launchBoxExe))
                    {
                        LogToFile($"Restarting LaunchBox from: {launchBoxExe}");
                        RestartLaunchBox(launchBoxExe);
                    }
                    else
                    {
                        LogToFile($"LaunchBox.exe NOT FOUND at: {launchBoxExe}");
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Fatal error: {ex}");
                return 99;
            }
        }

        static int RemoveAllRomm(string dataDir)
        {
            try
            {
                Console.WriteLine("=== Remove All RomM ===");
                LogToFile("RemoveAllRomm started");

                BackupXml(Path.Combine(dataDir, "Parents.xml"), dataDir);
                BackupXml(Path.Combine(dataDir, "Platforms.xml"), dataDir);

                var parentsPath = Path.Combine(dataDir, "Parents.xml");
                if (File.Exists(parentsPath))
                {
                    var doc = XDocument.Load(parentsPath);
                    var root = doc.Root;
                    if (root != null)
                    {
                        var removed = root.Elements("Parent")
                            .Where(p =>
                            {
                                var name = (string)p.Element("PlatformCategoryName") ?? "";
                                var platform = (string)p.Element("PlatformName") ?? "";
                                var playlistId = (string)p.Element("PlaylistId") ?? "";
                                var parentCat = (string)p.Element("ParentPlatformCategoryName") ?? "";

                                return name.StartsWith("RomM") || platform.StartsWith("RomM")
                                    || parentCat.StartsWith("RomM")
                                    || (playlistId != "" && IsRommPlaylist(dataDir, playlistId));
                            })
                            .ToList();

                        foreach (var el in removed)
                            el.Remove();

                        doc.Save(parentsPath);
                        Console.WriteLine($"Removed {removed.Count} entries from Parents.xml");
                        LogToFile($"RemoveAllRomm: removed {removed.Count} entries from Parents.xml");
                    }
                }

                var platformsPath = Path.Combine(dataDir, "Platforms.xml");
                if (File.Exists(platformsPath))
                {
                    var doc = XDocument.Load(platformsPath);
                    var root = doc.Root;
                    if (root != null)
                    {
                        var removed = root.Elements("PlatformCategory")
                            .Where(p => ((string)p.Element("Name") ?? "").StartsWith("RomM"))
                            .ToList();

                        foreach (var el in removed)
                            el.Remove();

                        doc.Save(platformsPath);
                        Console.WriteLine($"Removed {removed.Count} PlatformCategory entries from Platforms.xml");
                        LogToFile($"RemoveAllRomm: removed {removed.Count} PlatformCategory entries from Platforms.xml");
                    }
                }

                var playlistsDir = Path.Combine(dataDir, "Playlists");
                if (Directory.Exists(playlistsDir))
                {
                    var files = Directory.GetFiles(playlistsDir, "RomM _ *.xml");
                    foreach (var file in files)
                    {
                        try
                        {
                            File.Delete(file);
                            Console.WriteLine($"Deleted playlist: {Path.GetFileName(file)}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Warning: could not delete {Path.GetFileName(file)}: {ex.Message}");
                        }
                    }
                    LogToFile($"RemoveAllRomm: deleted {files.Length} playlist files");
                }

                Console.WriteLine("=== Remove All RomM completed ===");
                LogToFile("RemoveAllRomm completed");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                LogToFile($"RemoveAllRomm error: {ex}");
                return 99;
            }
        }

        static bool IsRommPlaylist(string dataDir, string playlistId)
        {
            var playlistsDir = Path.Combine(dataDir, "Playlists");
            if (!Directory.Exists(playlistsDir))
                return false;

            foreach (var file in Directory.GetFiles(playlistsDir, "RomM _ *.xml"))
            {
                try
                {
                    var doc = XDocument.Load(file);
                    var id = (string)doc.Root?.Element("Playlist")?.Element("PlaylistId");
                    if (id == playlistId)
                        return true;
                }
                catch { }
            }
            return false;
        }

        static string GetBackupDir(string dataDir)
        {
            var backupDir = Path.Combine(dataDir, "RomM_Backups");
            if (!Directory.Exists(backupDir))
                Directory.CreateDirectory(backupDir);
            return backupDir;
        }

        static void BackupXml(string filePath, string dataDir)
        {
            if (!File.Exists(filePath))
                return;

            var backupDir = GetBackupDir(dataDir);
            var baseName = Path.GetFileNameWithoutExtension(filePath);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupPath = Path.Combine(backupDir, $"{baseName}_{timestamp}.xml");

            File.Copy(filePath, backupPath, true);
            Console.WriteLine($"Backup created: {Path.GetFileName(backupPath)}");

            var backups = Directory.GetFiles(backupDir, baseName + "_*.xml")
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .ToList();

            while (backups.Count >= 5)
            {
                var oldest = backups.Last();
                backups.RemoveAt(backups.Count - 1);
                try
                {
                    File.Delete(oldest);
                    Console.WriteLine($"Deleted old backup: {Path.GetFileName(oldest)}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: could not delete backup {Path.GetFileName(oldest)}: {ex.Message}");
                }
            }
        }

        static bool EnsureRootRomM(XElement root)
        {
            var rootEntries = root.Elements("Parent")
                .Where(p => (string)p.Element("PlatformCategoryName") == "RomM")
                .ToList();

            if (rootEntries.Count > 1)
            {
                var best = rootEntries
                    .OrderByDescending(e => e.Elements().Count(el => !string.IsNullOrEmpty(el.Value)))
                    .First();

                foreach (var dup in rootEntries.Where(e => e != best))
                {
                    dup.Remove();
                    Console.WriteLine("Removed duplicate root 'RomM' entry");
                }

                EnsureChildElements(best);
                return true;
            }

            if (rootEntries.Count == 1)
            {
                EnsureChildElements(rootEntries[0]);
                return false;
            }

            root.Add(new XElement("Parent",
                new XElement("PlatformName"),
                new XElement("PlaylistId"),
                new XElement("PlatformCategoryName", "RomM"),
                new XElement("ParentPlatformName"),
                new XElement("ParentPlaylistId"),
                new XElement("ParentPlatformCategoryName")
            ));
            Console.WriteLine("Created root 'RomM' category");
            return true;
        }

        static void EnsureChildElements(XElement entry)
        {
            var names = new[] { "PlatformName", "PlaylistId", "ParentPlatformName", "ParentPlaylistId", "ParentPlatformCategoryName" };
            foreach (var name in names)
            {
                if (entry.Element(name) == null)
                    entry.Add(new XElement(name));
            }
        }

        static bool FixCategories(XElement root, List<string> categoriesWithGames)
        {
            var changed = false;

            foreach (var category in categoriesWithGames)
            {
                var entries = root.Elements("Parent")
                    .Where(p => (string)p.Element("PlatformCategoryName"    ) == category)
                    .ToList();

                if (entries.Count > 1)
                {
                    var best = entries
                        .OrderByDescending(e => !string.IsNullOrEmpty((string)e.Element("ParentPlatformCategoryName")))
                        .ThenByDescending(e => e.Elements().Count(el => !string.IsNullOrEmpty(el.Value)))
                        .First();

                    foreach (var dup in entries.Where(e => e != best))
                    {
                        dup.Remove();
                        Console.WriteLine($"Removed duplicate entry for '{category}'");
                        changed = true;
                    }

                    EnsureChildElements(best);
                    changed |= SetParentCategory(best, "RomM");
                }
                else if (entries.Count == 1)
                {
                    EnsureChildElements(entries[0]);
                    changed |= SetParentCategory(entries[0], "RomM");
                }
                else
                {
                    root.Add(new XElement("Parent",
                        new XElement("PlatformName"),
                        new XElement("PlaylistId"),
                        new XElement("PlatformCategoryName", category),
                        new XElement("ParentPlatformName"),
                        new XElement("ParentPlaylistId"),
                        new XElement("ParentPlatformCategoryName", "RomM")
                    ));
                    Console.WriteLine($"Created category '{category}' with parent 'RomM'");
                    changed = true;
                }
            }

            return changed;
        }

        static bool FixPlatforms(XElement root, Dictionary<string, List<string>> categoryPlatforms)
        {
            var changed = false;

            foreach (var kvp in categoryPlatforms)
            {
                var categoryName = kvp.Key;
                foreach (var platformName in kvp.Value)
                {
                    var entries = root.Elements("Parent")
                        .Where(p => (string)p.Element("PlatformName") == platformName)
                        .ToList();

                    if (entries.Count == 0)
                    {
                        root.Add(new XElement("Parent",
                            new XElement("PlatformName", platformName),
                            new XElement("PlaylistId"),
                            new XElement("PlatformCategoryName"),
                            new XElement("ParentPlatformName"),
                            new XElement("ParentPlaylistId"),
                            new XElement("ParentPlatformCategoryName", categoryName)
                        ));
                        Console.WriteLine($"Created platform '{platformName}' with parent '{categoryName}'");
                        changed = true;
                    }
                    else
                    {
                        foreach (var entry in entries)
                        {
                            changed |= SetParentCategory(entry, categoryName);
                        }
                    }
                }
            }

            return changed;
        }

        static bool FixPlaylistLinks(XElement root, List<string> categoriesWithGames)
        {
            var playlistsDir = Path.Combine(
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Data")),
                "Playlists"
            );
            var changed = false;

            foreach (var category in categoriesWithGames)
            {
                var shortCategory = category.Substring("RomM | ".Length);
                var playlistFileName = $"RomM _ {shortCategory} Installed.xml";
                var playlistFilePath = Path.Combine(playlistsDir, playlistFileName);

                if (!File.Exists(playlistFilePath))
                    continue;

                var playlistDoc = XDocument.Load(playlistFilePath);
                var playlistId = (string)playlistDoc.Root?.Element("Playlist")?.Element("PlaylistId");
                if (string.IsNullOrEmpty(playlistId))
                    continue;

                var existing = root.Elements("Parent")
                    .Where(p => (string)p.Element("PlaylistId") == playlistId)
                    .ToList();

                if (existing.Count > 1)
                {
                    var best = existing
                        .OrderByDescending(e => !string.IsNullOrEmpty((string)e.Element("ParentPlatformCategoryName")))
                        .First();

                    foreach (var dup in existing.Where(e => e != best))
                    {
                        dup.Remove();
                        Console.WriteLine($"Removed duplicate playlist link '{playlistId}'");
                        changed = true;
                    }

                    changed |= SetParentCategory(best, category);
                }
                else if (existing.Count == 1)
                {
                    changed |= SetParentCategory(existing[0], category);
                }
                else
                {
                    root.Add(new XElement("Parent",
                        new XElement("PlatformName"),
                        new XElement("PlaylistId", playlistId),
                        new XElement("PlatformCategoryName"),
                        new XElement("ParentPlatformName"),
                        new XElement("ParentPlaylistId"),
                        new XElement("ParentPlatformCategoryName", category)
                    ));
                    Console.WriteLine($"Created playlist link '{playlistId}' -> '{category}'");
                    changed = true;
                }
            }

            var allInstalledPath = Path.Combine(playlistsDir, "RomM _ Installed Games.xml");
            if (File.Exists(allInstalledPath))
            {
                var allDoc = XDocument.Load(allInstalledPath);
                var allId = (string)allDoc.Root?.Element("Playlist")?.Element("PlaylistId");
                if (!string.IsNullOrEmpty(allId))
                {
                    var existing = root.Elements("Parent")
                        .Where(p => (string)p.Element("PlaylistId") == allId)
                        .ToList();

                    if (existing.Count == 0)
                    {
                        root.Add(new XElement("Parent",
                            new XElement("PlatformName"),
                            new XElement("PlaylistId", allId),
                            new XElement("PlatformCategoryName"),
                            new XElement("ParentPlatformName"),
                            new XElement("ParentPlaylistId"),
                            new XElement("ParentPlatformCategoryName", "RomM")
                        ));
                        Console.WriteLine($"Created playlist link '{allId}' -> 'RomM'");
                        changed = true;
                    }
                    else
                    {
                        foreach (var entry in existing)
                        {
                            changed |= SetParentCategory(entry, "RomM");
                        }
                    }
                }
            }

            return changed;
        }

        static bool RemoveOrphans(XElement root, List<string> categoriesWithGames)
        {
            var changed = false;

            var orphanCategories = root.Elements("Parent")
                .Where(p =>
                {
                    var name = (string)p.Element("PlatformCategoryName") ?? "";
                    return name.StartsWith("RomM | ") && !categoriesWithGames.Contains(name);
                })
                .ToList();

            foreach (var orphan in orphanCategories)
            {
                var name = (string)orphan.Element("PlatformCategoryName");
                orphan.Remove();
                Console.WriteLine($"Removed obsolete category '{name}'");
                changed = true;
            }

            return changed;
        }

        static bool RemoveOrphanPlaylists(string dataDir, List<string> categoriesWithGames)
        {
            var playlistsDir = Path.Combine(dataDir, "Playlists");
            if (!Directory.Exists(playlistsDir))
                return false;

            var changed = false;
            var rommPlaylists = Directory.GetFiles(playlistsDir, "RomM _ * Installed.xml");

            foreach (var file in rommPlaylists)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);

                if (fileName == "RomM _ Installed Games")
                    continue;

                var shortCat = fileName.Replace("RomM _ ", "").Replace(" Installed", "");
                var categoryName = $"RomM | {shortCat}";

                if (!categoriesWithGames.Contains(categoryName))
                {
                    try
                    {
                        File.Delete(file);
                        Console.WriteLine($"Removed obsolete playlist '{Path.GetFileName(file)}'");
                        changed = true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: could not delete playlist '{Path.GetFileName(file)}': {ex.Message}");
                    }
                }
            }

            return changed;
        }

        static bool EnsurePlaylists(string dataDir, List<string> categoriesWithGames, Dictionary<string, List<string>> categoryPlatforms, XElement parentsRoot)
        {
            var playlistsDir = Path.Combine(dataDir, "Playlists");
            if (!Directory.Exists(playlistsDir))
                Directory.CreateDirectory(playlistsDir);

            var changed = false;

            foreach (var category in categoriesWithGames)
            {
                var shortCategory = category.Substring("RomM | ".Length);
                var playlistName = $"RomM | {shortCategory} Installed";
                var playlistFileName = $"RomM _ {shortCategory} Installed.xml";
                var playlistFilePath = Path.Combine(playlistsDir, playlistFileName);

                var platforms = categoryPlatforms.ContainsKey(category)
                    ? categoryPlatforms[category]
                    : new List<string>();

                var existingLink = parentsRoot.Elements("Parent")
                    .Where(p => (string)p.Element("ParentPlatformCategoryName") == category
                                && !string.IsNullOrEmpty((string)p.Element("PlaylistId")))
                    .FirstOrDefault();

                if (File.Exists(playlistFilePath))
                {
                    var existingDoc = XDocument.Load(playlistFilePath);
                    var playlistId = (string)existingDoc.Root?.Element("Playlist")?.Element("PlaylistId");

                    if (existingLink != null && !string.IsNullOrEmpty((string)existingLink.Element("PlaylistId")))
                    {
                        var linkId = (string)existingLink.Element("PlaylistId");
                        if (playlistId != linkId)
                        {
                            playlistId = linkId;
                            existingDoc.Root.Element("Playlist").Element("PlaylistId").Value = linkId;
                        }
                    }

                    if (string.IsNullOrEmpty(playlistId))
                        playlistId = Guid.NewGuid().ToString();

                    UpdatePlaylistFilters(existingDoc, playlistId, playlistName, platforms);
                    existingDoc.Save(playlistFilePath);
                    Console.WriteLine($"Updated playlist '{playlistName}' with {platforms.Count} platforms");
                }
                else
                {
                    var playlistId = existingLink != null
                        ? (string)existingLink.Element("PlaylistId")
                        : Guid.NewGuid().ToString();

                    var doc = CreateCategoryPlaylist(playlistId, playlistName, platforms);
                    doc.Save(playlistFilePath);
                    Console.WriteLine($"Created playlist '{playlistName}' with {platforms.Count} platforms");
                    changed = true;
                }
            }

            var allInstalledPath = Path.Combine(playlistsDir, "RomM _ Installed Games.xml");
            if (!File.Exists(allInstalledPath))
            {
                var allId = Guid.NewGuid().ToString();
                var doc = CreateAllInstalledPlaylist(allId);
                doc.Save(allInstalledPath);
                Console.WriteLine("Created playlist 'RomM | Installed Games'");
                changed = true;
            }

            return changed;
        }

        static XDocument CreateCategoryPlaylist(string id, string name, List<string> platformNames)
        {
            var doc = new XDocument(
                new XElement("LaunchBox",
                    new XElement("Playlist",
                        new XElement("PlaylistId", id),
                        new XElement("Name", name),
                        new XElement("NestedName", "Installed"),
                        new XElement("SortBy", "Default"),
                        new XElement("Notes"),
                        new XElement("VideoPath"),
                        new XElement("ImageType"),
                        new XElement("Category"),
                        new XElement("LastGameId"),
                        new XElement("BigBoxView"),
                        new XElement("BigBoxTheme"),
                        new XElement("IncludeWithPlatforms", "false"),
                        new XElement("AutoPopulate", "true"),
                        new XElement("SortTitle"),
                        new XElement("IsAutogenerated", "false"),
                        new XElement("LocalDbParsed", "false"),
                        new XElement("LastSelectedChild"),
                        new XElement("Developer"),
                        new XElement("Manufacturer"),
                        new XElement("Cpu"),
                        new XElement("Memory"),
                        new XElement("Graphics"),
                        new XElement("Sound"),
                        new XElement("Display"),
                        new XElement("Media"),
                        new XElement("MaxControllers"),
                        new XElement("Folder"),
                        new XElement("VideosFolder"),
                        new XElement("FrontImagesFolder"),
                        new XElement("BackImagesFolder"),
                        new XElement("ClearLogoImagesFolder"),
                        new XElement("FanartImagesFolder"),
                        new XElement("ScreenshotImagesFolder"),
                        new XElement("BannerImagesFolder"),
                        new XElement("SteamBannerImagesFolder"),
                        new XElement("ManualsFolder"),
                        new XElement("MusicFolder"),
                        new XElement("ScrapeAs"),
                        new XElement("AndroidThemeVideoPath"),
                        new XElement("HideInBigBox", "false"),
                        new XElement("DisableAutoImport", "false")
                    ),
                    new XElement("PlaylistFilter",
                        new XElement("Value", "(Not Used)"),
                        new XElement("FieldKey", "Installed"),
                        new XElement("ComparisonTypeKey", "IsTrue")
                    )
                )
            );

            var launchBox = doc.Root;
            foreach (var platformName in platformNames)
            {
                launchBox.Add(new XElement("PlaylistFilter",
                    new XElement("Value", platformName),
                    new XElement("FieldKey", "Platform"),
                    new XElement("ComparisonTypeKey", "EqualTo")
                ));
            }

            return doc;
        }

        static void UpdatePlaylistFilters(XDocument doc, string id, string name, List<string> platformNames)
        {
            var launchBox = doc.Root;
            if (launchBox == null) return;

            var playlist = launchBox.Element("Playlist");
            if (playlist != null)
            {
                var nameEl = playlist.Element("Name");
                if (nameEl != null) nameEl.Value = name;
            }

            launchBox.Elements("PlaylistFilter").Remove();

            launchBox.Add(new XElement("PlaylistFilter",
                new XElement("Value", "(Not Used)"),
                new XElement("FieldKey", "Installed"),
                new XElement("ComparisonTypeKey", "IsTrue")
            ));

            foreach (var platformName in platformNames)
            {
                launchBox.Add(new XElement("PlaylistFilter",
                    new XElement("Value", platformName),
                    new XElement("FieldKey", "Platform"),
                    new XElement("ComparisonTypeKey", "EqualTo")
                ));
            }
        }

        static XDocument CreateAllInstalledPlaylist(string id)
        {
            return new XDocument(
                new XElement("LaunchBox",
                    new XElement("Playlist",
                        new XElement("PlaylistId", id),
                        new XElement("Name", "RomM | Installed Games"),
                        new XElement("NestedName", "Installed Games"),
                        new XElement("SortBy", "Default"),
                        new XElement("Notes"),
                        new XElement("VideoPath"),
                        new XElement("ImageType"),
                        new XElement("Category"),
                        new XElement("LastGameId"),
                        new XElement("BigBoxView"),
                        new XElement("BigBoxTheme"),
                        new XElement("IncludeWithPlatforms", "false"),
                        new XElement("AutoPopulate", "true"),
                        new XElement("SortTitle"),
                        new XElement("IsAutogenerated", "false"),
                        new XElement("LocalDbParsed", "false"),
                        new XElement("LastSelectedChild"),
                        new XElement("Developer"),
                        new XElement("Manufacturer"),
                        new XElement("Cpu"),
                        new XElement("Memory"),
                        new XElement("Graphics"),
                        new XElement("Sound"),
                        new XElement("Display"),
                        new XElement("Media"),
                        new XElement("MaxControllers"),
                        new XElement("Folder"),
                        new XElement("VideosFolder"),
                        new XElement("FrontImagesFolder"),
                        new XElement("BackImagesFolder"),
                        new XElement("ClearLogoImagesFolder"),
                        new XElement("FanartImagesFolder"),
                        new XElement("ScreenshotImagesFolder"),
                        new XElement("BannerImagesFolder"),
                        new XElement("SteamBannerImagesFolder"),
                        new XElement("ManualsFolder"),
                        new XElement("MusicFolder"),
                        new XElement("ScrapeAs"),
                        new XElement("AndroidThemeVideoPath"),
                        new XElement("HideInBigBox", "false"),
                        new XElement("DisableAutoImport", "false")
                    ),
                    new XElement("PlaylistFilter",
                        new XElement("Value", "(Not Used)"),
                        new XElement("FieldKey", "Installed"),
                        new XElement("ComparisonTypeKey", "IsTrue")
                    ),
                    new XElement("PlaylistFilter",
                        new XElement("Value", "RomM |"),
                        new XElement("FieldKey", "Platform"),
                        new XElement("ComparisonTypeKey", "Contains")
                    )
                )
            );
        }

        static bool SetParentCategory(XElement entry, string parentCategory)
        {
            var parentEl = entry.Element("ParentPlatformCategoryName");
            if (parentEl == null)
            {
                entry.Add(new XElement("ParentPlatformCategoryName", parentCategory));
                Console.WriteLine($"Set parent '{parentCategory}' on entry");
                return true;
            }

            if (string.IsNullOrEmpty(parentEl.Value) || parentEl.Value != parentCategory)
            {
                parentEl.Value = parentCategory;
                Console.WriteLine($"Set parent '{parentCategory}' on entry");
                return true;
            }

            return false;
        }

        static bool DeduplicatePlatforms(XElement root)
        {
            var changed = false;
            var platformEntries = root.Elements("Parent")
                .Where(p => !string.IsNullOrEmpty((string)p.Element("PlatformName")))
                .ToList();

            var grouped = platformEntries
                .GroupBy(p => (string)p.Element("PlatformName"))
                .Where(g => g.Count() > 1);

            foreach (var group in grouped)
            {
                var best = group
                    .OrderByDescending(e => !string.IsNullOrEmpty((string)e.Element("ParentPlatformCategoryName")))
                    .ThenByDescending(e => e.Elements().Count(el => !string.IsNullOrEmpty(el.Value)))
                    .First();

                foreach (var dup in group.Where(e => e != best))
                {
                    dup.Remove();
                    Console.WriteLine($"Removed duplicate platform entry '{group.Key}'");
                    changed = true;
                }
            }

            return changed;
        }

        static bool RemoveOrphanPlaylistLinks(XElement root, List<string> categoriesWithGames)
        {
            var changed = false;

            var orphanLinks = root.Elements("Parent")
                .Where(p =>
                {
                    var playlistId = (string)p.Element("PlaylistId");
                    var platformName = (string)p.Element("PlatformName");
                    var category = (string)p.Element("PlatformCategoryName");
                    var parentCat = (string)p.Element("ParentPlatformCategoryName");

                    if (string.IsNullOrEmpty(playlistId))
                        return false;

                    if (!string.IsNullOrEmpty(platformName) || !string.IsNullOrEmpty(category))
                        return false;

                    if (string.IsNullOrEmpty(parentCat))
                        return true;

                    return parentCat != "RomM"
                        && (!parentCat.StartsWith("RomM | ") || !categoriesWithGames.Contains(parentCat));
                })
                .ToList();

            foreach (var orphan in orphanLinks)
            {
                var id = (string)orphan.Element("PlaylistId");
                orphan.Remove();
                Console.WriteLine($"Removed orphan playlist link '{id}'");
                changed = true;
            }

            return changed;
        }

        static void ValidatePlaylistFiles(string dataDir, XElement root)
        {
            var playlistsDir = Path.Combine(dataDir, "Playlists");
            if (!Directory.Exists(playlistsDir))
                return;

            var playlistFiles = Directory.GetFiles(playlistsDir, "RomM*.xml");
            var allPlaylistIds = root.Elements("Parent")
                .Where(p => !string.IsNullOrEmpty((string)p.Element("PlaylistId")))
                .Select(p => (string)p.Element("PlaylistId"))
                .ToHashSet();

            foreach (var file in playlistFiles)
            {
                try
                {
                    var doc = XDocument.Load(file);
                    var playlistId = (string)doc.Root?.Element("Playlist")?.Element("PlaylistId");
                    if (!string.IsNullOrEmpty(playlistId) && !allPlaylistIds.Contains(playlistId))
                    {
                        Console.WriteLine($"Warning: playlist file '{Path.GetFileName(file)}' (ID: {playlistId}) has no link in Parents.xml");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: could not validate playlist '{Path.GetFileName(file)}': {ex.Message}");
                }
            }
        }

        static void RestartLaunchBox(string launchBoxExe)
        {
            var processes = Process.GetProcessesByName("LaunchBox");
            LogToFile($"Found {processes.Length} LaunchBox process(es)");
            foreach (var proc in processes)
            {
                try
                {
                    proc.Kill();
                    LogToFile($"Killed process: {proc.ProcessName} (PID: {proc.Id})");
                }
                catch (Exception ex)
                {
                    LogToFile($"Could not kill process {proc.ProcessName}: {ex.Message}");
                }
            }

            System.Threading.Thread.Sleep(2000);

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = launchBoxExe,
                    WorkingDirectory = Path.GetDirectoryName(launchBoxExe)
                });
                LogToFile("LaunchBox restarted successfully");
            }
            catch (Exception ex)
            {
                LogToFile($"Could not restart LaunchBox: {ex.Message}");
            }
        }
    }

    public class SyncRequest
    {
        [JsonProperty("PlatformCategoryMap")]
        public Dictionary<string, string> PlatformCategoryMap { get; set; } = new Dictionary<string, string>();

        [JsonProperty("AllCategories")]
        public List<string> AllCategories { get; set; } = new List<string>();

        [JsonProperty("CategoriesWithGames")]
        public List<string> CategoriesWithGames { get; set; } = new List<string>();

        [JsonProperty("CategoryPlatforms")]
        public Dictionary<string, List<string>> CategoryPlatforms { get; set; } = new Dictionary<string, List<string>>();

        [JsonProperty("RestartLaunchBox")]
        public bool RestartLaunchBox { get; set; }

        [JsonProperty("LaunchBoxExe")]
        public string LaunchBoxExe { get; set; }
    }
}
