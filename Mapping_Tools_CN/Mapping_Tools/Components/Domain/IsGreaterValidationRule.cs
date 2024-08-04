using Mapping_Tools.Classes.SystemTools;
using System.Globalization;
using System.Windows.Controls;

namespace Mapping_Tools.Components.Domain {
    internal class IsGreaterValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            double limit = ValueWrapper.Value;
            string str = (value ?? "").ToString();
            if (!TypeConverters.TryParseDouble(str, out double result)) {
                return new ValidationResult(false, "Double格式错误。");
            }
            return result > limit ? ValidationResult.ValidResult : new ValidationResult(false, $"数值必须大于 {limit}.");
        }

        public DoubleWrapper ValueWrapper { get; set; }
    }
}