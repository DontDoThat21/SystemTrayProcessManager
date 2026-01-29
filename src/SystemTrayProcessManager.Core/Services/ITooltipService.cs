namespace SystemTrayProcessManager.Core.Services;

/// <summary>
/// Service for managing application tooltip text.
/// Provides localized, consistent tooltip text throughout the UI.
/// </summary>
public interface ITooltipService
{
    /// <summary>
    /// Gets tooltip text for a specific key.
    /// </summary>
    /// <param name="key">The tooltip key (e.g., "ProcessCard.BringToFront").</param>
    /// <returns>The tooltip text, or empty string if not found.</returns>
    string GetTooltip(string key);

    /// <summary>
    /// Gets tooltip text with formatted parameters.
    /// </summary>
    /// <param name="key">The tooltip key.</param>
    /// <param name="args">Format arguments to insert into the template.</param>
    /// <returns>The formatted tooltip text, or empty string if not found.</returns>
    string GetTooltip(string key, params object[] args);

    /// <summary>
    /// Checks if a tooltip exists for the specified key.
    /// </summary>
    /// <param name="key">The tooltip key to check.</param>
    /// <returns>True if a tooltip exists for the key; otherwise, false.</returns>
    bool HasTooltip(string key);

    /// <summary>
    /// Gets all available tooltip keys.
    /// </summary>
    /// <returns>Collection of all registered tooltip keys.</returns>
    IReadOnlyCollection<string> GetAllKeys();
}
