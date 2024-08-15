using static Mapping_Tools.Classes.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Classes.BeatmapHelper.Events {
    public class Break : Event, IHasStartTime, IHasEndTime {
        public string EventType { get; set; }
        public double StartTime { get; set; }
        public double EndTime { get; set; }

        public Break() { }

        public Break(string line) {
            SetLine(line);
        }

        public override string GetLine() {
            return $"{EventType},{(SaveWithFloatPrecision ? StartTime.ToInvariant() : StartTime.ToRoundInvariant())},{(SaveWithFloatPrecision ? EndTime.ToInvariant() : EndTime.ToRoundInvariant())}";
        }

        public override sealed void SetLine(string line) {
            string[] values = line.Split(',');

            // Either 'Break' or '2' indicates a break. We save the value so we dont accidentally change it.
            if (values[0] != "2" && values[0] != "Break") {
                throw new BeatmapParsingException("该行不是一个休息段。", line);
            }

            EventType = values[0];

            if (TryParseDouble(values[1], out double startTime))
                StartTime = startTime;
            else throw new BeatmapParsingException("转换休息段开始时间失败。", line);

            if (TryParseDouble(values[2], out double endTime))
                EndTime = endTime;
            else throw new BeatmapParsingException("转换休息段结束时间失败。", line);
        }
    }
}