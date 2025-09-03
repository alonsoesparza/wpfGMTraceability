using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace wpfGMTraceability.Helpers
{
    public class TypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //
            //
            //
            var Type = value as string;
            if (Type == "Error")
                return "CloseCircleOutline";
            else if (Type == "Warning")
                return "Alert";
            else if (Type == "Info")
                return "information";
            else
                return "CheckCircle";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
