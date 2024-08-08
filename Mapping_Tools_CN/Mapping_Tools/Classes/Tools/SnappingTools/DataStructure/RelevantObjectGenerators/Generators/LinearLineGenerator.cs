using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class LinearLineGenerator : RelevantObjectsGenerator {
        public override string Name => "直线滑条";
        public override string Tooltip => "在直线滑条上生成辅助线。";
        public override GeneratorType GeneratorType => GeneratorType.Basic;

        public LinearLineGenerator() {
            Settings.RelevancyRatio = 1;
            Settings.IsActive = true;
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantLine GetRelevantObjects(RelevantHitObject relevantHitObject) {
            var ho = relevantHitObject.HitObject;
            return ho.IsSlider && ho.SliderType == PathType.Linear && ho.CurvePoints.Count >= 1
                ? new RelevantLine(Line2.FromPoints(ho.Pos, ho.CurvePoints.Last()))
                : null;
        }
    }
}
