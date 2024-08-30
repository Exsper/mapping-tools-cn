using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class SliderEndGenerator : RelevantObjectsGenerator {
        public override string Name => "滑条尾作点";
        public override string Tooltip => "在滑条尾作辅助点。";
        public override GeneratorType GeneratorType => GeneratorType.基本;
        public override GeneratorTemporalPositioning TemporalPositioning => GeneratorTemporalPositioning.Custom;

        public SliderEndGenerator() {
            Settings.RelevancyRatio = 0.8;
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantPoint GetRelevantObjects(RelevantHitObject relevantHitObject) {
            var ho = relevantHitObject.HitObject;
            return ho.IsSlider ? new RelevantPoint(ho.GetSliderPath().PositionAt(1)) { CustomTime = ho.EndTime } : null;
        }
    }
}
