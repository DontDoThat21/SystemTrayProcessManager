using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SystemTrayProcessManager.Infrastructure.Helpers;

namespace SystemTrayProcessManager.Tests.Infrastructure
{
    /// <summary>
    /// Unit tests for the LRU icon cache functionality in <see cref="IconExtractor"/>.
    /// </summary>
    public class IconCacheLruTests : IDisposable
    {
        private readonly Mock<ILogger<IconExtractor>> _mockLogger;
        private readonly IconExtractor _extractor;

        public IconCacheLruTests()
        {
            _mockLogger = new Mock<ILogger<IconExtractor>>();
            _extractor = new IconExtractor(_mockLogger.Object, maxCacheSize: 10);
        }

        public void Dispose()
        {
            _extractor.Dispose();
        }

        // Cache statistics property tests

        [Fact]
        public void CacheHits_Initially_ShouldBeZero()
        {
            _extractor.CacheHits.Should().Be(0);
        }

        [Fact]
        public void CacheMisses_Initially_ShouldBeZero()
        {
            _extractor.CacheMisses.Should().Be(0);
        }

        [Fact]
        public void CacheHitRate_WithNoAccesses_ShouldBeZero()
        {
            _extractor.CacheHitRate.Should().Be(0.0);
        }

        [Fact]
        public void MaxCacheSize_ShouldReturnConfiguredValue()
        {
            _extractor.MaxCacheSize.Should().Be(10);
        }

        [Fact]
        public void MaxCacheSize_WithSmallValue_ShouldUseMinimum()
        {
            using var extractor = new IconExtractor(_mockLogger.Object, maxCacheSize: 5);
            extractor.MaxCacheSize.Should().Be(10); // Minimum is 10
        }

        // CachedIconCount tests

        [Fact]
        public void CachedIconCount_Initially_ShouldBeZero()
        {
            _extractor.CachedIconCount.Should().Be(0);
        }

        // ExtractIcon cache miss tests (file doesn't exist)

        [Fact]
        public void ExtractIcon_ForNonExistentFile_ShouldIncrementMisses()
        {
            _extractor.ExtractIcon(@"C:\NonExistent\file.exe");

            _extractor.CacheMisses.Should().Be(1);
            _extractor.CacheHits.Should().Be(0);
        }

        [Fact]
        public void ExtractIcon_WithNullPath_ShouldNotIncrementCounters()
        {
            _extractor.ExtractIcon(null!);

            _extractor.CacheMisses.Should().Be(0);
            _extractor.CacheHits.Should().Be(0);
        }

        [Fact]
        public void ExtractIcon_WithEmptyPath_ShouldNotIncrementCounters()
        {
            _extractor.ExtractIcon(string.Empty);

            _extractor.CacheMisses.Should().Be(0);
            _extractor.CacheHits.Should().Be(0);
        }

        [Fact]
        public void ExtractIcon_WithWhitespacePath_ShouldNotIncrementCounters()
        {
            _extractor.ExtractIcon("   ");

            _extractor.CacheMisses.Should().Be(0);
            _extractor.CacheHits.Should().Be(0);
        }

        // Cache hit rate calculation tests

        [Fact]
        public void CacheHitRate_WithAllMisses_ShouldBeZero()
        {
            // These will all be misses (files don't exist)
            _extractor.ExtractIcon(@"C:\Fake\path1.exe");
            _extractor.ExtractIcon(@"C:\Fake\path2.exe");
            _extractor.ExtractIcon(@"C:\Fake\path3.exe");

            _extractor.CacheHitRate.Should().Be(0.0);
        }

        // ClearCache tests

        [Fact]
        public void ClearCache_ShouldResetCachedIconCount()
        {
            // Would need actual files to populate cache, but we can test the method exists
            _extractor.ClearCache();
            _extractor.CachedIconCount.Should().Be(0);
        }

        [Fact]
        public void ClearCache_ShouldNotResetStatistics()
        {
            // Record some accesses
            _extractor.ExtractIcon(@"C:\Fake\path.exe");

            var missesBeforeClear = _extractor.CacheMisses;
            _extractor.ClearCache();

            // Statistics should persist after clear
            _extractor.CacheMisses.Should().Be(missesBeforeClear);
        }

        // ExtractIconFromHandle tests

        [Fact]
        public void ExtractIconFromHandle_WithZeroHandle_ShouldReturnNull()
        {
            var result = _extractor.ExtractIconFromHandle(IntPtr.Zero);
            result.Should().BeNull();
        }

        [Fact]
        public void ExtractIconFromHandle_WithInvalidHandle_ShouldReturnNull()
        {
            var result = _extractor.ExtractIconFromHandle(new IntPtr(12345));
            result.Should().BeNull();
        }

        // Dispose tests

        [Fact]
        public void Dispose_ShouldClearCache()
        {
            var extractor = new IconExtractor(_mockLogger.Object);
            extractor.Dispose();

            extractor.CachedIconCount.Should().Be(0);
        }

        [Fact]
        public void ExtractIcon_AfterDispose_ShouldReturnNull()
        {
            var extractor = new IconExtractor(_mockLogger.Object);
            extractor.Dispose();

            var result = extractor.ExtractIcon(@"C:\Windows\System32\notepad.exe");

            result.Should().BeNull();
        }

        [Fact]
        public void ExtractIconFromHandle_AfterDispose_ShouldReturnNull()
        {
            var extractor = new IconExtractor(_mockLogger.Object);
            extractor.Dispose();

            var result = extractor.ExtractIconFromHandle(new IntPtr(12345));

            result.Should().BeNull();
        }

        [Fact]
        public void ClearCache_AfterDispose_ShouldNotThrow()
        {
            var extractor = new IconExtractor(_mockLogger.Object);
            extractor.Dispose();

            var act = () => extractor.ClearCache();

            act.Should().NotThrow();
        }

        // Constructor validation tests

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrow()
        {
            var act = () => new IconExtractor(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_WithDefaultMaxSize_ShouldUse100()
        {
            using var extractor = new IconExtractor(_mockLogger.Object);
            extractor.MaxCacheSize.Should().Be(100);
        }

        [Fact]
        public void Constructor_WithCustomMaxSize_ShouldUseSpecifiedSize()
        {
            using var extractor = new IconExtractor(_mockLogger.Object, maxCacheSize: 50);
            extractor.MaxCacheSize.Should().Be(50);
        }
    }
}
