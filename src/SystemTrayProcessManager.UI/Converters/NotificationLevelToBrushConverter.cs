using System.Globalization;
using System.Windows.Data;
using SystemTrayProcessManager.Core.Models;
using WpfBrush = System.Windows.Media.Brush;
using WpfColor = System.Windows.Media.Color;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace SystemTrayProcessManager.UI.Converters
{
    /// <summary>
    /// Converts a <see cref="NotificationLevel"/> to a corresponding brush for visual indication.
    /// </summary>
    [ValueConversion(typeof(NotificationLevel), typeof(WpfBrush))]
    public class NotificationLevelToBrushConverter : IValueConverter
    {
        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is NotificationLevel level)
            {
                return level switch
                {
                    NotificationLevel.Info => new WpfSolidColorBrush(WpfColor.FromRgb(0x00, 0x78, 0xD4)),
                    NotificationLevel.Success => new WpfSolidColorBrush(WpfColor.FromRgb(0x10, 0x7C, 0x10)),
                    NotificationLevel.Warning => new WpfSolidColorBrush(WpfColor.FromRgb(0xFF, 0xB9, 0x00)),
                    NotificationLevel.Error => new WpfSolidColorBrush(WpfColor.FromRgb(0xE8, 0x11, 0x23)),
                    _ => new WpfSolidColorBrush(WpfColor.FromRgb(0x00, 0x78, 0xD4))
                };
            }
            return new WpfSolidColorBrush(WpfColor.FromRgb(0x00, 0x78, 0xD4));
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("NotificationLevelToBrushConverter does not support ConvertBack.");
        }
    }
}
