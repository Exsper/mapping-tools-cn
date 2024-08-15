using System;
using Mapping_Tools.Classes.MathUtil;
using static Mapping_Tools.Classes.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Classes.BeatmapHelper.Events {
    public class Sprite : Event {
        public StoryboardLayer Layer { get; set; }
        public Origin Origin { get; set; }

        /// <summary>
        /// This is a partial path to the image file for this sprite.
        /// </summary>
        public string FilePath { get; set; }

        public Vector2 Pos { get; set; }

        /// <summary>
        /// Serializes this object to .osu code.
        /// </summary>
        /// <returns></returns>
        public override string GetLine() {
            return $"Sprite,{Layer},{Origin},\"{FilePath}\",{Pos.X.ToInvariant()},{Pos.Y.ToInvariant()}";
        }

        /// <summary>
        /// Deserializes a string of .osu code and populates the properties of this object.
        /// </summary>
        /// <param name="line"></param>
        public override void SetLine(string line) {
            string[] values = line.Split(',');

            if (values[0] != "Sprite") {
                throw new BeatmapParsingException("该行不是一个精灵图。", line);
            }

            if (Enum.TryParse(values[1], out StoryboardLayer layer))
                Layer = layer;
            else throw new BeatmapParsingException("转换精灵图的图层失败。", line);

            if (Enum.TryParse(values[2], out Origin origin))
                Origin = origin;
            else throw new BeatmapParsingException("转换精灵图的原点失败。", line);

            FilePath = values[3].Trim('"');

            if (!TryParseDouble(values[4], out double x))
                throw new BeatmapParsingException("转换精灵图的X轴坐标失败。", line);

            if (!TryParseDouble(values[5], out double y))
                throw new BeatmapParsingException("转换精灵图的Y轴坐标失败。", line);

            Pos = new Vector2(x, y);
        }
    }
}