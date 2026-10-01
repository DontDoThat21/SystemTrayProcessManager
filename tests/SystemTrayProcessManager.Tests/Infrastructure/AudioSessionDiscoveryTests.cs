using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SystemTrayProcessManager.Core.Services;
using SystemTrayProcessManager.Infrastructure.Services;

namespace SystemTrayProcessManager.Tests.Infrastructure;

/// <summary>Regression tests for late-started games, changing outputs, and multiple sessions.</summary>
public class AudioSessionDiscoveryTests
{
    [Fact]
    public async Task GameStartedAfterManager_IsDiscoveredOnNextMute()
    {
        var provider = new Mock<IAudioSessionProvider>();
        var game = Session(42, "Game", "Headphones");
        provider.SetupSequence(p => p.GetSessions()).Returns(Array.Empty<IAudioSession>()).Returns(new[] { game.Object });
        using var service = new AudioManagerService(NullLogger<AudioManagerService>.Instance, provider.Object);
        Assert.Empty(await service.GetAudioProcessesAsync());
        Assert.True(await service.ToggleMuteProcessAsync(42));
        Assert.True(game.Object.IsMuted);
        game.Verify(s => s.Dispose(), Times.Once);
        provider.Verify(p => p.GetSessions(), Times.Exactly(2));
    }

    [Fact]
    public async Task Toggle_ChangesEverySessionOnEveryOutput_WithoutChangingOtherApps()
    {
        var game = new[]
        {
            Session(42, "Game", "Headphones"), Session(42, "Game", "Controller"),
            Session(42, "Game", "Headphones"), Session(42, "Game", "Monitor")
        };
        var browser = Session(99, "Browser", "Headphones");
        var provider = Provider(game.Append(browser).ToArray());
        using var service = new AudioManagerService(NullLogger<AudioManagerService>.Instance, provider.Object);

        Assert.True(await service.ToggleMuteProcessAsync(42));
        Assert.All(game, session => Assert.True(session.Object.IsMuted));
        Assert.False(browser.Object.IsMuted);
        Assert.True(await service.ToggleMuteProcessAsync(42));
        Assert.All(game, session => Assert.False(session.Object.IsMuted));
        browser.VerifySet(s => s.IsMuted = It.IsAny<bool>(), Times.Never);
        foreach (var session in game.Append(browser)) session.Verify(s => s.Dispose(), Times.Exactly(2));
    }

    [Fact]
    public async Task MixedMuteStates_ConvergeInsteadOfInvertingEachSession()
    {
        var first = Session(42, "Game", "Headphones");
        var second = Session(42, "Game", "Controller");
        first.Object.IsMuted = true;
        using var service = new AudioManagerService(NullLogger<AudioManagerService>.Instance, Provider(first, second).Object);
        Assert.False(await service.IsProcessMutedAsync(42));
        Assert.True(await service.ToggleMuteProcessAsync(42));
        Assert.True(await service.IsProcessMutedAsync(42));
        Assert.True(second.Object.IsMuted);
    }

