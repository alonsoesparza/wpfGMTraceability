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
                return "CloseCircle";
            else if (Type == "Warning")
                return "Alert";
            else if (Type == "Info")
                return "Info";
            else if (Type == "SystemError")
                return "AlertBox";
            else
                return "CheckCircleOutline";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
