namespace Mapping_Tools.Classes.Tools.PatternGallery {
    public enum PatternOverwriteMode {
        /// <summary>
        /// Remove no objects from the original beatmap.
        /// </summary>
        不覆盖,
        /// <summary>
        /// Remove objects from the original beatmap only in dense parts of the pattern.
        /// </summary>
        分区覆盖,
        /// <summary>
        /// Remove all objects from the original beatmap between the start time of the pattern and the end time of the pattern.
        /// </summary>
        完全覆盖,
    }
}