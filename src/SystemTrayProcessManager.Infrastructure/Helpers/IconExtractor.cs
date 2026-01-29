using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SystemTrayProcessManager.Core.Services;
using WpfImageSource = System.Windows.Media.ImageSource;

namespace SystemTrayProcessManager.Infrastructure.Helpers
{
    /// <summary>
    /// Extracts icons from executable files and window handles with LRU caching support.
    /// Uses Shell32 API for reliable icon extraction with access-time-based eviction.
    /// </summary>
    public sealed class IconExtractor : IIconExtractor
    {
        private readonly ILogger<IconExtractor> _logger;
        private readonly ConcurrentDictionary<string, IconCacheEntry> _iconCache;
        private readonly int _maxCacheSize;
        private readonly object _evictionLock = new();
        private long _cacheHits;
        private long _cacheMisses;
        private bool _disposed;

        /// <inheritdoc/>
        public int CachedIconCount => _iconCache.Count;

        /// <inheritdoc/>
        public int MaxCacheSize => _maxCacheSize;

        /// <inheritdoc/>
        public long CacheHits => Interlocked.Read(ref _cacheHits);

        /// <inheritdoc/>
        public long CacheMisses => Interlocked.Read(ref _cacheMisses);

        /// <inheritdoc/>
        public double CacheHitRate
        {
            get
            {
                var hits = CacheHits;
                var misses = CacheMisses;
                var total = hits + misses;
                return total > 0 ? (double)hits / total : 0.0;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IconExtractor"/> class.
        /// </summary>
        /// <param name="logger">Logger instance for diagnostic output.</param>
        /// <param name="maxCacheSize">Maximum number of icons to cache. Defaults to 100.</param>
        /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
        public IconExtractor(ILogger<IconExtractor> logger, int maxCacheSize = 100)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _maxCacheSize = Math.Max(10, maxCacheSize); // Minimum cache size of 10
            _iconCache = new ConcurrentDictionary<string, IconCacheEntry>(StringComparer.OrdinalIgnoreCase);
        }

        /// <inheritdoc/>
        public WpfImageSource? ExtractIcon(string executablePath)
        {
            if (_disposed)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(executablePath))
            {
                _logger.LogDebug("Cannot extract icon from null or empty path");
                return null;
            }

            try
            {
                // Check cache first with LRU access tracking
                if (_iconCache.TryGetValue(executablePath, out var cacheEntry))
                {
                    cacheEntry.RecordAccess();
                    Interlocked.Increment(ref _cacheHits);
                    _logger.LogDebug("Cache hit for icon: {Path} (accesses: {Count})", 
                        executablePath, cacheEntry.AccessCount);
                    return cacheEntry.Icon;
                }

                // Cache miss
                Interlocked.Increment(ref _cacheMisses);

                // Verify file exists
                if (!File.Exists(executablePath))
                {
                    _logger.LogDebug("Executable not found: {Path}", executablePath);
                    return null;
                }

                // Extract icon using Shell32
                var icon = ExtractIconFromFile(executablePath);
                if (icon == null)
                {
                    _logger.LogDebug("Failed to extract icon from: {Path}", executablePath);
                    return null;
                }

                // Convert to WPF ImageSource
                var imageSource = ConvertIconToImageSource(icon);
                icon.Dispose();

                if (imageSource == null)
                {
                    return null;
                }

                // Add to cache with LRU eviction if needed
                AddToCacheWithLruEviction(executablePath, imageSource);

                _logger.LogDebug("Extracted and cached icon for: {Path}", executablePath);
                return imageSource;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error extracting icon from: {Path}", executablePath);
                return null;
            }
        }

        /// <inheritdoc/>
        public WpfImageSource? ExtractIconFromHandle(IntPtr windowHandle)
        {
            if (_disposed)
            {
                return null;
            }

            if (windowHandle == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                // Try to get the icon from the window
                var hIcon = SendMessage(windowHandle, WM_GETICON, ICON_SMALL, IntPtr.Zero);

                if (hIcon == IntPtr.Zero)
                {
                    hIcon = SendMessage(windowHandle, WM_GETICON, ICON_BIG, IntPtr.Zero);
                }

                if (hIcon == IntPtr.Zero)
                {
                    hIcon = GetClassLongPtr(windowHandle, GCL_HICON);
                }

                if (hIcon == IntPtr.Zero)
                {
                    hIcon = GetClassLongPtr(windowHandle, GCL_HICONSM);
                }

                if (hIcon == IntPtr.Zero)
                {
                    return null;
                }

                // Convert to Icon and then to ImageSource
                using var icon = Icon.FromHandle(hIcon);
                return ConvertIconToImageSource(icon);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error extracting icon from window handle");
                return null;
            }
        }


        /// <inheritdoc/>
        public void ClearCache()
        {
            if (_disposed)
            {
                return;
            }

            _iconCache.Clear();
            _logger.LogDebug("Icon cache cleared");
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _iconCache.Clear();
            _disposed = true;

            _logger.LogDebug("IconExtractor disposed");
        }

