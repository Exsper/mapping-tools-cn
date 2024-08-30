using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class PointBisectorGenerator : RelevantObjectsGenerator {
        public override string Name => "两点垂直平分线";
        public override string Tooltip => "选取一对辅助点，生成垂直平分线。";
        public override GeneratorType GeneratorType => GeneratorType.中级;

        public PointBisectorGenerator() {
            Settings.IsActive = true;
            Settings.IsSequential = true;
            Settings.IsDeep = true;
            Settings.InputPredicate.Predicates.Add(new SelectionPredicate {NeedSelected = true, MinRelevancy = 0.5});
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantLine GetRelevantObjects(RelevantPoint point1, RelevantPoint point2) {
            return new RelevantLine(new Line2((point1.Child + point2.Child) / 2, (point2.Child - point1.Child).PerpendicularLeft));
        }
    }
}
