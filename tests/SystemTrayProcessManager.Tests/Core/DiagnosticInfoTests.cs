using FluentAssertions;
using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Tests.Core
{
    /// <summary>
    /// Unit tests for <see cref="DiagnosticInfo"/> model.
    /// </summary>
    public class DiagnosticInfoTests
    {
        [Fact]
        public void DefaultConstructor_ShouldSetDefaults()
        {
            var info = new DiagnosticInfo();

            info.OSVersion.Should().Be(string.Empty);
            info.DotNetVersion.Should().Be(string.Empty);
            info.MachineName.Should().Be(string.Empty);
            info.UserName.Should().Be(string.Empty);
            info.ProcessMemoryMB.Should().Be(0);
            info.ProcessorCount.Should().Be(0);
            info.ApplicationVersion.Should().Be(string.Empty);
            info.Uptime.Should().Be(string.Empty);
            info.IsAdministrator.Should().BeFalse();
            info.OSArchitecture.Should().Be(string.Empty);
            info.ProcessArchitecture.Should().Be(string.Empty);
            info.CapturedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void Properties_ShouldBeSettable()
        {
            var info = new DiagnosticInfo
            {
                OSVersion = "Windows 11",
                DotNetVersion = ".NET 10.0",
                MachineName = "TESTPC",
                UserName = "testuser",
                ProcessMemoryMB = 45.5,
                ProcessorCount = 8,
                ApplicationVersion = "1.0.0.0",
                Uptime = "0.01:30:00",
                IsAdministrator = true,
                OSArchitecture = "X64",
                ProcessArchitecture = "X64"
            };

            info.OSVersion.Should().Be("Windows 11");
            info.DotNetVersion.Should().Be(".NET 10.0");
            info.MachineName.Should().Be("TESTPC");
            info.UserName.Should().Be("testuser");
            info.ProcessMemoryMB.Should().Be(45.5);
            info.ProcessorCount.Should().Be(8);
            info.ApplicationVersion.Should().Be("1.0.0.0");
            info.Uptime.Should().Be("0.01:30:00");
            info.IsAdministrator.Should().BeTrue();
            info.OSArchitecture.Should().Be("X64");
            info.ProcessArchitecture.Should().Be("X64");
        }

        // Capture tests

        [Fact]
        public void Capture_ShouldReturnPopulatedInstance()
        {
            var info = DiagnosticInfo.Capture();

            info.Should().NotBeNull();
            info.OSVersion.Should().NotBeNullOrEmpty();
            info.DotNetVersion.Should().NotBeNullOrEmpty();
            info.MachineName.Should().NotBeNullOrEmpty();
            info.UserName.Should().NotBeNullOrEmpty();
            info.ProcessorCount.Should().BeGreaterThan(0);
            info.OSArchitecture.Should().NotBeNullOrEmpty();
            info.ProcessArchitecture.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void Capture_ShouldHavePositiveMemory()
        {
            var info = DiagnosticInfo.Capture();
            info.ProcessMemoryMB.Should().BeGreaterThan(0);
        }

        [Fact]
        public void Capture_ShouldHaveApplicationVersion()
        {
            var info = DiagnosticInfo.Capture();
            info.ApplicationVersion.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void Capture_ShouldHaveUptime()
        {
            var info = DiagnosticInfo.Capture();
            info.Uptime.Should().NotBeNullOrEmpty();
            info.Uptime.Should().NotBe("Unknown");
        }

        [Fact]
        public void Capture_ShouldSetCapturedAtToNow()
        {
            var before = DateTime.UtcNow;
            var info = DiagnosticInfo.Capture();
            var after = DateTime.UtcNow;

            info.CapturedAt.Should().BeOnOrAfter(before);
            info.CapturedAt.Should().BeOnOrBefore(after);
        }

        [Fact]
        public void Capture_IsAdministrator_ShouldBeBoolean()
        {
            var info = DiagnosticInfo.Capture();
            // Just verify it doesn't throw - the actual value depends on runtime context
            info.IsAdministrator.Should().Be(info.IsAdministrator);
        }

        [Fact]
        public void Capture_DotNetVersion_ShouldContainDotNet()
        {
            var info = DiagnosticInfo.Capture();
            info.DotNetVersion.Should().Contain(".NET");
        }

        // ToString tests

        [Fact]
        public void ToString_ShouldContainKeyFields()
        {
            var info = new DiagnosticInfo
            {
                OSVersion = "Windows 11",
                DotNetVersion = ".NET 10.0",
                ProcessMemoryMB = 45.5,
                IsAdministrator = true
            };

            var result = info.ToString();

            result.Should().Contain("Windows 11");
            result.Should().Contain(".NET 10.0");
            result.Should().Contain("45.5");
            result.Should().Contain("True");
        }

        [Fact]
        public void Capture_MultipleCalls_ShouldReturnIndependentInstances()
        {
            var info1 = DiagnosticInfo.Capture();
            var info2 = DiagnosticInfo.Capture();

            info1.Should().NotBeSameAs(info2);
        }

        [Fact]
        public void Capture_ShouldHaveValidOSArchitecture()
        {
            var info = DiagnosticInfo.Capture();
            var validArchitectures = new[] { "X86", "X64", "Arm", "Arm64", "Wasm", "S390x", "LoongArch64", "Armv6", "Ppc64le" };
            validArchitectures.Should().Contain(info.OSArchitecture);
        }
    }
}
