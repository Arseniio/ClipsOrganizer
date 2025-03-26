using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ClipsOrganizer.Converter {
    public class ExportAutoConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is int intValue) {
                return intValue == -1 ? "auto" : intValue.ToString();
            }
            return "auto"; 
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is string strValue) {
                return strValue == "auto" ? -1 : int.TryParse(strValue, out int result) ? result : -1;
            }
            return -1;
        }
    }
}
