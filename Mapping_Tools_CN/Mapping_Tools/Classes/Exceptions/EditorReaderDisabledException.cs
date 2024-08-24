using System;

namespace Mapping_Tools.Classes.Exceptions {
    public class EditorReaderDisabledException : Exception {
        public static readonly string EditorReaderDisabledText = "该功能需要开启编辑器读取功能。";
        
        public EditorReaderDisabledException() : base(EditorReaderDisabledText) { }
    }
}