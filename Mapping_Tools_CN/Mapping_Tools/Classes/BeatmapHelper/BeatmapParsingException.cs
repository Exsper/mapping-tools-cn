using System;

namespace Mapping_Tools.Classes.BeatmapHelper
{
    [Serializable]
    class BeatmapParsingException : Exception
    {
        public BeatmapParsingException() {

        }

        public BeatmapParsingException(string line)
            : base($"分析谱面时遇到意外数值。\n{line}") {

        }

        public BeatmapParsingException(string message, string line)
            : base($"{message}\n{line}") {

        }
    }
}
