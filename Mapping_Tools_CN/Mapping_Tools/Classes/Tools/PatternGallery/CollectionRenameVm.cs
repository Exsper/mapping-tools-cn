using System.ComponentModel;

namespace Mapping_Tools.Classes.Tools.PatternGallery {
    public class CollectionRenameVm {
        [DisplayName("新名称")]
        [Description("该收藏夹的新名称。")]
        public string NewName { get; set; }

        [DisplayName("新文件夹名称")]
        [Description("Pattern文件夹中该收藏夹的新文件夹名称。")]
        public string NewFolderName { get; set; }
    }
}