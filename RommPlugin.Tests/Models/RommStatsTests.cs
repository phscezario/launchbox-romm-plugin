using System;
using RommPlugin.Core.Models;
using Xunit;

namespace RommPlugin.Tests.Models
{
    public class RommStatsTests
    {
        [Fact]
        public void TotalPlayTimeSeconds_ConvertsCorrectly()
        {
            var stats = new RommStats { TotalPlayTimeMs = 3600000 };
            Assert.Equal(3600, stats.TotalPlayTimeSeconds);
        }

        [Fact]
        public void TotalPlayTimeSeconds_ZeroMilliseconds()
        {
            var stats = new RommStats { TotalPlayTimeMs = 0 };
            Assert.Equal(0, stats.TotalPlayTimeSeconds);
        }

        [Fact]
        public void TotalPlayTimeSeconds_TruncatesDecimal()
        {
            var stats = new RommStats { TotalPlayTimeMs = 1500 };
            Assert.Equal(1, stats.TotalPlayTimeSeconds);
        }

        [Fact]
        public void LastPlayed_NullByDefault()
        {
            var stats = new RommStats();
            Assert.Null(stats.LastPlayed);
        }

        [Fact]
        public void PlayCount_DefaultsToZero()
        {
            var stats = new RommStats();
            Assert.Equal(0, stats.PlayCount);
        }

        [Fact]
        public void TotalPlayTimeMs_DefaultsToZero()
        {
            var stats = new RommStats();
            Assert.Equal(0, stats.TotalPlayTimeMs);
        }
    }
}
