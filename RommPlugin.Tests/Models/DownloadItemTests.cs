using System;
using System.Globalization;
using System.IO;
using System.Threading;
using RommPlugin.Core.Locale;
using RommPlugin.Core.Models;
using Xunit;

namespace RommPlugin.Tests.Models
{
    [Collection("Locale")]
    public class DownloadItemTests
    {
        public DownloadItemTests()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            LocaleFixture.EnsureInitialized();
        }
        [Fact]
        public void Percentage_ReturnsZero_WhenTotalBytesIsZero()
        {
            var item = new DownloadItem { TotalBytes = 0, BytesReceived = 0 };
            Assert.Equal(0, item.Percentage);
        }

        [Fact]
        public void Percentage_ReturnsZero_WhenTotalBytesIsNegative()
        {
            var item = new DownloadItem { TotalBytes = -100, BytesReceived = 50 };
            Assert.Equal(0, item.Percentage);
        }

        [Theory]
        [InlineData(100, 50, 50)]
        [InlineData(100, 0, 0)]
        [InlineData(100, 100, 100)]
        [InlineData(200, 50, 25)]
        [InlineData(1000, 333, 33)]
        public void Percentage_ReturnsCorrectValue(long total, long received, int expected)
        {
            var item = new DownloadItem { TotalBytes = total, BytesReceived = received };
            Assert.Equal(expected, item.Percentage);
        }

        [Fact]
        public void Percentage_CapsAt100()
        {
            var item = new DownloadItem { TotalBytes = 100, BytesReceived = 200 };
            Assert.Equal(100, item.Percentage);
        }

        [Fact]
        public void SpeedText_ReturnsDoubleDash_WhenZero()
        {
            var item = new DownloadItem { SpeedBytesPerSecond = 0 };
            Assert.Equal("--", item.SpeedText);
        }

        [Fact]
        public void SpeedText_ReturnsDoubleDash_WhenNegative()
        {
            var item = new DownloadItem { SpeedBytesPerSecond = -1 };
            Assert.Equal("--", item.SpeedText);
        }

        [Theory]
        [InlineData(500, "500 B/s")]
        [InlineData(1023, "1023 B/s")]
        [InlineData(1024, "1.0 KB/s")]
        [InlineData(1536, "1.5 KB/s")]
        [InlineData(1048576, "1.0 MB/s")]
        [InlineData(2097152, "2.0 MB/s")]
        public void SpeedText_FormatsCorrectly(double speed, string expected)
        {
            var item = new DownloadItem { SpeedBytesPerSecond = speed };
            Assert.Equal(expected, item.SpeedText);
        }

        [Fact]
        public void TimeRemainingText_ReturnsDoubleDash_WhenNotDownloading()
        {
            var item = new DownloadItem
            {
                Status = DownloadStatus.Pending,
                EstimatedTimeRemaining = TimeSpan.FromSeconds(30)
            };
            Assert.Equal("--", item.TimeRemainingText);
        }

        [Fact]
        public void TimeRemainingText_ReturnsDoubleDash_WhenZero()
        {
            var item = new DownloadItem
            {
                Status = DownloadStatus.Downloading,
                EstimatedTimeRemaining = TimeSpan.Zero
            };
            Assert.Equal("--", item.TimeRemainingText);
        }

        [Fact]
        public void TimeRemainingText_ShowsSeconds_WhenLessThanMinute()
        {
            var item = new DownloadItem
            {
                Status = DownloadStatus.Downloading,
                EstimatedTimeRemaining = TimeSpan.FromSeconds(30)
            };
            Assert.Equal("30s", item.TimeRemainingText);
        }

        [Fact]
        public void TimeRemainingText_ShowsMinutesAndSeconds()
        {
            var item = new DownloadItem
            {
                Status = DownloadStatus.Downloading,
                EstimatedTimeRemaining = TimeSpan.FromMinutes(2.5)
            };
            Assert.Equal("2m 30s", item.TimeRemainingText);
        }

        [Fact]
        public void TimeRemainingText_ShowsHoursAndMinutes()
        {
            var item = new DownloadItem
            {
                Status = DownloadStatus.Downloading,
                EstimatedTimeRemaining = TimeSpan.FromHours(1.5)
            };
            Assert.Equal("1h 30m", item.TimeRemainingText);
        }

        [Fact]
        public void SizeText_ReturnsDoubleDash_WhenZero()
        {
            var item = new DownloadItem { TotalBytes = 0 };
            Assert.Equal("--", item.SizeText);
        }

        [Theory]
        [InlineData(500, "500 B")]
        [InlineData(1024, "1.0 KB")]
        [InlineData(1048576, "1.0 MB")]
        [InlineData(1073741824, "1.00 GB")]
        public void SizeText_FormatsCorrectly(long bytes, string expected)
        {
            var item = new DownloadItem { TotalBytes = bytes };
            Assert.Equal(expected, item.SizeText);
        }

        [Theory]
        [InlineData(DownloadStatus.Pending, "dm.status.pending")]
        [InlineData(DownloadStatus.Downloading, "dm.status.downloading")]
        [InlineData(DownloadStatus.Paused, "dm.status.paused")]
        [InlineData(DownloadStatus.Completed, "dm.status.completed")]
        [InlineData(DownloadStatus.Failed, "dm.status.failed")]
        [InlineData(DownloadStatus.WaitingInstall, "gm.status.installing")]
        [InlineData(DownloadStatus.WaitingUninstall, "gm.status.pending_uninstall")]
        [InlineData(DownloadStatus.Installed, "gm.status.installed")]
        public void StatusText_ReturnsLocalizedText(DownloadStatus status, string expectedKey)
        {
            var item = new DownloadItem { Status = status };
            var expected = LocaleManager.Get(expectedKey);
            Assert.Equal(expected, item.StatusText);
        }
    }
}
