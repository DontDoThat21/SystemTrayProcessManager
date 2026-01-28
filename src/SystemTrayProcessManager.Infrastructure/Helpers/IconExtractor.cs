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
    /// Extracts icons from executable files and window handles with caching support.
    /// Uses Shell32 API for reliable icon extraction.
    /// </summary>
    public sealed class IconExtractor : IIconExtractor
    {
        private readonly ILogger<IconExtractor> _logger;
        private readonly ConcurrentDictionary<string, WpfImageSource> _iconCache;
        private readonly int _maxCacheSize;
        private bool _disposed;

        /// <inheritdoc/>
        public int CachedIconCount => _iconCache.Count;

        /// <summary>
        /// Initializes a new instance of the <see cref="IconExtractor"/> class.
        /// </summary>
        /// <param name="logger">Logger instance for diagnostic output.</param>
        /// <param name="maxCacheSize">Maximum number of icons to cache. Defaults to 100.</param>
        /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
        public IconExtractor(ILogger<IconExtractor> logger, int maxCacheSize = 100)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _maxCacheSize = maxCacheSize;
            _iconCache = new ConcurrentDictionary<string, WpfImageSource>(StringComparer.OrdinalIgnoreCase);
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
                // Check cache first
                if (_iconCache.TryGetValue(executablePath, out var cachedIcon))
                {
                    _logger.LogDebug("Cache hit for icon: {Path}", executablePath);
                    return cachedIcon;
                }

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

                // Add to cache (with eviction if needed)
                AddToCache(executablePath, imageSource);

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
        /// Adds an icon to the cache, evicting oldest entries if necessary.
        /// </summary>
        private void AddToCache(string key, WpfImageSource imageSource)
        {
            // Simple eviction: if cache is full, clear half of it
            // A more sophisticated LRU could be implemented if needed
            if (_iconCache.Count >= _maxCacheSize)
            {
                var keysToRemove = _iconCache.Keys.Take(_maxCacheSize / 2).ToList();
                foreach (var keyToRemove in keysToRemove)
                {
                    _iconCache.TryRemove(keyToRemove, out _);
                }

                _logger.LogDebug("Evicted {Count} icons from cache", keysToRemove.Count);
            }

            _iconCache.TryAdd(key, imageSource);
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
}
