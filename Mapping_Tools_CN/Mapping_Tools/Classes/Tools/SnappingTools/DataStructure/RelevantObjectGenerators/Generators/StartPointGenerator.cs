using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class StartPointGenerator : RelevantObjectsGenerator {
        public override string Name => "圆圈和滑条头作点";
        public override string Tooltip => "在圆圈和滑条头位置作辅助点。";
        public override GeneratorType GeneratorType => GeneratorType.Basic;

        public StartPointGenerator() {
            Settings.RelevancyRatio = 1;
            Settings.IsActive = true;
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantPoint GetRelevantObjects(RelevantHitObject ho) {
            return new RelevantPoint(ho.HitObject.Pos);
        }
    }
}
