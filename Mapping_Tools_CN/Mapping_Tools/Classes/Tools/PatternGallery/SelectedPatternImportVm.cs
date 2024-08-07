using Mapping_Tools.Classes.SystemTools;
using System.ComponentModel;

namespace Mapping_Tools.Classes.Tools.PatternGallery {
    public class SelectedPatternImportVm : BindableBase {
        private string name;

        [DisplayName("名称")]
        [Description("Pattern名称。")]
        public string Name { 
            get => name; 
            set => Set(ref name, value);
        }
    }
}