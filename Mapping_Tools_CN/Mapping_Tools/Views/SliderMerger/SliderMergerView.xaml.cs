using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.SystemTools.QuickRun;
using Mapping_Tools.Classes.ToolHelpers;
using Mapping_Tools.Classes.ToolHelpers.Sliders;
using Mapping_Tools.Viewmodels;

namespace Mapping_Tools.Views.SliderMerger {
    /// <summary>
    ///     Interaktionslogik für UserControl1.xaml
    /// </summary>
    [SmartQuickRunUsage(SmartQuickRunTargets.MultipleSelection)]
    [VerticalContentScroll]
    [HorizontalContentScroll]
    public partial class SliderMergerView : IQuickRun, ISavable<SliderMergerVm> {
        public static readonly string ToolName = "滑条合并器";

        public static readonly string ToolDescription =
            $@"将多个滑条组合为长滑条。{Environment.NewLine}该程序会自动转换任何类型的滑条变成贝塞尔滑条来进行合并。{Environment.NewLine}圆圈也能被合并，且固定使用直线连接模式。";

        public SliderMergerView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.ContentViews.Width;
            Height = MainWindow.AppWindow.ContentViews.Height;
            DataContext = new SliderMergerVm();
            ProjectManager.LoadProject(this, message: false);
        }

        public SliderMergerVm ViewModel => (SliderMergerVm) DataContext;

        public event EventHandler RunFinished;

