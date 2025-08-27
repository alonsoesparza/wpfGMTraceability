using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace wpfGMTraceability.Helpers
{
    public class TypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var Type = value as string;
            if (Type == "Error")
                return Brushes.Red;
            else if (Type == "Warning")
                return Brushes.Orange;
            else if (Type == "Info")
                return Brushes.Green;
            else
                return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
