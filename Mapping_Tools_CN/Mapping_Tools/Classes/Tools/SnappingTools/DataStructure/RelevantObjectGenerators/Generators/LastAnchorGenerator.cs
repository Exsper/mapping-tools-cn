using System.Linq;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class LastAnchorGenerator : RelevantObjectsGenerator {
        public override string Name => "滑条末尾锚点";
        public override string Tooltip => "在滑条最后一个锚点上生成辅助点。";
        public override GeneratorType GeneratorType => GeneratorType.基本;
        public override GeneratorTemporalPositioning TemporalPositioning => GeneratorTemporalPositioning.Custom;

        public LastAnchorGenerator() {
            Settings.RelevancyRatio = 1;
            Settings.IsActive = true;
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantPoint GetRelevantObjects(RelevantHitObject relevantHitObject) {
            var ho = relevantHitObject.HitObject;
            if (ho.CurvePoints == null || ho.CurvePoints.Count == 0)
                return null;
            return ho.IsSlider ? new RelevantPoint(ho.CurvePoints.Last()) { CustomTime = ho.EndTime } : null;
        }
    }
}
