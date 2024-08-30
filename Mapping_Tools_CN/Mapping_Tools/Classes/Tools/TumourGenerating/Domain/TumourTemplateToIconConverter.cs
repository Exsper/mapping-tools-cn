using System;
using System.Globalization;
using System.Windows.Data;
using Mapping_Tools.Classes.Tools.TumourGenerating.Enums;
using MaterialDesignThemes.Wpf;

namespace Mapping_Tools.Classes.Tools.TumourGenerating.Domain {
    public class TumourTemplateToIconConverter : IValueConverter {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                TumourTemplate.三角形 => PackIconKind.TriangleOutline,
                TumourTemplate.方形 => PackIconKind.SquareOutline,
                TumourTemplate.圆形 => PackIconKind.CircleOutline,
                TumourTemplate.抛物线 => PackIconKind.Multiply,
                _ => PackIconKind.TriangleOutline,
            };
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                PackIconKind.TriangleOutline => TumourTemplate.三角形,
                PackIconKind.SquareOutline => TumourTemplate.方形,
                PackIconKind.CircleOutline => TumourTemplate.圆形,
                PackIconKind.Multiply => TumourTemplate.抛物线,
                _ => TumourTemplate.三角形,
            };
        }
    }
}