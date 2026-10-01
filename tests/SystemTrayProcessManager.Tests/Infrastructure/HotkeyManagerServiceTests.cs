using Microsoft.Extensions.Logging.Abstractions;
using SystemTrayProcessManager.Infrastructure.Services;

namespace SystemTrayProcessManager.Tests.Infrastructure;

/// <summary>Tests key-repeat handling without installing a global keyboard hook.</summary>
[Collection("Wpf")]
public class HotkeyManagerServiceTests
{
    [Fact]
    public async Task Initialize_InstallsRealWindowsHookAndReleasesIt()
    {
        await SystemTrayProcessManager.Tests.UI.HotkeyCaptureBoxTests.RunStaAsync(() =>
        {
            using var service = new HotkeyManagerService(NullLogger<HotkeyManagerService>.Instance);
            service.Initialize();
            Assert.True(service.IsInitialized);
            service.Shutdown();
            Assert.False(service.IsInitialized);
            // Verify cleanup permits a fresh installation.
            service.Initialize();
            Assert.True(service.IsInitialized);
        });
    }

    [Fact]
    public void HeldKey_OnlyTriggersAgainAfterRelease()
    {
        using var service = new HotkeyManagerService(NullLogger<HotkeyManagerService>.Instance);
        Assert.True(service.TryBeginKeyPress(0x4D, true));
        Assert.False(service.TryBeginKeyPress(0x4D, true));
        Assert.False(service.TryBeginKeyPress(0x4D, true));
        Assert.False(service.TryBeginKeyPress(0x4D, false));
        Assert.True(service.TryBeginKeyPress(0x4D, true));
    }

    [Fact]
    public void Shutdown_ClearsHeldKeys()
    {
        using var service = new HotkeyManagerService(NullLogger<HotkeyManagerService>.Instance);
        service.TryBeginKeyPress(0x4D, true);
        service.Shutdown();
        Assert.True(service.TryBeginKeyPress(0x4D, true));
    }
}
