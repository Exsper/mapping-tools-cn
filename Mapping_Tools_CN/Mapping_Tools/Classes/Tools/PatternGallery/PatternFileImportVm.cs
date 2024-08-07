using System.ComponentModel;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Components.Dialogs.CustomDialog;

namespace Mapping_Tools.Classes.Tools.PatternGallery {
    public class PatternFileImportVm : BindableBase {
        private string name = string.Empty;
        private string filePath = string.Empty;
        private string filter = string.Empty;
        private double startTime = -1;
        private double endTime = -1;

        [DisplayName("名称")]
        [Description("Pattern名称。")]
        public string Name { 
            get => name; 
            set => Set(ref name, value);
        }

        [BeatmapBrowse]
        [DisplayName("Pattern文件路径")]
        [Description("导入的Pattern文件路径。")]
        public string FilePath {
            get => filePath;
            set => Set(ref filePath, value);
        }

        [DisplayName("筛选器")]
        [Description("在此处输入时间码。例如：00:56:823 (1,2,1,2) - ")]
        public string Filter {
            get => filter;
            set => Set(ref filter, value);
        }

        [TimeInput]
        [ConverterParameter(-1)]
        [DisplayName("开始时间")]
        [Description("可选填的开始时间。在此之前的物件会被忽略。")]
        public double StartTime {
            get => startTime;
            set => Set(ref startTime, value);
        }

        [TimeInput]
        [ConverterParameter(-1)]
        [DisplayName("结束时间")]
        [Description("可选填的结束时间。在此之前的物件会被忽略。")]
        public double EndTime {
            get => endTime;
            set => Set(ref endTime, value);
        }
    }
}