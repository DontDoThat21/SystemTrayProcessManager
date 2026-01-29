using WpfImageSource = System.Windows.Media.ImageSource;

namespace SystemTrayProcessManager.Core.Services
{
    /// <summary>
    /// Provides functionality for extracting icons from executable files and window handles.
    /// Implements LRU caching to improve performance for repeated icon requests.
    /// </summary>
    public interface IIconExtractor : IDisposable
    {
        /// <summary>
        /// Extracts the icon from the specified executable file.
        /// Returns a cached icon if previously extracted.
        /// </summary>
        /// <param name="executablePath">The full path to the executable file.</param>
        /// <returns>The extracted icon as a WPF ImageSource, or null if extraction fails.</returns>
        WpfImageSource? ExtractIcon(string executablePath);

        /// <summary>
        /// Extracts the icon from a window using its handle.
        /// </summary>
        /// <param name="windowHandle">The handle to the window.</param>
        /// <returns>The extracted icon as a WPF ImageSource, or null if extraction fails.</returns>
        WpfImageSource? ExtractIconFromHandle(IntPtr windowHandle);

        /// <summary>
        /// Clears all cached icons and releases associated resources.
        /// </summary>
        void ClearCache();

        /// <summary>
        /// Gets the current number of icons in the cache.
        /// </summary>
        int CachedIconCount { get; }

        /// <summary>
        /// Gets the maximum cache capacity.
        /// </summary>
        int MaxCacheSize { get; }

        /// <summary>
        /// Gets the total number of cache hits.
        /// </summary>
        long CacheHits { get; }

        /// <summary>
        /// Gets the total number of cache misses.
        /// </summary>
        long CacheMisses { get; }

        /// <summary>
        /// Gets the cache hit rate (0.0 to 1.0).
        /// Returns 0.0 if no cache accesses have been made.
        /// </summary>
        double CacheHitRate { get; }
    }
}
