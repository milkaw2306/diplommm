using System.Globalization;
using System.Windows.Data;

namespace Diploma.Converters
{
    public class CollageTileSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int seed = value is int id ? id : 0;
            string dimension = parameter?.ToString() ?? "width";
            int variant = Math.Abs(seed) % 6;

            int[] widths = { 220, 280, 180, 240, 320, 200 };
            int[] heights = { 260, 190, 220, 300, 230, 180 };

            return dimension == "height" ? heights[variant] : widths[variant];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
