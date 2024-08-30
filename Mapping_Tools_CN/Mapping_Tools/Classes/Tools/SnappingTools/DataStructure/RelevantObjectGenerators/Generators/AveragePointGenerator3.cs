using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class AveragePointGenerator3 : RelevantObjectsGenerator {
        public override string Name => "三点中心点";
        public override string Tooltip => "选取三个辅助点，生成中心点。";
        public override GeneratorType GeneratorType => GeneratorType.中级;

        public AveragePointGenerator3() {
            Settings.IsActive = true;
            Settings.IsSequential = true;
            Settings.IsDeep = true;
            Settings.InputPredicate.Predicates.Add(new SelectionPredicate {NeedSelected = true, MinRelevancy = 0.8});
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantPoint GetRelevantObjects(RelevantPoint point1, RelevantPoint point2, RelevantPoint point3) {
            return new RelevantPoint((point1.Child + point2.Child + point3.Child) / 3);
        }
    }
}
