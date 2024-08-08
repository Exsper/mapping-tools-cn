using System;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class SquareGenerator : RelevantObjectsGenerator {
        public override string Name => "两点作正方形（对角线）";
        public override string Tooltip => "选取两个辅助点作为正方形对角线，生成正方形的其他两个点。";
        public override GeneratorType GeneratorType => GeneratorType.Intermediate;

        public SquareGenerator() {
            Settings.IsActive = true;
            Settings.IsDeep = true;
            Settings.InputPredicate.Predicates.Add(new SelectionPredicate {NeedSelected = true, MinRelevancy = 0.5});
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantPoint[] GetRelevantObjects(RelevantPoint point1, RelevantPoint point2) {
            var diff = point2.Child - point1.Child;
            var rotated = Vector2.Rotate(diff, Math.PI * 3 / 4) / Math.Sqrt(2);

            return new[] {
                new RelevantPoint(point1.Child - rotated),
                new RelevantPoint(point2.Child + rotated)
            };
        }
    }
}