        /// <summary>
        /// Extracts icon from file using Shell32 API.
        /// </summary>
        private static Icon? ExtractIconFromFile(string filePath)
        {
            try
            {
                var hIcon = IntPtr.Zero;

                // Try ExtractIconEx first for better quality
                var iconCount = ExtractIconEx(filePath, 0, out var largeIcon, out var smallIcon, 1);

                if (iconCount > 0 && largeIcon != IntPtr.Zero)
                {
                    hIcon = largeIcon;
                    if (smallIcon != IntPtr.Zero)
                    {
                        DestroyIcon(smallIcon);
                    }
                }
                else if (smallIcon != IntPtr.Zero)
                {
                    hIcon = smallIcon;
                    if (largeIcon != IntPtr.Zero)
                    {
                        DestroyIcon(largeIcon);
                    }
                }
                else
                {
                    // Fallback to ExtractAssociatedIcon
                    using var ico = Icon.ExtractAssociatedIcon(filePath);
                    return ico != null ? (Icon)ico.Clone() : null;
                }

                if (hIcon != IntPtr.Zero)
                {
                    var icon = (Icon)Icon.FromHandle(hIcon).Clone();
                    DestroyIcon(hIcon);
                    return icon;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Converts a GDI+ Icon to a WPF ImageSource.
        /// </summary>
        private WpfImageSource? ConvertIconToImageSource(Icon icon)
        {
            try
            {
                using var bitmap = icon.ToBitmap();
                var hBitmap = bitmap.GetHbitmap();

                try
                {
                    var imageSource = Imaging.CreateBitmapSourceFromHBitmap(
                        hBitmap,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());

                    // Freeze for thread-safety
                    imageSource.Freeze();
                    return imageSource;
                }
                finally
                {
                    DeleteObject(hBitmap);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error converting icon to ImageSource");
                return null;
            }
        }

        /// <summary>
        /// Adds an icon to the cache with LRU eviction when capacity is reached.
        /// Evicts the oldest 25% of entries based on last access time.
        /// </summary>
        private void AddToCacheWithLruEviction(string key, WpfImageSource imageSource)
        {
            // Check if eviction is needed
            if (_iconCache.Count >= _maxCacheSize)
            {
                lock (_evictionLock)
                {
                    // Double-check after acquiring lock
                    if (_iconCache.Count >= _maxCacheSize)
                    {
                        PerformLruEviction();
                    }
                }
            }

            var entry = new IconCacheEntry(imageSource);
            _iconCache.TryAdd(key, entry);
        }

        /// <summary>
        /// Evicts the oldest 25% of cache entries based on last access time.
        /// </summary>
        private void PerformLruEviction()
        {
            try
            {
                var evictionCount = Math.Max(1, _maxCacheSize / 4); // Evict 25%

                // Get entries sorted by last access time (oldest first)
                var entriesToEvict = _iconCache
                    .OrderBy(kvp => kvp.Value.LastAccessTime)
                    .Take(evictionCount)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in entriesToEvict)
                {
                    _iconCache.TryRemove(key, out _);
                }

                _logger.LogDebug(
                    "LRU eviction: removed {EvictedCount} oldest entries from icon cache (remaining: {Remaining})",
                    entriesToEvict.Count,
                    _iconCache.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during LRU cache eviction");
            }
        }

        #region P/Invoke Declarations

        private const int WM_GETICON = 0x007F;
        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;
        private const int GCL_HICON = -14;
        private const int GCL_HICONSM = -34;

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int ExtractIconEx(
            string lpszFile,
            int nIconIndex,
            out IntPtr phiconLarge,
            out IntPtr phiconSmall,
            int nIcons);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "GetClassLongPtr")]
        private static extern IntPtr GetClassLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetClassLong")]
        private static extern IntPtr GetClassLong32(IntPtr hWnd, int nIndex);

        private static IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex)
        {
            return IntPtr.Size == 8 ? GetClassLongPtr64(hWnd, nIndex) : GetClassLong32(hWnd, nIndex);
        }

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        #endregion
    }

    /// <summary>
    /// Represents a cached icon with access tracking for LRU eviction.
    /// </summary>
    internal sealed class IconCacheEntry
    {
        private DateTime _lastAccessTime;
        private int _accessCount;

        /// <summary>
        /// Gets the cached icon.
        /// </summary>
        public WpfImageSource Icon { get; }

        /// <summary>
        /// Gets the time this entry was created.
        /// </summary>
        public DateTime CreatedTime { get; }

        /// <summary>
        /// Gets the time this entry was last accessed.
        /// </summary>
        public DateTime LastAccessTime => _lastAccessTime;

        /// <summary>
        /// Gets the number of times this entry has been accessed.
        /// </summary>
        public int AccessCount => _accessCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="IconCacheEntry"/> class.
        /// </summary>
        /// <param name="icon">The icon to cache.</param>
        /// <exception cref="ArgumentNullException">Thrown when icon is null.</exception>
        public IconCacheEntry(WpfImageSource icon)
        {
            Icon = icon ?? throw new ArgumentNullException(nameof(icon));
            CreatedTime = DateTime.UtcNow;
            _lastAccessTime = CreatedTime;
            _accessCount = 1;
        }

        /// <summary>
        /// Records an access to this cache entry, updating the last access time.
        /// </summary>
        public void RecordAccess()
        {
            _lastAccessTime = DateTime.UtcNow;
            Interlocked.Increment(ref _accessCount);
        }
    }
}
