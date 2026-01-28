using System.Globalization;
using System.Windows.Data;

namespace SystemTrayProcessManager.UI.Converters
{
    /// <summary>
    /// Converts a boolean mute state to a mute/unmute icon character.
    /// True (muted) returns a muted speaker icon; false (unmuted) returns a speaker icon.
    /// </summary>
    [ValueConversion(typeof(bool), typeof(string))]
    public class BoolToMuteIconConverter : IValueConverter
    {
        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isMuted)
            {
                return isMuted ? "\uD83D\uDD07" : "\uD83D\uDD0A"; // muted speaker : speaker
            }
            return "\uD83D\uDD0A";
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("BoolToMuteIconConverter does not support ConvertBack.");
        }
    }
}