        public void QuickRun() {
            RunTool(new[] {IOHelper.GetCurrentBeatmapOrCurrentBeatmap()}, true);
        }

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Merge_Sliders((SliderMergerVm) e.Argument, bgw);
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            // Get the current beatmap if the selection mode is 'Selected' because otherwise the selection would always fail
            RunTool(SelectionModeBox.SelectedIndex == 0
                ? new[] {IOHelper.GetCurrentBeatmapOrCurrentBeatmap()}
                : MainWindow.AppWindow.GetCurrentMaps());
        }

        private void RunTool(string[] paths, bool quick = false) {
            if (!CanRun) return;

            // Remove logical focus to trigger LostFocus on any fields that didn't yet update the ViewModel
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(this), null);

            BackupManager.SaveMapBackup(paths);

            ViewModel.Paths = paths;
            ViewModel.Quick = quick;

            BackgroundWorker.RunWorkerAsync(ViewModel);
            CanRun = false;
        }

        private string Merge_Sliders(SliderMergerVm arg, BackgroundWorker worker) {
            var slidersMerged = 0;

            var reader = EditorReaderStuff.GetFullEditorReaderOrNot(out var editorReaderException1);

            if (arg.ImportModeSetting == 0 && editorReaderException1 != null) {
                throw new Exception("无法获取选中物件。", editorReaderException1);
            }

            foreach (var path in arg.Paths) {
                var editor = EditorReaderStuff.GetNewestVersionOrNot(path, reader, out var selected, out var editorReaderException2);

                if (arg.ImportModeSetting == SliderMergerVm.ImportMode.Selected && editorReaderException2 != null) {
                    throw new Exception("无法获取选中物件。", editorReaderException2);
                }

                var beatmap = editor.Beatmap;
                var markedObjects = arg.ImportModeSetting == 0 ? selected :
                    arg.ImportModeSetting == SliderMergerVm.ImportMode.Bookmarked ? beatmap.GetBookmarkedObjects() :
                    arg.ImportModeSetting == SliderMergerVm.ImportMode.Time ? beatmap.QueryTimeCode(arg.TimeCode).ToList() :
                    beatmap.HitObjects;

                var mergeLast = false;
                for (var i = 0; i < markedObjects.Count - 1; i++) {
                    if (worker != null && worker.WorkerReportsProgress) worker.ReportProgress(i / markedObjects.Count);

                    var ho1 = markedObjects[i];
                    var ho2 = markedObjects[i + 1];

                    var lastPos1 = ho1.IsSlider
                        ? arg.MergeOnSliderEnd ? ho1.GetSliderPath().PositionAt(1) : ho1.CurvePoints.Last()
                        : ho1.Pos;

                    double dist = Vector2.Distance(lastPos1, ho2.Pos);

                    if (dist > arg.Leniency || !(ho2.IsSlider || ho2.IsCircle) || !(ho1.IsSlider || ho1.IsCircle)) {
                        mergeLast = false;
                        continue;
                    }

                    if (ho1.IsSlider && ho2.IsSlider) {
                        if (arg.MergeOnSliderEnd) {
                            // In order to merge on the slider end we first move the anchors such that the last anchor is exactly on the slider end
                            // After that merge as usual
                            ho1.SetAllCurvePoints(SliderPathUtil.MoveAnchorsToLength(
                                ho1.GetAllCurvePoints(), ho1.SliderType, ho1.PixelLength, out var pathType));
                            ho1.SliderType = pathType;
                        }

                        var sp1 = BezierConverter.ConvertToBezierAnchors(ho1.GetAllCurvePoints(), ho1.SliderType);
                        var sp2 = BezierConverter.ConvertToBezierAnchors(ho2.GetAllCurvePoints(), ho2.SliderType);

                        double extraLength = 0;
                        switch (arg.ConnectionModeSetting) {
                            case SliderMergerVm.ConnectionMode.Move:
                                Move(sp2, sp1.Last() - sp2.First());
                                break;
                            case SliderMergerVm.ConnectionMode.Linear:
                                sp1.Add(sp1.Last());
                                sp1.Add(sp2.First());
                                extraLength = (ho1.CurvePoints.Last() - ho2.Pos).Length;
                                break;
                        }

                        var mergedAnchors = sp1.Concat(sp2).ToList();
                        mergedAnchors.Round();

                        var linearLinear = arg.LinearOnLinear && IsLinearBezier(sp1) && IsLinearBezier(sp2);
                        if (linearLinear) {
                            for (var j = 0; j < mergedAnchors.Count - 1; j++) {
                                if (mergedAnchors[j] != mergedAnchors[j + 1]) continue;
                                mergedAnchors.RemoveAt(j);
                                j--;
                            }
                        }

                        ho1.SetAllCurvePoints(mergedAnchors);
                        ho1.SliderType = linearLinear ? PathType.Linear : PathType.Bezier;
                        ho1.PixelLength = ho1.PixelLength + ho2.PixelLength + extraLength;

                        beatmap.HitObjects.Remove(ho2);
                        markedObjects.Remove(ho2);
                        i--;

                        slidersMerged++;
                        if (!mergeLast) slidersMerged++;
                        mergeLast = true;
                    } else if (ho1.IsSlider && ho2.IsCircle) {
                        if (Precision.DefinitelyBigger(dist, 0)) {
                            var sp1 = BezierConverter.ConvertToBezierAnchors(ho1.GetAllCurvePoints(), ho1.SliderType);

                            sp1.Add(sp1.Last());
                            sp1.Add(ho2.Pos);
                            var extraLength = (ho1.CurvePoints.Last() - ho2.Pos).Length;

                            var mergedAnchors = sp1;
                            mergedAnchors.Round();

                            var linearLinear = arg.LinearOnLinear && IsLinearBezier(sp1);
                            if (linearLinear) {
                                for (var j = 0; j < mergedAnchors.Count - 1; j++) {
                                    if (mergedAnchors[j] != mergedAnchors[j + 1]) continue;
                                    mergedAnchors.RemoveAt(j);
                                    j--;
                                }
                            }

                            ho1.SetAllCurvePoints(mergedAnchors);
                            ho1.SliderType = linearLinear ? PathType.Linear : PathType.Bezier;
                            ho1.PixelLength += extraLength;
                        }

                        beatmap.HitObjects.Remove(ho2);
                        markedObjects.Remove(ho2);
                        i--;

                        slidersMerged++;
                        if (!mergeLast) slidersMerged++;
                        mergeLast = true;
                    } else if (ho1.IsCircle && ho2.IsSlider) {
                        if (Precision.DefinitelyBigger(dist, 0)) {
                            var sp2 = BezierConverter.ConvertToBezierAnchors(ho2.GetAllCurvePoints(), ho2.SliderType);

                            sp2.Insert(0, sp2.First());
                            sp2.Insert(0, ho1.Pos);
                            var extraLength = (ho1.Pos - ho2.Pos).Length;

                            var mergedAnchors = sp2;
                            mergedAnchors.Round();

                            var linearLinear = arg.LinearOnLinear && IsLinearBezier(sp2);
                            if (linearLinear) {
                                for (var j = 0; j < mergedAnchors.Count - 1; j++) {
                                    if (mergedAnchors[j] != mergedAnchors[j + 1]) continue;
                                    mergedAnchors.RemoveAt(j);
                                    j--;
                                }
                            }

                            ho2.SetAllCurvePoints(mergedAnchors);
                            ho2.SliderType = linearLinear ? PathType.Linear : PathType.Bezier;
                            ho2.PixelLength += extraLength;
                        }

                        beatmap.HitObjects.Remove(ho1);
                        markedObjects.Remove(ho1);
                        i--;

                        slidersMerged++;
                        if (!mergeLast) slidersMerged++;
                        mergeLast = true;
                    } else if (ho1.IsCircle && ho2.IsCircle) {
                        if (Precision.DefinitelyBigger(dist, 0)) {
                            var mergedAnchors = new List<Vector2> {ho1.Pos, ho2.Pos};

                            ho1.SetAllCurvePoints(mergedAnchors);
                            ho1.SliderType = arg.LinearOnLinear ? PathType.Linear : PathType.Bezier;
                            ho1.PixelLength = (ho1.Pos - ho2.Pos).Length;
                            ho1.IsCircle = false;
                            ho1.IsSlider = true;
                            ho1.Repeat = 1;
                            ho1.EdgeHitsounds = new List<int> {ho1.GetHitsounds(), ho2.GetHitsounds()};
                            ho1.EdgeSampleSets = new List<SampleSet> {ho1.SampleSet, ho2.SampleSet};
                            ho1.EdgeAdditionSets = new List<SampleSet> {ho1.AdditionSet, ho2.AdditionSet};
                        }

                        beatmap.HitObjects.Remove(ho2);
                        markedObjects.Remove(ho2);
                        i--;

                        slidersMerged++;
                        if (!mergeLast) slidersMerged++;
                        mergeLast = true;
                    }
                    else {
                        mergeLast = false;
                    }

                    if (mergeLast && arg.Leniency == 727) {
                        ho1.SetAllCurvePoints(MakePenis(ho1.GetAllCurvePoints(), ho1.PixelLength));
                        ho1.PixelLength *= 2;
                        ho1.SliderType = PathType.Bezier;
                    }
                }

                // Save the file
                editor.SaveFile();
            }

            // Complete progressbar
            if (worker != null && worker.WorkerReportsProgress) worker.ReportProgress(100);

            // Do stuff
            RunFinished?.Invoke(this, new RunToolCompletedEventArgs(true, reader != null, arg.Quick));

            // Make an accurate message
            var message = "";
            if (Math.Abs(slidersMerged) == 1)
                message += "成功合并 " + slidersMerged + " 个滑条！";
            else
                message += "成功合并 " + slidersMerged + " 个滑条！";
            return arg.Quick ? "" : message;
        }

        private static List<Vector2> MakePenis(List<Vector2> points, double sliderLength) {
            // Penis shape
            List<Vector2> newPoints = new List<Vector2>
            {
                new(0,0),
                new(40,-40),
                new(0,-70),
                new(-40,-40),
                new(0,0),
                new(0,0),
                new(96,24),
                new(168,0),
                new(168,0),
                new(96,-24),
                new(0,0),
                new(0,0),
                new(-40,40),
                new(0,70),
                new(40,40),
                new(0,0)
            };

            double sizeMultiplier = sliderLength / 591 * 2;  // 591 is the size of the dick
            double normalAngle = -(points.Last() - points.First()).Theta;
            var mat = Matrix2.CreateRotation(normalAngle);
            mat *= sizeMultiplier;

            for (var i = 0; i < newPoints.Count; i++) {
                Vector2 point = newPoints[i];
                // transform to slider
                Vector2 transformPoint = Matrix2.Mult(mat, point);
                newPoints[i] = points.First() + transformPoint;
            }

            return newPoints;
        }

        public static bool IsLinearBezier(List<Vector2> points) {
            // Every point at not the endpoints must have an anchor before or after it at the same position
            for (var i = 1; i < points.Count-1; i++) {
                if (points[i] != points[i - 1] && points[i] != points[i + 1]) {
                    return false;
                }
            }

            return true;
        }

        public static void Move(List<Vector2> points,  Vector2 delta) {
            for (var i = 0; i < points.Count; i++) {
                points[i] = points[i] + delta;
            }
        }
        public SliderMergerVm GetSaveData() {
            return ViewModel;
        }

        public void SetSaveData(SliderMergerVm saveData) {
            DataContext = saveData;
        }

        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "slidermergerproject.json");

        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Slider Merger Projects");
    }
}