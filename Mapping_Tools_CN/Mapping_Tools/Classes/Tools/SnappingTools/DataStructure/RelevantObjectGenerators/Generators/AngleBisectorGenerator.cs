﻿using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class AngleBisectorGenerator : RelevantObjectsGenerator {
        public override string Name => "角平分线";
        public override string Tooltip => "选取一对辅助线，在交点处生成角平分线。";
        public override GeneratorType GeneratorType => GeneratorType.中级;

        public AngleBisectorGenerator() {
            Settings.IsActive = true;
            Settings.IsDeep = true;
            Settings.InputPredicate.Predicates.Add(new SelectionPredicate {NeedSelected = true, MinRelevancy = 0.8});
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantLine[] GetRelevantObjects(RelevantLine line1, RelevantLine line2) {
            if (!Line2.Intersection(line1.Child, line2.Child, out var intersection)) return null;
            var dir1Norm = Vector2.Normalize(line1.Child.DirectionVector);
            var dir2Norm = Vector2.Normalize(line2.Child.DirectionVector);
            return new[] {
                new RelevantLine(new Line2(intersection, dir1Norm + dir2Norm)), 
                new RelevantLine(new Line2(intersection, dir1Norm - dir2Norm))
            };
        }
    }
}
