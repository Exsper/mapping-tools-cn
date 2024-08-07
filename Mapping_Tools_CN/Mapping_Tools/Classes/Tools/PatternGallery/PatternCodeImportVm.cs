using System.ComponentModel;
using System.Windows;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Components.Dialogs.CustomDialog;

namespace Mapping_Tools.Classes.Tools.PatternGallery {
    public class PatternCodeImportVm : BindableBase {
        private string name = string.Empty;
        private string hitObjects = string.Empty;
        private string timingPoints = string.Empty;
        private double globalSv = 1.4;
        private GameMode gameMode = GameMode.Standard;

        [DisplayName("名称")]
        [Description("Pattern名称。")]
        public string Name { 
            get => name; 
            set => Set(ref name, value);
        }

        [MultiLineInput]
        [TextWrapping(TextWrapping.NoWrap)]
        [DisplayName("打击物件")]
        [Description("Pattern中的所有打击物件代码。")]
        public string HitObjects {
            get => hitObjects;
            set => Set(ref hitObjects, value);
        }

        [MultiLineInput]
        [TextWrapping(TextWrapping.NoWrap)]
        [DisplayName("时间轴")]
        [Description("Pattern所在的时间轴。提示：请包含一条红线，使程序在输出时能够缩放timing。")]
        public string TimingPoints {
            get => timingPoints;
            set => Set(ref timingPoints, value);
        }

        [DisplayName("全局SV")]
        [Description("Pattern所用的全局滑条速度。")]
        public double GlobalSv {
            get => globalSv;
            set => Set(ref globalSv, value);
        }

        [DisplayName("游戏模式")]
        [Description("Pattern的游戏模式。")]
        public GameMode GameMode {
            get => gameMode;
            set => Set(ref gameMode, value);
        }
    }
}