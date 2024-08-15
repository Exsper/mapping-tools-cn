using System;

namespace Mapping_Tools.Classes.Exceptions {
    public class BeatmapIncompatibleException : Exception {
        public static readonly string BeatmapIncompatibleText = "该谱面不适用该操作。";
        
        public BeatmapIncompatibleException() : base(BeatmapIncompatibleText) { }

        public BeatmapIncompatibleException(string message) : base(message) { }

        public BeatmapIncompatibleException(string message, Exception innerException) : base(message, innerException) { }
    }
}