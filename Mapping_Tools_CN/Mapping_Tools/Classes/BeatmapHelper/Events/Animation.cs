using System;
using Mapping_Tools.Classes.MathUtil;
using static Mapping_Tools.Classes.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Classes.BeatmapHelper.Events {
    public class Animation : Event, IHasDuration {
        public StoryboardLayer Layer { get; set; }
        public Origin Origin { get; set; }

        /// <summary>
        /// This is a partial path to the image file for this sprite.
        /// </summary>
        public string FilePath { get; set; }

        public Vector2 Pos { get; set; }

        public int FrameCount { get; set; }
        public double FrameDelay { get; set; }
        public LoopType LoopType { get; set; }

        /// <summary>
        /// Serializes this object to .osu code.
        /// </summary>
        /// <returns></returns>
        public override string GetLine() {
            return $"Animation,{Layer},{Origin},\"{FilePath}\",{Pos.X.ToInvariant()},{Pos.Y.ToInvariant()},{FrameCount.ToInvariant()},{FrameDelay.ToInvariant()},{LoopType}";
        }

        /// <summary>
        /// Deserializes a string of .osu code and populates the properties of this object.
        /// </summary>
        /// <param name="line"></param>
        public override void SetLine(string line) {
            string[] values = line.Split(',');

            if (values[0] != "Animation") {
                throw new BeatmapParsingException("该行不是一个动画。", line);
            }

            if (Enum.TryParse(values[1], out StoryboardLayer layer))
                Layer = layer;
            else throw new BeatmapParsingException("转换动画行失败。", line);

            if (Enum.TryParse(values[2], out Origin origin))
                Origin = origin;
            else throw new BeatmapParsingException("转换动画原点失败。", line);

            FilePath = values[3].Trim('"');

            if (!TryParseDouble(values[4], out double x))
                throw new BeatmapParsingException("转换动画X轴位置失败。", line);

            if (!TryParseDouble(values[5], out double y))
                throw new BeatmapParsingException("转换动画Y轴位置失败。", line);

            Pos = new Vector2(x, y);

            if (TryParseInt(values[6], out int frameCount))
                FrameCount = frameCount;
            else throw new BeatmapParsingException("转换动画帧数失败。", line);

            if (TryParseDouble(values[7], out double frameDelay))
                FrameDelay = frameDelay;
            else throw new BeatmapParsingException("转换动画帧延迟失败。", line);

            if (Enum.TryParse(values[8], out LoopType loopType))
                LoopType = loopType;
            else throw new BeatmapParsingException("转换动画循环类型失败。", line);
        }

        public double Duration { 
            get => FrameDelay;
            set => FrameDelay = value;
        }
    }
}