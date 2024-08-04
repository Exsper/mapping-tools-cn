using System.Globalization;
using System.Text;
using System.Windows.Controls;

namespace Mapping_Tools.Components.Domain {
    internal class IsASCIIValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            string str = (value ?? "").ToString();
            return Encoding.UTF8.GetByteCount(str) == str.Length ? 
                ValidationResult.ValidResult : 
                new ValidationResult(false, "字段包含非ASCII字符。");
        }
    }

}