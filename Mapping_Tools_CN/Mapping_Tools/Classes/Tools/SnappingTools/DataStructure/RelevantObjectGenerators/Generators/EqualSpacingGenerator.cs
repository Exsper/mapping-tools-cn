using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class EqualSpacingGenerator : RelevantObjectsGenerator {
        public override string Name => "两点作相交圆";
        public override string Tooltip => "选取两个辅助点，分别作以自己为中心，到对方的距离为半径的圆。";
        public override GeneratorType GeneratorType => GeneratorType.Intermediate;

        public EqualSpacingGenerator() {
            Settings.IsActive = true;
            Settings.IsSequential = true;
            Settings.IsDeep = true;
            Settings.InputPredicate.Predicates.Add(new SelectionPredicate {NeedSelected = true, MinRelevancy = 0.5});
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantCircle[] GetRelevantObjects(RelevantPoint point1, RelevantPoint point2) {
            var radius = (point1.Child - point2.Child).Length;
            return new[] {
                new RelevantCircle(new Circle(point1.Child, radius)),
                new RelevantCircle(new Circle(point2.Child, radius))
            };
        }
    }
}
