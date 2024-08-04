using System.Globalization;
using System.Windows.Controls;

namespace Mapping_Tools.Components.Domain {
    public class NotEmptyValidationRule : ValidationRule {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            return string.IsNullOrWhiteSpace((value ?? "").ToString())
                ? new ValidationResult(false, "字段为必填项。")
                : ValidationResult.ValidResult;
        }
    }
}