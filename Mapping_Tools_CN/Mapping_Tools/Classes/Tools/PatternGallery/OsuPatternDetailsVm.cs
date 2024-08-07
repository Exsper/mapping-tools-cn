using System;
using Mapping_Tools.Classes.SystemTools;
using System.ComponentModel;
using Mapping_Tools.Annotations;
using Mapping_Tools.Components.Dialogs.CustomDialog;

namespace Mapping_Tools.Classes.Tools.PatternGallery {
    public class OsuPatternDetailsVm : BindableBase {

        #region Fields

        private string name;

        [UsedImplicitly]
        [DisplayName("名称")]
        [Description("Pattern名称。")]
        public string Name {
            get => name;
            set => Set(ref name, value);
        }

        [UsedImplicitly]
        [DisplayName("制作时间")]
        public DateTime CreationTime { get; }

        [UsedImplicitly]
        [DisplayName("最后使用时间")]
        public DateTime LastUsedTime { get; }

        [UsedImplicitly]
        [DisplayName("使用次数")]
        [InvariantCulture]
        public int UseCount { get; }

        [UsedImplicitly]
        [DisplayName("物件数")]
        [InvariantCulture]
        public int ObjectCount { get; }

        [UsedImplicitly]
        [DisplayName("时长")]
        [InvariantCulture]
        public TimeSpan Duration { get; }

        [UsedImplicitly]
        [DisplayName("节拍数")]
        [InvariantCulture]
        public double BeatLength { get; }

        [UsedImplicitly]
        [DisplayName("文件名")]
        [InvariantCulture]
        public string FileName { get; }

        #endregion

        public OsuPatternDetailsVm(OsuPattern pattern) {
            Name = pattern.Name;
            CreationTime = pattern.CreationTime;
            LastUsedTime = pattern.LastUsedTime;
            UseCount = pattern.UseCount;
            FileName = pattern.FileName;
            ObjectCount = pattern.ObjectCount;
            Duration = pattern.Duration;
            BeatLength = pattern.BeatLength;
        }
    }
}