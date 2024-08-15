using System;

namespace Mapping_Tools.Classes.Exceptions {
    public class InvalidEditorReaderStateException : Exception {
        public static readonly string InvalidEditorReaderStateText = "无法验证编辑器读取器的状态。";

        public InvalidEditorReaderStateException() : base(InvalidEditorReaderStateText) { }
    }
}