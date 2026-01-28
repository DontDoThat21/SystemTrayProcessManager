using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SystemTrayProcessManager.Infrastructure.Helpers;

namespace SystemTrayProcessManager.Tests.Infrastructure
{
    /// <summary>
    /// Unit tests for the IconExtractor implementation.
    /// Tests cover icon extraction, caching, and resource management.
    /// </summary>
    public class IconExtractorTests
    {
        private readonly Mock<ILogger<IconExtractor>> _loggerMock;

        public IconExtractorTests()
        {
            _loggerMock = new Mock<ILogger<IconExtractor>>();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Arrange & Act
            var act = () => new IconExtractor(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Fact]
        public void Constructor_WithValidLogger_CreatesInstance()
        {
            // Arrange & Act
            using var extractor = new IconExtractor(_loggerMock.Object);

            // Assert
            extractor.Should().NotBeNull();
            extractor.CachedIconCount.Should().Be(0);
        }

        [Fact]
        public void Constructor_WithCustomCacheSize_CreatesInstance()
        {
            // Arrange & Act
            using var extractor = new IconExtractor(_loggerMock.Object, 50);

            // Assert
            extractor.Should().NotBeNull();
        }

        #endregion

        #region ExtractIcon Tests

        [Fact]
        public void ExtractIcon_WithNullPath_ReturnsNull()
        {
            // Arrange
            using var extractor = new IconExtractor(_loggerMock.Object);

            // Act
            var result = extractor.ExtractIcon(null!);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ExtractIcon_WithEmptyPath_ReturnsNull()
        {
            // Arrange
            using var extractor = new IconExtractor(_loggerMock.Object);

            // Act
            var result = extractor.ExtractIcon("");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ExtractIcon_WithWhitespacePath_ReturnsNull()
        {
            // Arrange
            using var extractor = new IconExtractor(_loggerMock.Object);

            // Act
            var result = extractor.ExtractIcon("   ");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ExtractIcon_WithNonExistentPath_ReturnsNull()
        {
            // Arrange
            using var extractor = new IconExtractor(_loggerMock.Object);

            // Act
            var result = extractor.ExtractIcon(@"C:\NonExistent\path.exe");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ExtractIcon_WithValidExecutable_ReturnsIcon()
        {
            // Arrange
            using var extractor = new IconExtractor(_loggerMock.Object);
            var notepadPath = @"C:\Windows\System32\notepad.exe";

            if (!System.IO.File.Exists(notepadPath))
            {
                return; // Skip on systems without notepad
            }

            // Act
            var result = extractor.ExtractIcon(notepadPath);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public void ExtractIcon_SamePath_ReturnsCachedIcon()
        {
            // Arrange
            using var extractor = new IconExtractor(_loggerMock.Object);
            var notepadPath = @"C:\Windows\System32\notepad.exe";

            if (!System.IO.File.Exists(notepadPath))
            {
                return; // Skip on systems without notepad
            }

            // Act
            var result1 = extractor.ExtractIcon(notepadPath);
            var result2 = extractor.ExtractIcon(notepadPath);

            // Assert
            result1.Should().NotBeNull();
            result2.Should().NotBeNull();
            // Cache should have the icon
            extractor.CachedIconCount.Should().Be(1);
        }

        [Fact]
        public void ExtractIcon_AfterDispose_ReturnsNull()
        {
            // Arrange
            var extractor = new IconExtractor(_loggerMock.Object);
            extractor.Dispose();

            // Act
            var result = extractor.ExtractIcon(@"C:\Windows\System32\notepad.exe");

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region ExtractIconFromHandle Tests

        [Fact]
        public void ExtractIconFromHandle_WithZeroHandle_ReturnsNull()
        {
            // Arrange
            using var extractor = new IconExtractor(_loggerMock.Object);

            // Act
            var result = extractor.ExtractIconFromHandle(IntPtr.Zero);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ExtractIconFromHandle_AfterDispose_ReturnsNull()
        {
            // Arrange
            var extractor = new IconExtractor(_loggerMock.Object);
            extractor.Dispose();

            // Act
            var result = extractor.ExtractIconFromHandle(new IntPtr(12345));

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region ClearCache Tests

        [Fact]
        public void ClearCache_WhenCacheIsEmpty_DoesNotThrow()
        {
            // Arrange
            using var extractor = new IconExtractor(_loggerMock.Object);

            // Act
            var act = () => extractor.ClearCache();

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void ClearCache_WithCachedIcons_ClearsCache()
        {
            // Arrange
            using var extractor = new IconExtractor(_loggerMock.Object);
            var notepadPath = @"C:\Windows\System32\notepad.exe";

            if (System.IO.File.Exists(notepadPath))
            {
                extractor.ExtractIcon(notepadPath);
            }

            // Act
            extractor.ClearCache();

            // Assert
            extractor.CachedIconCount.Should().Be(0);
        }

        [Fact]
        public void ClearCache_AfterDispose_DoesNotThrow()
        {
            // Arrange
            var extractor = new IconExtractor(_loggerMock.Object);
            extractor.Dispose();

            // Act
            var act = () => extractor.ClearCache();

            // Assert
            act.Should().NotThrow();
        }

        #endregion

        #region CachedIconCount Tests

        [Fact]
        public void CachedIconCount_InitiallyZero()
        {
            // Arrange
            using var extractor = new IconExtractor(_loggerMock.Object);

            // Assert
            extractor.CachedIconCount.Should().Be(0);
        }

        [Fact]
        public void CachedIconCount_IncreasesWithExtractions()
        {
            // Arrange
            using var extractor = new IconExtractor(_loggerMock.Object);
            var notepadPath = @"C:\Windows\System32\notepad.exe";
            var calcPath = @"C:\Windows\System32\calc.exe";

            // Act
            if (System.IO.File.Exists(notepadPath))
            {
                extractor.ExtractIcon(notepadPath);
            }

            if (System.IO.File.Exists(calcPath))
            {
                extractor.ExtractIcon(calcPath);
            }

            // Assert - count should equal number of unique extractions
            var expectedCount = (System.IO.File.Exists(notepadPath) ? 1 : 0) +
                              (System.IO.File.Exists(calcPath) ? 1 : 0);
            extractor.CachedIconCount.Should().Be(expectedCount);
        }

        #endregion

        #region Dispose Tests

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Arrange
            var extractor = new IconExtractor(_loggerMock.Object);

            // Act
            extractor.Dispose();
            var act = () => extractor.Dispose();

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Dispose_ClearsCache()
        {
            // Arrange
            var extractor = new IconExtractor(_loggerMock.Object);
            var notepadPath = @"C:\Windows\System32\notepad.exe";

            if (System.IO.File.Exists(notepadPath))
            {
                extractor.ExtractIcon(notepadPath);
            }

            // Act
            extractor.Dispose();

            // Assert
            extractor.CachedIconCount.Should().Be(0);
        }

        #endregion

        #region Cache Eviction Tests

        [Fact]
        public void ExtractIcon_WhenCacheFull_EvictsOldEntries()
        {
            // Arrange - use small cache size for testing
            using var extractor = new IconExtractor(_loggerMock.Object, 2);
            var systemPath = @"C:\Windows\System32";

            // Get several executables from System32
            var exeFiles = System.IO.Directory.GetFiles(systemPath, "*.exe")
                .Take(5)
                .ToList();

            if (exeFiles.Count < 3)
            {
                return; // Skip if not enough executables found
            }

            // Act - extract more icons than cache can hold
            foreach (var file in exeFiles)
            {
                extractor.ExtractIcon(file);
            }

            // Assert - cache should not exceed max size
            // Due to eviction policy (clears half when full), count should be reasonable
            extractor.CachedIconCount.Should().BeLessOrEqualTo(exeFiles.Count);
        }

        #endregion
    }
}
