using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class PerpendicularGenerator : RelevantObjectsGenerator {
        public override string Name => "垂线";
        public override string Tooltip => "选取一条辅助线和一个辅助点，过辅助点作辅助线的垂线。";
        public override GeneratorType GeneratorType => GeneratorType.中级;

        public PerpendicularGenerator() {
            Settings.IsActive = true;
            Settings.IsDeep = true;
            Settings.InputPredicate.Predicates.Add(new SelectionPredicate {NeedSelected = true, MinRelevancy = 0.5});
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantLine GetRelevantObjects(RelevantLine line, RelevantPoint point) {
            return new RelevantLine(new Line2(point.Child, line.Child.DirectionVector.PerpendicularLeft));
        }
    }
}
