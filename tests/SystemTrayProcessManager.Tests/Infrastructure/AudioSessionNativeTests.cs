using Microsoft.Extensions.Logging.Abstractions;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using SystemTrayProcessManager.Infrastructure.Services;

namespace SystemTrayProcessManager.Tests.Infrastructure;

/// <summary>Exercises late session discovery against WASAPI using only a silent test stream.</summary>
[Collection("Wpf")]
public class AudioSessionNativeTests
{
    [Fact]
    public async Task SilentStreamStartedAfterDiscovery_CanBeMutedAndUnmuted()
    {
        using var service = new AudioManagerService(NullLogger<AudioManagerService>.Instance);
        // Hardware-dependent check: deterministic provider tests cover machines without audio.
        if (!service.IsAudioAvailable) return;

        await service.GetAudioProcessesAsync(); // Populate the initial discovery before the stream exists.
        using var enumerator = new MMDeviceEnumerator();
        using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        using var client = device.AudioClient;
        using var output = new WasapiOut(device, AudioClientShareMode.Shared, true, 100);
        output.Init(new SilenceProvider(client.MixFormat));
        output.Play();
        try
        {
            var sessions = await service.GetAudioProcessesAsync();
            Assert.Contains(sessions, s => s.ProcessId == Environment.ProcessId);
            Assert.True(await service.MuteProcessAsync(Environment.ProcessId));
            Assert.True(await service.IsProcessMutedAsync(Environment.ProcessId));
            Assert.True(await service.ToggleMuteProcessAsync(Environment.ProcessId));
            Assert.False(await service.IsProcessMutedAsync(Environment.ProcessId));
        }
        finally
        {
            await service.UnmuteProcessAsync(Environment.ProcessId);
            output.Stop();
        }
    }
}
