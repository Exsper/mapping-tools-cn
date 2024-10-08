﻿using System.Collections.Generic;
// ReSharper disable CompareOfFloatsByEqualityOperator

namespace Mapping_Tools.Classes.HitsoundStuff {
    /// <summary>
    /// 
    /// </summary>
    public class ImportReloadingArgsComparer : IEqualityComparer<ImportReloadingArgs> {
        public bool Equals(ImportReloadingArgs x, ImportReloadingArgs y) {
            if (x.ImportType != y.ImportType)
                return false;

            switch (x.ImportType) {
                case ImportType.堆叠:
                    return x.Path == y.Path &&
                    x.X == y.X &&
                    x.Y == y.Y;
                case ImportType.音效:
                    return x.Path == y.Path;
                case ImportType.MIDI:
                    return x.Path == y.Path &&
                    x.LengthRoughness == y.LengthRoughness &&
                    x.VelocityRoughness == y.VelocityRoughness &&
                    x.Offset == y.Offset;
                case ImportType.无:
                    return true;
                default:
                    return x.Equals(y);
            }
        }

        /// <summary>Returns a hash code for the specified object.</summary>
        /// <param name="obj">The <see cref="T:System.Object" /> for which a hash code is to be returned.</param>
        /// <returns>A hash code for the specified object.</returns>
        /// <exception cref="T:System.ArgumentNullException">The type of <paramref name="obj" /> is a reference type and <paramref name="obj" /> is null.</exception>
        public int GetHashCode(ImportReloadingArgs x) {
            var hashCode = 1887348610;
            hashCode = hashCode * -1521134295 + x.ImportType.GetHashCode();
            switch (x.ImportType) {
                case ImportType.堆叠:
                    hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(x.Path);
                    hashCode = hashCode * -1521134295 + x.X.GetHashCode();
                    hashCode = hashCode * -1521134295 + x.Y.GetHashCode();
                    return hashCode;
                case ImportType.音效:
                    hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(x.Path);
                    return hashCode;
                case ImportType.MIDI:
                    hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(x.Path);
                    hashCode = hashCode * -1521134295 + x.LengthRoughness.GetHashCode();
                    hashCode = hashCode * -1521134295 + x.VelocityRoughness.GetHashCode();
                    hashCode = hashCode * -1521134295 + x.Offset.GetHashCode();
                    return hashCode;
                case ImportType.无:
                    return hashCode;
                default:
                    return x.GetHashCode();
            }
        }
    }
}
