﻿using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorSettingses;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class SymmetryGenerator : RelevantObjectsGenerator {
        public override string Name => "镜像";
        public override string Tooltip => "将辅助线作为对称轴，作选中辅助对象的镜像。在设置中修改选择对称轴的额外规则。";
        public override GeneratorType GeneratorType => GeneratorType.高级;

        private SymmetryGeneratorSettings MySettings => (SymmetryGeneratorSettings) Settings;

        /// <summary>
        /// Initializes SymmetryGenerator with a custom settings object
        /// </summary>
        public SymmetryGenerator() : base(new SymmetryGeneratorSettings()) {
            Settings.Generator = this;

            Settings.RelevancyRatio = 0.8;
            Settings.IsActive = true;
            Settings.IsDeep = true;
            MySettings.AxisInputPredicate.Predicates.Add(new SelectionPredicate {NeedSelected = true, NeedLocked = true, NeedGeneratedNotByThis = true});
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantPoint GetRelevantObjects(RelevantLine axis, RelevantPoint point) {
            return !MySettings.AxisInputPredicate.Check(axis, this) || !MySettings.OtherInputPredicate.Check(point, this) ? null : 
                new RelevantPoint(Vector2.Mirror(point.Child, axis.Child));
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantLine GetRelevantObjects(RelevantLine line1, RelevantLine line2) {
            // Any line can be the axis
            if (MySettings.AxisInputPredicate.Check(line1, this) && 
                MySettings.OtherInputPredicate.Check(line2, this)) {
                return new RelevantLine(Line2.FromPoints(Vector2.Mirror(line2.Child.PositionVector, line1.Child),
                    Vector2.Mirror(line2.Child.PositionVector + line2.Child.DirectionVector, line1.Child)));
            }

            if (MySettings.AxisInputPredicate.Check(line2, this) &&
                MySettings.OtherInputPredicate.Check(line1, this)) {
                return new RelevantLine(Line2.FromPoints(Vector2.Mirror(line1.Child.PositionVector, line2.Child),
                    Vector2.Mirror(line1.Child.PositionVector + line1.Child.DirectionVector, line2.Child)));
            }

            return null;
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantCircle GetRelevantObjects(RelevantLine axis, RelevantCircle circle) {
            return !MySettings.AxisInputPredicate.Check(axis, this) || !MySettings.OtherInputPredicate.Check(circle, this) ? null : 
                new RelevantCircle(new Circle(Vector2.Mirror(circle.Child.Centre, axis.Child), circle.Child.Radius));
        }
    }
}
