using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using Mapping_Tools.Classes.SystemTools;

namespace Mapping_Tools.Components.Domain {
    internal class DoubleToStringConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value != null) {
                if ((double) value == 727) {
                    return "727 WYSI";
                }
                return ((double) value).ToString(CultureInfo.InvariantCulture);
            }
            return parameter != null ? parameter.ToString() : "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null) {
                if (parameter != null) {
                    return double.Parse(parameter.ToString());
                }
                return new ValidationResult(false, "不能逆转换null。");
            }

            if (value.ToString() == "727 WYSI") {
                return 727;
            }

            if (parameter == null) {
                if (TypeConverters.TryParseDouble(value.ToString(), out double result1)) {
                    return result1;
                }

                return new ValidationResult(false, "Double格式错误。");
            }
            TypeConverters.TryParseDouble(value.ToString(), out double result2, double.Parse(parameter.ToString()));
            return result2;
        }
    }
}
