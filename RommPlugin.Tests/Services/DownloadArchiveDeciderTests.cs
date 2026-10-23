using System;
using System.IO;
using System.IO.Compression;
using RommPlugin.Core.Decisions;
using Xunit;

namespace RommPlugin.Tests.Services
{
    public class DownloadArchiveDeciderTests : IDisposable
    {
        private readonly string _root;

        public DownloadArchiveDeciderTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "dlarchive_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        public void Dispose()
        {
            try { Directory.Delete(_root, true); } catch { }
        }

        private string WriteArchive(string name, bool valid, params string[] entries)
        {
            var path = Path.Combine(_root, name);
            if (!valid)
            {
                // Zip local file header then nothing: the central directory is missing.
                File.WriteAllBytes(path, new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x00 });
                return path;
            }

            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                foreach (var entry in entries)
                {
                    using (var writer = new StreamWriter(archive.CreateEntry(entry).Open()))
                        writer.Write("data");
                }
            }

            return path;
        }

        [Fact]
        public void IsReleasable_AcceptsReadableArchive()
        {
            var zip = WriteArchive("good.zip", true, "game.gbc");
            Assert.True(DownloadArchiveDecider.IsReleasable(zip, true));
        }

        [Fact]
        public void IsReleasable_RejectsCorruptArchive()
        {
            var zip = WriteArchive("truncated.zip", false);
            Assert.False(DownloadArchiveDecider.IsReleasable(zip, true));
        }

        [Fact]
        public void IsReleasable_RejectsMissingFile()
        {
            Assert.False(DownloadArchiveDecider.IsReleasable(Path.Combine(_root, "gone.zip"), true));
        }

        [Fact]
        public void IsReleasable_NeverGatesNonZipContent()
        {
            var iso = Path.Combine(_root, "game.iso");
            File.WriteAllText(iso, "not a zip at all");

            Assert.True(DownloadArchiveDecider.IsReleasable(iso, false));
            // Non-zip content is not probed, so even a missing file passes the gate.
            Assert.True(DownloadArchiveDecider.IsReleasable(Path.Combine(_root, "missing.iso"), false));
        }
    }
}
