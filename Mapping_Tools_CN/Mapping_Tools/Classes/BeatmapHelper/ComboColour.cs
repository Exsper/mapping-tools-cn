﻿using System.Windows.Media;
using Mapping_Tools.Classes.SystemTools;
using static Mapping_Tools.Classes.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Classes.BeatmapHelper {
    /// <summary>
    /// The british alternative because main developer wants to keep the spelling.
    /// Its spelled "Colours" in the game.
    /// </summary>
    public class ComboColour : BindableBase {
        private Color color;

        private bool hasAlpha;

        /// <summary>
        /// The color value of the colour.
        /// </summary>
        public Color Color {
            get => color;
            set => Set(ref color, value);
        }
        
        /// <inheritdoc />
        public ComboColour() {
            Color = new Color();
        }

        /// <inheritdoc />
        public ComboColour(Color color) {
            Color = color;
        }

        /// <inheritdoc />
        public ComboColour(byte r, byte g, byte b) {
            Color = Color.FromRgb(r, g, b);
        }

        /// <inheritdoc />
        public ComboColour(string line) {
            string[] split = line.Split(':');
            string[] commaSplit = split[1].Split(',');

            if (!TryParseInt(commaSplit[0], out int r))
                throw new BeatmapParsingException("转换颜色的红色分量失败。", line);

            if (!TryParseInt(commaSplit[1], out int g))
                throw new BeatmapParsingException("转换颜色的绿色分量失败。", line);

            if (!TryParseInt(commaSplit[2], out int b))
                throw new BeatmapParsingException("转换颜色的蓝色分量失败。", line);

            if (commaSplit.Length > 3) {
                if (!TryParseInt(commaSplit[3], out int a))
                    throw new BeatmapParsingException("转换颜色的透明度分量失败。", line);
                Color = Color.FromArgb((byte)a, (byte)r, (byte)g, (byte)b);
                hasAlpha = true;
            } else {
                Color = Color.FromRgb((byte)r, (byte)g, (byte)b);
            }
        }

        public ComboColour Copy() {
            return (ComboColour) MemberwiseClone();
        }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString() {
            if (hasAlpha) {
                return $"{Color.R.ToInvariant()},{Color.G.ToInvariant()},{Color.B.ToInvariant()},{Color.A.ToInvariant()}";
            }
            return $"{Color.R.ToInvariant()},{Color.G.ToInvariant()},{Color.B.ToInvariant()}";
        }

        public static ComboColour[] GetDefaultComboColours() {
            return new []{new ComboColour(255, 192, 0),
                new ComboColour(0, 202, 0),
                new ComboColour(18, 124, 255),
                new ComboColour(242, 24, 57)};
        }
    }
}
