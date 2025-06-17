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
            Debug.WriteLine($"[Convert] Input value: '{value}', Type: {value?.GetType()}");
            if (value == null) {
                Debug.WriteLine("[Convert] Value is null, returning 'auto'");
                return "auto";
            }
            
            if (value is int intValue) {
                var result = intValue == -1 ? "auto" : intValue.ToString();
                Debug.WriteLine($"[Convert] Integer value: {intValue}, returning: '{result}'");
                return result;
            }
            
            if (value is double doubleValue) {
                var result = doubleValue == -1 ? "auto" : doubleValue.ToString();
                Debug.WriteLine($"[Convert] Double value: {doubleValue}, returning: '{result}'");
                return result;
            }
            
            if (value is string strValue) {
                Debug.WriteLine($"[Convert] String value: '{strValue}', returning as is");
                return strValue;
            }
            
            Debug.WriteLine($"[Convert] Unknown type, returning 'auto'");
            return "auto";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            Debug.WriteLine($"[ConvertBack] Input value: '{value}', Type: {value?.GetType()}");
            if (value == null) {
                Debug.WriteLine("[ConvertBack] Value is null, returning -1");
                return -1;
            }
            
            if (value is string strValue) {
                if (string.IsNullOrWhiteSpace(strValue)) {
                    Debug.WriteLine("[ConvertBack] Empty string, returning -1");
                    return -1;
                }
                
                if (strValue.ToLower() == "auto") {
                    Debug.WriteLine("[ConvertBack] Value is 'auto', returning -1");
                    return -1;
                }
                
                if (double.TryParse(strValue, out double result)) {
                    Debug.WriteLine($"[ConvertBack] Successfully parsed number: {result}");
                    return result;
                }
                
                Debug.WriteLine("[ConvertBack] Failed to parse number, returning -1");
                return -1;
            }
            
            Debug.WriteLine("[ConvertBack] Unknown type, returning -1");
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

    public class ImageResolutionConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            Debug.WriteLine($"[ImageResolutionConverter] Input value: '{value}', Type: {value?.GetType()}");
            if (value == null) {
                Debug.WriteLine("[ImageResolutionConverter] Value is null, returning 'auto'");
                return "auto";
            }
            
            if (value is int intValue) {
                var result = intValue == -1 ? "auto" : intValue.ToString();
                Debug.WriteLine($"[ImageResolutionConverter] Integer value: {intValue}, returning: '{result}'");
                return result;
            }
            
            if (value is double doubleValue) {
                var result = doubleValue == -1 ? "auto" : doubleValue.ToString();
                Debug.WriteLine($"[ImageResolutionConverter] Double value: {doubleValue}, returning: '{result}'");
                return result;
            }
            
            if (value is string strValue) {
                Debug.WriteLine($"[ImageResolutionConverter] String value: '{strValue}', returning as is");
                return strValue;
            }
            
            Debug.WriteLine($"[ImageResolutionConverter] Unknown type, returning 'auto'");
            return "auto";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            Debug.WriteLine($"[ImageResolutionConverter] Input value: '{value}', Type: {value?.GetType()}");
            if (value == null) {
                Debug.WriteLine("[ImageResolutionConverter] Value is null, returning -1");
                return -1;
            }
            
            if (value is string strValue) {
                if (string.IsNullOrWhiteSpace(strValue)) {
                    Debug.WriteLine("[ImageResolutionConverter] Empty string, returning -1");
                    return -1;
                }
                
                if (strValue.ToLower() == "auto") {
                    Debug.WriteLine("[ImageResolutionConverter] Value is 'auto', returning -1");
                    return -1;
                }
                
                if (int.TryParse(strValue, out int result)) {
                    Debug.WriteLine($"[ImageResolutionConverter] Successfully parsed number: {result}");
                    return result;
                }
                
                Debug.WriteLine("[ImageResolutionConverter] Failed to parse number, returning -1");
                return -1;
            }
            
            Debug.WriteLine("[ImageResolutionConverter] Unknown type, returning -1");
            return -1;
        }
    }
}
