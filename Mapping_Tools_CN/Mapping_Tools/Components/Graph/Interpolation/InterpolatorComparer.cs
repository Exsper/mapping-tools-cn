using System;
using System.Collections.Generic;

namespace Mapping_Tools.Components.Graph.Interpolation {
    public class InterpolatorComparer : IComparer<string> {
        public static string[] InterpolatorOrder = {"单弯曲线", "单弯曲线 2", "单弯曲线 3", 
            "双弯曲线", "双弯曲线 2", "双弯曲线 3", "半正弦曲线", "波浪线", "抛物线", "直线"};

        public int Compare(string x, string y) {
            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (x == null && y == null) {
                return 0;
            }

            if (x == null) {
                return -1;
            }

            if (y == null) {
                return 1;
            }

            var indexX = IndexOf(x);
            var indexY = IndexOf(y);

            if (indexX == -1 && indexY == -1) {
                return string.Compare(x, y, StringComparison.Ordinal);
            }

            return indexX.CompareTo(indexY);
        }

        private int IndexOf(string name) {
            for (int i = 0; i < InterpolatorOrder.Length; i++) {
                if (InterpolatorOrder[i] == name) {
                    return i;
                }
            }

            return -1;
        }
    }
}