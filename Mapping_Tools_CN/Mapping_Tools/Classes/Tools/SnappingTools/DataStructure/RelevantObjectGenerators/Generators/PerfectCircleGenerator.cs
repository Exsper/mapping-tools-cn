using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class PerfectCircleGenerator : RelevantObjectsGenerator {
        public override string Name => "三点滑条所在圆";
        public override string Tooltip => "选取一个有且只有三个白色锚点的弧形滑条，沿弧形生成完整圆。";
        public override GeneratorType GeneratorType => GeneratorType.Basic;

        public PerfectCircleGenerator() {
            Settings.RelevancyRatio = 1;
            Settings.IsActive = true;
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantCircle GetRelevantObjects(RelevantHitObject relevantHitObject) {
            var ho = relevantHitObject.HitObject;
            return ho.IsSlider && ho.SliderType == PathType.PerfectCurve && ho.CurvePoints.Count == 2
                ? new RelevantCircle(new Circle(new CircleArc(ho.GetAllCurvePoints())))
                : null;
        }
    }
}
