using System.ComponentModel;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorSettingses
{
    public class SinglePointCircleGeneratorSettings : GeneratorSettings
    {
        private double radius;
        [DisplayName("半径")]
        [Description("以osu!像素为单位的圆半径。")]
        public double Radius
        {
            get => radius;
            set => Set(ref radius, value);
        }

        public SinglePointCircleGeneratorSettings()
        {
            Radius = 0;
        }

        public override object Clone()
        {
            return new SinglePointCircleGeneratorSettings
            {
                Generator = Generator,
                IsActive = IsActive,
                IsSequential = IsSequential,
                IsDeep = IsDeep,
                RelevancyRatio = RelevancyRatio,
                GeneratesInheritable = GeneratesInheritable,
                InputPredicate = (SelectionPredicateCollection)InputPredicate.Clone(),
                Radius = Radius
            };
        }
    }
}