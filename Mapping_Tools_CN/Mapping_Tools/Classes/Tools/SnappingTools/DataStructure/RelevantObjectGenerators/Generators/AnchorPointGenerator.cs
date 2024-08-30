using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class AnchorPointGenerator : RelevantObjectsGenerator {
        public override string Name => "滑条锚点";
        public override string Tooltip => "在滑条锚点上生成辅助点。";
        public override GeneratorType GeneratorType => GeneratorType.基本;
        public override GeneratorTemporalPositioning TemporalPositioning => GeneratorTemporalPositioning.Custom;

        public AnchorPointGenerator() {
            Settings.RelevancyRatio = 0.8;
            Settings.IsActive = true;
        }

        [RelevantObjectsGeneratorMethod]
        public IEnumerable<RelevantPoint> GetRelevantObjects(RelevantHitObject relevantHitObject) {
            var ho = relevantHitObject.HitObject;
            if (!ho.IsSlider) return null;
            var curvePoints = ho.GetAllCurvePoints();
            return curvePoints.Select((o, i) => new RelevantPoint(o) { CustomTime = (double)i / (curvePoints.Count - 1) * (ho.EndTime - ho.Time) + ho.Time });
        }
    }
}
