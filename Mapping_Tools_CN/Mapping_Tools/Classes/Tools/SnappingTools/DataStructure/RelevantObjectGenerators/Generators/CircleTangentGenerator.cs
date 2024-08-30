using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;
using System;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class CircleTangentGenerator : RelevantObjectsGenerator {
        public override string Name => "过点作圆切线";
        public override string Tooltip => "选取一个辅助圆和一个辅助点，从辅助点作圆的切线。";
        public override GeneratorType GeneratorType => GeneratorType.中级;

        public CircleTangentGenerator() {
            Settings.IsActive = true;
            Settings.IsSequential = false;
            Settings.IsDeep = true;
            Settings.InputPredicate.Predicates.Add(new SelectionPredicate {NeedSelected = true, MinRelevancy = 0.8});
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantLine[] GetRelevantObjects(RelevantPoint point, RelevantCircle circle) {
            var c = circle.Child.Centre;
            var d = Vector2.Distance(point.Child, c);
            var r = circle.Child.Radius;

            if (d - r < 0.5) {
                // Degenerate to single tangent line
                return new[] { new RelevantLine(new Line2(point.Child, (point.Child - c).PerpendicularLeft)) };
            }

            var b = r / (d * Math.Sqrt(1 - r * r / (d * d)));
            var v = (point.Child - c).PerpendicularLeft * b;

            return new[] {
                new RelevantLine(Line2.FromPoints(point.Child, c + v)),
                new RelevantLine(Line2.FromPoints(point.Child, c - v))
            };
        }
    }
}
