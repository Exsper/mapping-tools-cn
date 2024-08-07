using Mapping_Tools.Classes.SystemTools;
using System.ComponentModel;

namespace Mapping_Tools.Classes.Tools.PatternGallery {
    public class NewGroupVm : BindableBase {
        private string groupName;

        [DisplayName("组名")]
        [Description("该组的新名称。")]
        public string GroupName {
            get => groupName;
            set => Set(ref groupName, value);
        }
    }
}