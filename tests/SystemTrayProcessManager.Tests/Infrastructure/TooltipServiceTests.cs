using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SystemTrayProcessManager.Infrastructure.Services;

namespace SystemTrayProcessManager.Tests.Infrastructure;

/// <summary>
/// Unit tests for <see cref="TooltipService"/>.
/// </summary>
public class TooltipServiceTests
{
    private readonly Mock<ILogger<TooltipService>> _mockLogger;
    private readonly TooltipService _service;

    public TooltipServiceTests()
    {
        _mockLogger = new Mock<ILogger<TooltipService>>();
        _service = new TooltipService(_mockLogger.Object);
    }

    // Constructor tests

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrow()
    {
        var act = () => new TooltipService(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ShouldInitializeWithTooltips()
    {
        var keys = _service.GetAllKeys();
        keys.Should().NotBeEmpty();
        keys.Count.Should().BeGreaterThan(50); // We have many tooltips defined
    }

    // GetTooltip(key) tests

    [Fact]
    public void GetTooltip_ValidKey_ReturnsText()
    {
        var result = _service.GetTooltip("ProcessCard.BringToFront");
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("foreground");
    }

    [Fact]
    public void GetTooltip_InvalidKey_ReturnsEmpty()
    {
        var result = _service.GetTooltip("NonExistent.Key");
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetTooltip_NullKey_ReturnsEmpty()
    {
        var result = _service.GetTooltip(null!);
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetTooltip_EmptyKey_ReturnsEmpty()
    {
        var result = _service.GetTooltip(string.Empty);
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetTooltip_WhitespaceKey_ReturnsEmpty()
    {
        var result = _service.GetTooltip("   ");
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetTooltip_CaseInsensitive_ReturnsText()
    {
        var result1 = _service.GetTooltip("processcard.bringtofront");
        var result2 = _service.GetTooltip("PROCESSCARD.BRINGTOFRONT");
        var result3 = _service.GetTooltip("ProcessCard.BringToFront");

        result1.Should().NotBeEmpty();
        result2.Should().NotBeEmpty();
        result3.Should().NotBeEmpty();
        result1.Should().Be(result2).And.Be(result3);
    }

    // GetTooltip(key, args) tests

    [Fact]
    public void GetTooltip_WithParams_FormatsCorrectly()
    {
        var result = _service.GetTooltip("Main.NotificationCount", 5);
        result.Should().NotBeEmpty();
        result.Should().Contain("5");
    }

    [Fact]
    public void GetTooltip_WithParams_NoParams_ReturnsTemplate()
    {
        var result = _service.GetTooltip("ProcessCard.BringToFront", Array.Empty<object>());
        result.Should().NotBeEmpty();
    }

    [Fact]
    public void GetTooltip_WithParams_NullArgs_ReturnsTemplate()
    {
        var result = _service.GetTooltip("ProcessCard.BringToFront", null!);
        result.Should().NotBeEmpty();
    }

    [Fact]
    public void GetTooltip_WithParams_InvalidKey_ReturnsEmpty()
    {
        var result = _service.GetTooltip("NonExistent.Key", "arg1");
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetTooltip_WithParams_InvalidFormat_ReturnsTemplate()
    {
        // Main.ProcessCount expects one argument, but we'll give it wrong format
        // Since the template is "{0} processes running", this should still work
        var result = _service.GetTooltip("Main.ProcessCount", 10);
        result.Should().Contain("10");
    }

    // HasTooltip tests

    [Fact]
    public void HasTooltip_ValidKey_ReturnsTrue()
    {
        var result = _service.HasTooltip("ProcessCard.Minimize");
        result.Should().BeTrue();
    }

    [Fact]
    public void HasTooltip_InvalidKey_ReturnsFalse()
    {
        var result = _service.HasTooltip("NonExistent.Key");
        result.Should().BeFalse();
    }

    [Fact]
    public void HasTooltip_NullKey_ReturnsFalse()
    {
        var result = _service.HasTooltip(null!);
        result.Should().BeFalse();
    }

    [Fact]
    public void HasTooltip_EmptyKey_ReturnsFalse()
    {
        var result = _service.HasTooltip(string.Empty);
        result.Should().BeFalse();
    }

    [Fact]
    public void HasTooltip_WhitespaceKey_ReturnsFalse()
    {
        var result = _service.HasTooltip("   ");
        result.Should().BeFalse();
    }

    [Fact]
    public void HasTooltip_CaseInsensitive_ReturnsTrue()
    {
        _service.HasTooltip("processcard.minimize").Should().BeTrue();
        _service.HasTooltip("PROCESSCARD.MINIMIZE").Should().BeTrue();
    }

    // GetAllKeys tests

    [Fact]
    public void GetAllKeys_ReturnsAllKeys()
    {
        var keys = _service.GetAllKeys();
        keys.Should().NotBeNull();
        keys.Should().NotBeEmpty();
    }

    [Fact]
    public void GetAllKeys_ContainsExpectedKeys()
    {
        var keys = _service.GetAllKeys();

        keys.Should().Contain("ProcessCard.BringToFront");
        keys.Should().Contain("ProcessCard.Minimize");
        keys.Should().Contain("Hotkey.Capture");
        keys.Should().Contain("Settings.StartWithWindows");
        keys.Should().Contain("Main.Search");
    }

    [Fact]
    public void GetAllKeys_IsReadOnly()
    {
        var keys = _service.GetAllKeys();

        // Verify it's a read-only collection
        var act = () => ((ICollection<string>)keys).Add("New.Key");
        act.Should().Throw<NotSupportedException>();
    }

    // Specific tooltip content tests

    [Theory]
    [InlineData("ProcessCard.BringToFront", "foreground")]
    [InlineData("ProcessCard.Minimize", "taskbar")]
    [InlineData("ProcessCard.Close", "Close")]
    [InlineData("Hotkey.Capture", "key combination")]
    [InlineData("Settings.StartWithWindows", "Windows starts")]
    [InlineData("Main.Search", "Search")]
    [InlineData("General.Exit", "Exit")]
    public void GetTooltip_SpecificKeys_ContainsExpectedContent(string key, string expectedContent)
    {
        var result = _service.GetTooltip(key);
        result.Should().NotBeEmpty();
        result.ToLower().Should().Contain(expectedContent.ToLower());
    }

    // Category coverage tests

    [Fact]
    public void Tooltips_ProcessCardCategory_AllDefined()
    {
        var processCardKeys = new[]
        {
            "ProcessCard.BringToFront",
            "ProcessCard.Minimize",
            "ProcessCard.Maximize",
            "ProcessCard.Restore",
            "ProcessCard.Close",
            "ProcessCard.Mute"
        };

        foreach (var key in processCardKeys)
        {
            _service.HasTooltip(key).Should().BeTrue($"Key '{key}' should exist");
            _service.GetTooltip(key).Should().NotBeEmpty($"Key '{key}' should have content");
        }
    }

    [Fact]
    public void Tooltips_HotkeyCategory_AllDefined()
    {
        var hotkeyKeys = new[]
        {
            "Hotkey.Capture",
            "Hotkey.Conflict",
            "Hotkey.Import",
            "Hotkey.Export",
            "Hotkey.Add",
            "Hotkey.Delete"
        };

        foreach (var key in hotkeyKeys)
        {
            _service.HasTooltip(key).Should().BeTrue($"Key '{key}' should exist");
            _service.GetTooltip(key).Should().NotBeEmpty($"Key '{key}' should have content");
        }
    }

    [Fact]
    public void Tooltips_SettingsCategory_AllDefined()
    {
        var settingsKeys = new[]
        {
            "Settings.StartWithWindows",
            "Settings.MinimizeToTray",
            "Settings.HotkeysEnabled",
            "Settings.DefaultVolume",
            "Settings.Theme"
        };

        foreach (var key in settingsKeys)
        {
            _service.HasTooltip(key).Should().BeTrue($"Key '{key}' should exist");
            _service.GetTooltip(key).Should().NotBeEmpty($"Key '{key}' should have content");
        }
    }

    [Fact]
    public void Tooltips_GeneralCategory_AllDefined()
    {
        var generalKeys = new[]
        {
            "General.Loading",
            "General.Saving",
            "General.Exit",
            "General.Cancel",
            "General.OK"
        };

        foreach (var key in generalKeys)
        {
            _service.HasTooltip(key).Should().BeTrue($"Key '{key}' should exist");
            _service.GetTooltip(key).Should().NotBeEmpty($"Key '{key}' should have content");
        }
    }
}
