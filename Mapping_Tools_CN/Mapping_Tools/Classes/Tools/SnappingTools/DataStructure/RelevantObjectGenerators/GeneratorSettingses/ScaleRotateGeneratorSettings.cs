using System.ComponentModel;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorSettingses {
    public class ScaleRotateGeneratorSettings : GeneratorSettings {
        private double angle;
        [DisplayName("角度")]
        [Description("旋转的角度（单位为度）。正数为逆时针旋转，负数为顺时针旋转。")]
        public double Angle {
            get => angle;
            set => Set(ref angle, value);
        }

        private double scalar;
        [DisplayName("缩放")]
        [Description("统一缩放为多少倍大小。")]
        public double Scalar {
            get => scalar;
            set => Set(ref scalar, value);
        }

        private SelectionPredicateCollection originInputPredicate;
        [DisplayName("原点规则")]
        [Description("选取原点需要遵守的额外规则。")]
        public SelectionPredicateCollection OriginInputPredicate {
            get => originInputPredicate;
            set => Set(ref originInputPredicate, value);
        }
        
        private SelectionPredicateCollection otherInputPredicate;
        [DisplayName("其他输入规则")]
        [Description("需要被旋转和缩放的辅助物件需要遵守的额外规则。")]
        public SelectionPredicateCollection OtherInputPredicate {
            get => otherInputPredicate;
            set => Set(ref otherInputPredicate, value);
        }

        public ScaleRotateGeneratorSettings() {
            Angle = 0;
            Scalar = 1;
            OriginInputPredicate = new SelectionPredicateCollection();
            OtherInputPredicate = new SelectionPredicateCollection();
        }

        public override object Clone() {
            return new ScaleRotateGeneratorSettings {Generator = Generator, IsActive = IsActive, IsSequential = IsSequential, IsDeep = IsDeep, 
                RelevancyRatio = RelevancyRatio, GeneratesInheritable = GeneratesInheritable,
                InputPredicate = (SelectionPredicateCollection)InputPredicate.Clone(),
                Angle = Angle, Scalar = Scalar, 
                OriginInputPredicate = (SelectionPredicateCollection)OriginInputPredicate.Clone(),
                OtherInputPredicate = (SelectionPredicateCollection)OtherInputPredicate.Clone()
            };
        }
    }
}