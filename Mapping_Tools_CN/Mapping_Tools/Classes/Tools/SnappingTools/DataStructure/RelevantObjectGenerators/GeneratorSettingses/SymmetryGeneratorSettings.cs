using System.ComponentModel;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorSettingses {
    public class SymmetryGeneratorSettings : GeneratorSettings {
        private SelectionPredicateCollection axisInputPredicate;
        [DisplayName("对称轴规则")]
        [Description("选取对称轴需要遵守的额外规则。")]
        public SelectionPredicateCollection AxisInputPredicate {
            get => axisInputPredicate;
            set => Set(ref axisInputPredicate, value);
        }
        
        private SelectionPredicateCollection otherInputPredicate;
        [DisplayName("其他输入规则")]
        [Description("需要被镜像的辅助物件需要遵守的额外规则。")]
        public SelectionPredicateCollection OtherInputPredicate {
            get => otherInputPredicate;
            set => Set(ref otherInputPredicate, value);
        }

        public SymmetryGeneratorSettings() {
            AxisInputPredicate = new SelectionPredicateCollection();
            OtherInputPredicate = new SelectionPredicateCollection();
        }

        public override object Clone() {
            return new SymmetryGeneratorSettings {Generator = Generator, IsActive = IsActive, IsSequential = IsSequential, IsDeep = IsDeep, 
                RelevancyRatio = RelevancyRatio, GeneratesInheritable = GeneratesInheritable,
                InputPredicate = (SelectionPredicateCollection)InputPredicate.Clone(),
                AxisInputPredicate = (SelectionPredicateCollection)AxisInputPredicate.Clone(),
                OtherInputPredicate = (SelectionPredicateCollection)OtherInputPredicate.Clone()
            };
        }
    }
}