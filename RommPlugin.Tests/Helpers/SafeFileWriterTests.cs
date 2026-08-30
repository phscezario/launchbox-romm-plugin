using System.IO;
using RommPlugin.Core.Helpers;
using Xunit;

namespace RommPlugin.Tests.Helpers
{
    public class SafeFileWriterTests
    {
        [Fact]
        public void WriteAllText_CreatesFile()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "romm_test_" + Path.GetRandomFileName());
            var filePath = Path.Combine(tempDir, "test.txt");

            try
            {
                SafeFileWriter.WriteAllText(filePath, "hello");

                Assert.True(File.Exists(filePath));
                Assert.Equal("hello", File.ReadAllText(filePath));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void WriteAllText_OverwritesExistingFile()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "romm_test_" + Path.GetRandomFileName());
            var filePath = Path.Combine(tempDir, "test.txt");

            try
            {
                Directory.CreateDirectory(tempDir);
                File.WriteAllText(filePath, "old content");

                SafeFileWriter.WriteAllText(filePath, "new content");

                Assert.Equal("new content", File.ReadAllText(filePath));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void WriteAllText_CreatesSubdirectory()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "romm_test_" + Path.GetRandomFileName());
            var filePath = Path.Combine(tempDir, "sub", "test.txt");

            try
            {
                SafeFileWriter.WriteAllText(filePath, "content");

                Assert.True(File.Exists(filePath));
                Assert.Equal("content", File.ReadAllText(filePath));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void WriteAllText_CleansUpTempFile()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "romm_test_" + Path.GetRandomFileName());
            var filePath = Path.Combine(tempDir, "test.txt");

            try
            {
                SafeFileWriter.WriteAllText(filePath, "content");

                var files = Directory.GetFiles(tempDir, "*.tmp");
                Assert.Empty(files);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }
    }
}