    [Fact]
    public async Task OutputSwitch_UsesNewSessionRatherThanStaleOne()
    {
        var oldSession = Session(42, "Game", "Monitor");
        var newSession = Session(42, "Game", "Headphones");
        var provider = new Mock<IAudioSessionProvider>();
        provider.SetupSequence(p => p.GetSessions()).Returns(new[] { oldSession.Object }).Returns(new[] { newSession.Object });
        using var service = new AudioManagerService(NullLogger<AudioManagerService>.Instance, provider.Object);
        await service.GetAudioProcessesAsync();
        Assert.True(await service.ToggleMuteProcessAsync(42));
        Assert.True(newSession.Object.IsMuted);
        Assert.False(oldSession.Object.IsMuted);
        oldSession.VerifySet(s => s.IsMuted = It.IsAny<bool>(), Times.Never);
        oldSession.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public async Task ExactPid_TakesPriorityOverNameFallback()
    {
        var exact = Session(42, "Game", "Headphones");
        var other = Session(43, "Game", "Monitor");
        var provider = Provider(exact, other);
        provider.Setup(p => p.GetProcessName(42)).Returns("Game");
        using var service = new AudioManagerService(NullLogger<AudioManagerService>.Instance, provider.Object);
        Assert.True(await service.MuteProcessAsync(42));
        Assert.True(exact.Object.IsMuted);
        Assert.False(other.Object.IsMuted);
    }

    [Fact]
    public async Task WindowProcessWithoutSession_FindsAllSameNameAudioChildren()
    {
        var first = Session(43, "Browser", "Headphones");
        var second = Session(44, "Browser", "Controller");
        var unrelated = Session(99, "Other", "Headphones");
        var provider = Provider(first, second, unrelated);
        provider.Setup(p => p.GetProcessName(42)).Returns("browser");
        using var service = new AudioManagerService(NullLogger<AudioManagerService>.Instance, provider.Object);
        Assert.True(await service.MuteProcessAsync(42));
        Assert.True(first.Object.IsMuted);
        Assert.True(second.Object.IsMuted);
        Assert.False(unrelated.Object.IsMuted);
    }

    [Fact]
    public async Task BrokenSession_DoesNotPreventOtherOutputsFromBeingMuted_AndDisposesAll()
    {
        var broken = Session(42, "Game", "Unplugged");
        var healthy = Session(42, "Game", "Headphones");
        broken.SetupSet(s => s.IsMuted = true).Throws(new InvalidOperationException("Disconnected"));
        using var service = new AudioManagerService(NullLogger<AudioManagerService>.Instance, Provider(broken, healthy).Object);
        Assert.False(await service.MuteProcessAsync(42));
        Assert.True(healthy.Object.IsMuted);
        broken.Verify(s => s.Dispose(), Times.Once);
        healthy.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public async Task Enumeration_AggregatesMultipleSessionsIntoOneProcess()
    {
        var first = Session(42, "Game", "Headphones");
        var second = Session(42, "Game", "Controller");
        second.Object.Volume = 0.5f;
        using var service = new AudioManagerService(NullLogger<AudioManagerService>.Instance, Provider(first, second).Object);
        var info = Assert.Single(await service.GetAudioProcessesAsync());
        Assert.Equal(42, info.ProcessId);
        Assert.Equal(1f, info.Volume);
        Assert.True(await service.SetProcessVolumeAsync(42, 0.2f));
        Assert.Equal(0.2f, first.Object.Volume);
        Assert.Equal(0.2f, second.Object.Volume);
    }

    [Fact]
    public async Task ConcurrentToggles_AreSerialized()
    {
        var game = Session(42, "Game", "Headphones");
        using var service = new AudioManagerService(NullLogger<AudioManagerService>.Instance, Provider(game).Object);
        var results = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => service.ToggleMuteProcessAsync(42)));
        Assert.All(results, Assert.True);
        Assert.False(game.Object.IsMuted);
    }

    [Fact]
    public async Task Disposal_PreventsFurtherNativeQueries()
    {
        var provider = Provider();
        var service = new AudioManagerService(NullLogger<AudioManagerService>.Instance, provider.Object);
        service.Dispose();
        Assert.False(await service.ToggleMuteProcessAsync(42));
        Assert.Empty(await service.GetAudioProcessesAsync());
        provider.Verify(p => p.GetSessions(), Times.Never);
    }

    private static Mock<IAudioSessionProvider> Provider(params Mock<IAudioSession>[] sessions)
    {
        var provider = new Mock<IAudioSessionProvider>();
        provider.Setup(p => p.GetSessions()).Returns(() => sessions.Select(s => s.Object).ToArray());
        return provider;
    }

    private static Mock<IAudioSession> Session(int processId, string name, string device)
    {
        var session = new Mock<IAudioSession>();
        session.SetupGet(s => s.ProcessId).Returns(processId);
        session.SetupGet(s => s.ProcessName).Returns(name);
        session.SetupGet(s => s.DeviceId).Returns(device);
        session.SetupGet(s => s.SessionIdentifier).Returns(Guid.NewGuid().ToString());
        session.SetupGet(s => s.DisplayName).Returns(name);
        session.SetupProperty(s => s.IsMuted, false);
        session.SetupProperty(s => s.Volume, 1f);
        return session;
    }
}
