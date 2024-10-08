﻿using System;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class SameTransformGenerator2 : RelevantObjectsGenerator {
        public override string Name => "两点后继点";
        public override string Tooltip => "选取两个辅助点，按前一个点到后一个点的速度作后续辅助点。";
        public override GeneratorType GeneratorType => GeneratorType.高级;
        public override GeneratorTemporalPositioning TemporalPositioning => GeneratorTemporalPositioning.After;

        public SameTransformGenerator2() {
            Settings.IsSequential = true;
            Settings.IsDeep = true;
            Settings.InputPredicate.Predicates.Add(new SelectionPredicate {NeedSelected = true, MinRelevancy = 0.5});
            Settings.InputPredicate.Predicates.Add(new SelectionPredicate {NeedGeneratedByThis = true});
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantPoint GetRelevantObjects(RelevantPoint point1, RelevantPoint point2) {
            // Get the vectors between the points
            var a = point2.Child - point1.Child;

            // Return null if length of a is zero
            if (Math.Abs(a.X) < double.Epsilon && Math.Abs(a.Y) < double.Epsilon) {
                return null;
            }

            return new RelevantPoint(point2.Child + a);
        }
    }
}
