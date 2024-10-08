﻿using System.Text;
using static Mapping_Tools.Classes.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Classes.BeatmapHelper.Events {
    /// <summary>
    /// Represents the standard loop event. This event has a different syntax so it can't be a <see cref="OtherCommand"/>.
    /// </summary>
    public class StandardLoop : Command {
        public override EventType EventType => EventType.L;

        public int LoopCount { get; set; }

        public override string GetLine() {
            return $"{EventType},{(SaveWithFloatPrecision ? StartTime.ToInvariant() : StartTime.ToRoundInvariant())},{LoopCount.ToInvariant()}";
        }

        public override void SetLine(string line) {
            var subLine = RemoveIndents(line);
            var values = subLine.Split(',');

            if (TryParseDouble(values[1], out double startTime))
                StartTime = startTime;
            else throw new BeatmapParsingException("转换事件参数的开始时间失败。", line);

            if (TryParseInt(values[2], out int loopCount))
                LoopCount = loopCount;
            else throw new BeatmapParsingException("转换事件参数的循环次数失败。", line);
        }
    }
}