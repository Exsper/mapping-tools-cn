﻿using System;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class TriangleGenerator2 : RelevantObjectsGenerator {
        public override string Name => "两点作等边三角形（对角线）";
        public override string Tooltip => "选取两个辅助点作为60°内角菱形的对角线，生成其余点补全两个等边三角形。";
        public override GeneratorType GeneratorType => GeneratorType.中级;

        public TriangleGenerator2() {
            Settings.IsActive = true;
            Settings.IsDeep = true;
            Settings.InputPredicate.Predicates.Add(new SelectionPredicate {NeedSelected = true, MinRelevancy = 0.5});
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantPoint[] GetRelevantObjects(RelevantPoint point1, RelevantPoint point2) {
            var diff = point2.Child - point1.Child;
            var rotated = Vector2.Rotate(diff, Math.PI * 5 / 6) / Math.Sqrt(3);
            return new[] {
                new RelevantPoint(point1.Child - rotated),
                new RelevantPoint(point2.Child + rotated)
            };
        }
    }
}
