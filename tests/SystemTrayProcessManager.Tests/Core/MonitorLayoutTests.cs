using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Tests.Core
{
    public class MonitorLayoutTests
    {
        [Fact]
        public void Create_WithMonitors_ComputesLayoutHash()
        {
            var monitors = new List<MonitorInfo>
            {
                MonitorInfo.Create("Monitor1", 0, 0, 1920, 1080, true),
                MonitorInfo.Create("Monitor2", 1920, 0, 1920, 1080, false)
            };

            MonitorLayout layout = MonitorLayout.Create(monitors);

            Assert.NotEmpty(layout.LayoutHash);
            Assert.Equal(2, layout.MonitorCount);
            Assert.Equal(2, layout.Monitors.Count);
        }

        [Fact]
        public void Create_WithNullMonitors_ReturnsEmptyLayout()
        {
            MonitorLayout layout = MonitorLayout.Create(null!);

            Assert.Equal("empty", layout.LayoutHash);
            Assert.Equal(0, layout.MonitorCount);
            Assert.Empty(layout.Monitors);
        }

        [Fact]
        public void Create_WithEmptyMonitors_ReturnsEmptyLayout()
        {
            MonitorLayout layout = MonitorLayout.Create(Enumerable.Empty<MonitorInfo>());

            Assert.Equal("empty", layout.LayoutHash);
            Assert.Equal(0, layout.MonitorCount);
            Assert.Empty(layout.Monitors);
        }

        [Fact]
        public void Create_SameMonitors_ProducesSameHash()
        {
            var monitors1 = new List<MonitorInfo>
            {
                MonitorInfo.Create("Monitor1", 0, 0, 1920, 1080, true)
            };
            var monitors2 = new List<MonitorInfo>
            {
                MonitorInfo.Create("Monitor1", 0, 0, 1920, 1080, true)
            };

            MonitorLayout layout1 = MonitorLayout.Create(monitors1);
            MonitorLayout layout2 = MonitorLayout.Create(monitors2);

            Assert.Equal(layout1.LayoutHash, layout2.LayoutHash);
        }

        [Fact]
        public void Create_DifferentMonitors_ProducesDifferentHash()
        {
            var monitors1 = new List<MonitorInfo>
            {
                MonitorInfo.Create("Monitor1", 0, 0, 1920, 1080, true)
            };
            var monitors2 = new List<MonitorInfo>
            {
                MonitorInfo.Create("Monitor1", 0, 0, 2560, 1440, true) // Different resolution
            };

            MonitorLayout layout1 = MonitorLayout.Create(monitors1);
            MonitorLayout layout2 = MonitorLayout.Create(monitors2);

            Assert.NotEqual(layout1.LayoutHash, layout2.LayoutHash);
        }

        [Fact]
        public void Create_MonitorsInDifferentOrder_ProducesSameHash()
        {
            var monitors1 = new List<MonitorInfo>
            {
                MonitorInfo.Create("Monitor1", 0, 0, 1920, 1080, true),
                MonitorInfo.Create("Monitor2", 1920, 0, 1920, 1080, false)
            };
            var monitors2 = new List<MonitorInfo>
            {
                MonitorInfo.Create("Monitor2", 1920, 0, 1920, 1080, false),
                MonitorInfo.Create("Monitor1", 0, 0, 1920, 1080, true)
            };

            MonitorLayout layout1 = MonitorLayout.Create(monitors1);
            MonitorLayout layout2 = MonitorLayout.Create(monitors2);

            Assert.Equal(layout1.LayoutHash, layout2.LayoutHash);
        }

        [Fact]
        public void PrimaryMonitor_ReturnsPrimaryMonitor()
        {
            var monitors = new List<MonitorInfo>
            {
                MonitorInfo.Create("Secondary", 1920, 0, 1920, 1080, false),
                MonitorInfo.Create("Primary", 0, 0, 1920, 1080, true)
            };

            MonitorLayout layout = MonitorLayout.Create(monitors);

            Assert.NotNull(layout.PrimaryMonitor);
            Assert.True(layout.PrimaryMonitor.IsPrimary);
            Assert.Equal("Primary", layout.PrimaryMonitor.DeviceName);
        }

        [Fact]
        public void PrimaryMonitor_NoPrimaryMonitor_ReturnsNull()
        {
            var monitors = new List<MonitorInfo>
            {
                MonitorInfo.Create("Monitor1", 0, 0, 1920, 1080, false),
                MonitorInfo.Create("Monitor2", 1920, 0, 1920, 1080, false)
            };

            MonitorLayout layout = MonitorLayout.Create(monitors);

            Assert.Null(layout.PrimaryMonitor);
        }

        [Fact]
        public void IsSameConfiguration_SameHash_ReturnsTrue()
        {
            var monitors = new List<MonitorInfo>
            {
                MonitorInfo.Create("Monitor1", 0, 0, 1920, 1080, true)
            };
            MonitorLayout layout1 = MonitorLayout.Create(monitors);
            MonitorLayout layout2 = MonitorLayout.Create(monitors);

            bool result = layout1.IsSameConfiguration(layout2);

            Assert.True(result);
        }

        [Fact]
        public void IsSameConfiguration_DifferentHash_ReturnsFalse()
        {
            var monitors1 = new List<MonitorInfo>
            {
                MonitorInfo.Create("Monitor1", 0, 0, 1920, 1080, true)
            };
            var monitors2 = new List<MonitorInfo>
            {
                MonitorInfo.Create("Monitor1", 0, 0, 2560, 1440, true)
            };
            MonitorLayout layout1 = MonitorLayout.Create(monitors1);
            MonitorLayout layout2 = MonitorLayout.Create(monitors2);

            bool result = layout1.IsSameConfiguration(layout2);

            Assert.False(result);
        }

        [Fact]
        public void IsSameConfiguration_NullOther_ReturnsFalse()
        {
            var monitors = new List<MonitorInfo>
            {
                MonitorInfo.Create("Monitor1", 0, 0, 1920, 1080, true)
            };
            MonitorLayout layout = MonitorLayout.Create(monitors);

            bool result = layout.IsSameConfiguration(null);

            Assert.False(result);
        }

        [Fact]
        public void LayoutHash_Is16Characters()
        {
            var monitors = new List<MonitorInfo>
            {
                MonitorInfo.Create("Monitor1", 0, 0, 1920, 1080, true)
            };

            MonitorLayout layout = MonitorLayout.Create(monitors);

            Assert.Equal(16, layout.LayoutHash.Length);
        }

        [Fact]
        public void LayoutHash_IsLowercase()
        {
            var monitors = new List<MonitorInfo>
            {
                MonitorInfo.Create("Monitor1", 0, 0, 1920, 1080, true)
            };

            MonitorLayout layout = MonitorLayout.Create(monitors);

            Assert.Equal(layout.LayoutHash.ToLowerInvariant(), layout.LayoutHash);
        }
    }
}
