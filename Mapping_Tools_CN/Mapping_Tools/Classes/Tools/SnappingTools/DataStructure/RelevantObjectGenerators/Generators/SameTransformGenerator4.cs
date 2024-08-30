﻿using System;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class SameTransformGenerator4 : RelevantObjectsGenerator {
        public override string Name => "四点后继点";
        public override string Tooltip => "选取四个辅助点，按前一个点到后一个点的相同角度、角度变化、速度变化和加速度变化作后续辅助点。";
        public override GeneratorType GeneratorType => GeneratorType.高级;
        public override GeneratorTemporalPositioning TemporalPositioning => GeneratorTemporalPositioning.After;

        public SameTransformGenerator4() {
            Settings.IsSequential = true;
            Settings.IsDeep = true;
            Settings.InputPredicate.Predicates.Add(new SelectionPredicate {NeedSelected = true, MinRelevancy = 0.5});
            Settings.InputPredicate.Predicates.Add(new SelectionPredicate {NeedGeneratedByThis = true});
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantPoint GetRelevantObjects(RelevantPoint point1, RelevantPoint point2, RelevantPoint point3, RelevantPoint point4) {
            // Get the vectors between the points
            var a = point2.Child - point1.Child;
            var b = point3.Child - point2.Child;
            var c = point4.Child - point3.Child;

            // Return null if any length is zero
            if (Math.Abs(a.X) < double.Epsilon && Math.Abs(a.Y) < double.Epsilon) {
                return null;
            }
            if (Math.Abs(b.X) < double.Epsilon && Math.Abs(b.Y) < double.Epsilon) {
                return null;
            }
            if (Math.Abs(c.X) < double.Epsilon && Math.Abs(c.Y) < double.Epsilon) {
                return null;
            }

            // Calculate the next point
            var d1 = Vector2.ComplexQuotient(b, a);
            var d2 = Vector2.ComplexQuotient(c, b);
            var dd = Vector2.ComplexQuotient(d2, d1);
            Vector2 newPoint = Vector2.ComplexProduct(c, Vector2.ComplexProduct(d2, dd)) + point4.Child;

            return new RelevantPoint(newPoint);
        }
    }
}
