using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class PerfectCircleBlanketGenerator : RelevantObjectsGenerator {
        public override string Name => "包裹（Blanket）中心点";
        public override string Tooltip => "选取一个弧形滑条，生成包裹（Blanket）中心点。";
        public override GeneratorType GeneratorType => GeneratorType.Basic;

        public PerfectCircleBlanketGenerator() {
            Settings.RelevancyRatio = 0.8;
            Settings.IsActive = true;
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantPoint GetRelevantObjects(RelevantHitObject relevantHitObject) {
            var ho = relevantHitObject.HitObject;
            return ho.IsSlider && ho.SliderType == PathType.PerfectCurve && ho.CurvePoints.Count == 2
                ? new RelevantPoint(new Circle(new CircleArc(ho.GetAllCurvePoints())).Centre)
                : null;
        }
    }
}
