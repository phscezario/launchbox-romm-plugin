using System;
using System.IO;
using System.Linq;
using RommPlugin.Core.Constants;
using RommPlugin.Core.Helpers;
using RommPlugin.Core.Logging;
using RommPlugin.Core.Storage;

namespace RommPlugin.Services
{
    public class RommBackupService : IRommBackupService
    {
        public void BackupXml(string fileName)
        {
            try
            {
                var dataDir = Path.GetFullPath(Path.Combine(RommPaths.PluginFolder, "..", "..", "Data"));
                var backupDir = Path.Combine(dataDir, RommConstants.BackupFolderName);
                if (!Directory.Exists(backupDir))
                    Directory.CreateDirectory(backupDir);

                var sourcePath = Path.Combine(dataDir, fileName);
                if (!File.Exists(sourcePath))
                    return;

                var baseName = Path.GetFileNameWithoutExtension(fileName);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupPath = Path.Combine(backupDir, $"{baseName}_{timestamp}.xml");

                File.Copy(sourcePath, backupPath, true);
                RommLogger.Log($"Backup created: {Path.GetFileName(backupPath)}");

                var backups = Directory.GetFiles(backupDir, baseName + "_*.xml")
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .ToList();

                while (backups.Count >= RommConstants.MaxXmlBackups)
                {
                    var oldest = backups.Last();
                    backups.RemoveAt(backups.Count - 1);
                    try { File.Delete(oldest); } catch { }
                }
            }
            catch (Exception ex)
            {
                RommLogger.Log($"Backup warning for {fileName}: {ex.Message}");
            }
        }
    }
}
