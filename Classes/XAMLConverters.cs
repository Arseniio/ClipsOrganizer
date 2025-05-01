using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace ClipsOrganizer.Converter {
    public class ExportAutoConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            Debug.WriteLine($"Convert to: {value}");
            if (value is int intValue) {
                return intValue == -1 ? "auto" : intValue.ToString();
            }
            return "auto"; 
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            Debug.WriteLine($"Convert back: {value}");
            if (value is string strValue) {
                return strValue == "auto" ? -1 : int.TryParse(strValue, out int result) ? result : -1;
            }
            return -1;
        }
    }

    public class BoolToVisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            return (value is bool && (bool)value) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
