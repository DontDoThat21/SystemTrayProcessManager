using System.Windows;
using SystemTrayProcessManager.Core.Services;

namespace SystemTrayProcessManager.UI.Helpers;

/// <summary>
/// Provides attached properties for binding tooltip text via ITooltipService.
/// Enables XAML-based tooltip assignment using service-managed strings.
/// </summary>
/// <example>
/// Usage in XAML:
/// <code>
/// &lt;Button helpers:TooltipHelper.TooltipKey="ProcessCard.BringToFront" /&gt;
/// </code>
/// </example>
public static class TooltipHelper
{
    private static ITooltipService? _tooltipService;
    private static bool _isInitialized;

    /// <summary>
    /// Initializes the TooltipHelper with the tooltip service.
    /// Must be called once during application startup.
    /// </summary>
    /// <param name="tooltipService">The tooltip service to use for retrieving tooltip text.</param>
    public static void Initialize(ITooltipService tooltipService)
    {
        ArgumentNullException.ThrowIfNull(tooltipService);
        _tooltipService = tooltipService;
        _isInitialized = true;
    }

    /// <summary>
    /// Identifies the TooltipKey attached property.
    /// </summary>
    public static readonly DependencyProperty TooltipKeyProperty =
        DependencyProperty.RegisterAttached(
            "TooltipKey",
            typeof(string),
            typeof(TooltipHelper),
            new PropertyMetadata(null, OnTooltipKeyChanged));

    /// <summary>
    /// Gets the TooltipKey attached property value.
    /// </summary>
    /// <param name="obj">The dependency object to get the value from.</param>
    /// <returns>The tooltip key.</returns>
    public static string GetTooltipKey(DependencyObject obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        return (string)obj.GetValue(TooltipKeyProperty);
    }

    /// <summary>
    /// Sets the TooltipKey attached property value.
    /// </summary>
    /// <param name="obj">The dependency object to set the value on.</param>
    /// <param name="value">The tooltip key to set.</param>
    public static void SetTooltipKey(DependencyObject obj, string value)
    {
        ArgumentNullException.ThrowIfNull(obj);
        obj.SetValue(TooltipKeyProperty, value);
    }

    /// <summary>
    /// Handles changes to the TooltipKey property.
    /// </summary>
    private static void OnTooltipKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
        {
            return;
        }

        if (e.NewValue is not string key || string.IsNullOrWhiteSpace(key))
        {
            element.ToolTip = null;
            return;
        }

        if (!_isInitialized || _tooltipService == null)
        {
            // Store the key and apply later when initialized
            // This handles design-time scenarios
            element.ToolTip = $"[{key}]";
            return;
        }

        var tooltip = _tooltipService.GetTooltip(key);
        if (!string.IsNullOrEmpty(tooltip))
        {
            element.ToolTip = tooltip;
        }
        else
        {
            element.ToolTip = null;
        }
    }

    /// <summary>
    /// Refreshes all tooltips on the given element and its visual children.
    /// Call this after initializing the service if tooltips were set before initialization.
    /// </summary>
    /// <param name="root">The root element to refresh tooltips for.</param>
    public static void RefreshTooltips(DependencyObject root)
    {
        if (!_isInitialized || _tooltipService == null || root == null)
        {
            return;
        }

        RefreshTooltipsRecursive(root);
    }

    private static void RefreshTooltipsRecursive(DependencyObject obj)
    {
        var key = GetTooltipKey(obj);
        if (!string.IsNullOrWhiteSpace(key) && obj is FrameworkElement element)
        {
            var tooltip = _tooltipService!.GetTooltip(key);
            if (!string.IsNullOrEmpty(tooltip))
            {
                element.ToolTip = tooltip;
            }
        }

        int childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(obj);
        for (int i = 0; i < childCount; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(obj, i);
            RefreshTooltipsRecursive(child);
        }
    }

    /// <summary>
    /// Gets whether the tooltip helper has been initialized.
    /// </summary>
    public static bool IsInitialized => _isInitialized;
}
