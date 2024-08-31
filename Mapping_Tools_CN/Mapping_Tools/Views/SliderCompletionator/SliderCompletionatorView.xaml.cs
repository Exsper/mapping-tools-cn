using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Mapping_Tools.Classes;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.SystemTools.QuickRun;
using Mapping_Tools.Classes.ToolHelpers;
using Mapping_Tools.Classes.ToolHelpers.Sliders;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Viewmodels;

namespace Mapping_Tools.Views.SliderCompletionator {
    /// <summary>
    /// Interaktionslogik für UserControl1.xaml
    /// </summary>
    [SmartQuickRunUsage(SmartQuickRunTargets.AnySelection)]
    [VerticalContentScroll]
    [HorizontalContentScroll]
    public partial class SliderCompletionatorView : IQuickRun, ISavable<SliderCompletionatorVm> {
        public event EventHandler RunFinished;

        public static readonly string ToolName = "滑条补完器";

        public static readonly string ToolDescription = "修改选中滑条的长度和时长，本工具会自动帮您处理滑条速度。" +
                                                        Environment.NewLine + Environment.NewLine +
                                                        "任何数值输入-1表示不改变该数值。" +
                                                        Environment.NewLine +
                                                        "例如，时长设为1，长度设为-1，将改变时长到1拍，但维持长度不变。" +
                                                        Environment.NewLine + Environment.NewLine +
                                                        "将鼠标停靠至详细工具上以获取更多信息。";

        /// <inheritdoc />
        public SliderCompletionatorView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.ContentViews.Width;
            Height = MainWindow.AppWindow.ContentViews.Height;
            DataContext = new SliderCompletionatorVm();
            ProjectManager.LoadProject(this, message: false);
        }

        public SliderCompletionatorVm ViewModel => (SliderCompletionatorVm) DataContext;

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Complete_Sliders((SliderCompletionatorVm) e.Argument, bgw, e);
        }

       
        private void Start_Click(object sender, RoutedEventArgs e) {
            // Get the current beatmap if the selection mode is 'Selected' because otherwise the selection would always fail
            RunTool(SelectionModeBox.SelectedIndex == 0
                ? new[] {IOHelper.GetCurrentBeatmapOrCurrentBeatmap()}
                : MainWindow.AppWindow.GetCurrentMaps());
        }

        public void QuickRun() {
            RunTool(new[] { IOHelper.GetCurrentBeatmapOrCurrentBeatmap() }, quick: true);
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

        private string Complete_Sliders(SliderCompletionatorVm arg, BackgroundWorker worker, DoWorkEventArgs _) {
            int slidersCompleted = 0;
            double endTime = arg.EndTime;

            var reader = EditorReaderStuff.GetFullEditorReaderOrNot(out var editorReaderException1);

            if (arg.ImportModeSetting == SliderCompletionatorVm.ImportMode.选中的 && editorReaderException1 != null) {
                throw new Exception("无法获取选中物件。", editorReaderException1);
            }

            if (arg.UseCurrentEditorTime && arg.UseEndTime) {
                if (editorReaderException1 != null)
                    throw new Exception("无法获取当前编辑器时间。", editorReaderException1);

                endTime = reader.EditorTime();
            }

            foreach (string path in arg.Paths) {
                var editor = EditorReaderStuff.GetNewestVersionOrNot(path, reader, out var selected, out var editorReaderException2);

                if (arg.ImportModeSetting == SliderCompletionatorVm.ImportMode.选中的 && editorReaderException2 != null) {
                    throw new Exception("无法获取选中物件。", editorReaderException2);
                }

                Beatmap beatmap = editor.Beatmap;
                Timing timing = beatmap.BeatmapTiming;

                List<HitObject> markedObjects = arg.ImportModeSetting switch {
                    SliderCompletionatorVm.ImportMode.选中的 => selected,
                    SliderCompletionatorVm.ImportMode.书签处 => beatmap.GetBookmarkedObjects(),
                    SliderCompletionatorVm.ImportMode.指定时间处 => beatmap.QueryTimeCode(arg.TimeCode).ToList(),
                    SliderCompletionatorVm.ImportMode.所有物件 => beatmap.HitObjects,
                    _ => throw new ArgumentException("意料外的导入模式。")
                };

                for (int i = 0; i < markedObjects.Count; i++) {
                    HitObject ho = markedObjects[i];
                    if (ho.IsSlider) {
                        double mpb = timing.GetMpBAtTime(ho.Time);

                        double oldDuration = timing.CalculateSliderTemporalLength(ho.Time, ho.PixelLength);
                        double oldLength = ho.PixelLength;
                        double oldSv = timing.GetSvAtTime(ho.Time);

                        double newDuration = arg.UseEndTime ? endTime == -1 && !arg.UseCurrentEditorTime ? oldDuration : endTime - ho.Time :
                                                              arg.Duration == -1 ? oldDuration : timing.WalkBeatsInMillisecondTime(arg.Duration, ho.Time) - ho.Time;
                        double newLength = arg.Length == -1 ? oldLength : ho.GetSliderPath(fullLength: true).Distance * arg.Length;
                        double newSv = arg.SliderVelocity == -1 ? oldSv : -100 / arg.SliderVelocity;

                        switch (arg.FreeVariableSetting) {
                            case SliderCompletionatorVm.FreeVariable.速度:
                                newSv = -10000 * timing.SliderMultiplier * newDuration / (newLength * mpb);
                                break;
                            case SliderCompletionatorVm.FreeVariable.时长:
                                // This actually doesn't get used anymore because the .osu doesn't store the duration
                                newDuration = newLength * newSv * mpb / (-10000 * timing.SliderMultiplier);
                                break;
                            case SliderCompletionatorVm.FreeVariable.长度:
                                newLength = -10000 * timing.SliderMultiplier * newDuration / (newSv * mpb);
                                break;
                            default:
                                throw new ArgumentException("意料外的自由变量设置。");
                        }

                        if (double.IsNaN(newSv)) {
                            throw new Exception("计算滑条速度为NaN。请确保输入数值均不为0。");
                        }

                        if (newDuration < 0) {
                            throw new Exception("计算滑条时长为负值。请确保结束时间大于所有被选中滑条的结束时间。");
                        }

                        ho.SliderVelocity = newSv;
                        ho.PixelLength = newLength;

                        // Scale anchors to completion
                        if (arg.MoveAnchors) {
                            ho.SetAllCurvePoints(SliderPathUtil.MoveAnchorsToLength(
                                ho.GetAllCurvePoints(), ho.SliderType, ho.PixelLength, out var pathType));
                            ho.SliderType = pathType;
                        }

                        slidersCompleted++;
                    }
                    if (worker != null && worker.WorkerReportsProgress) {
                        worker.ReportProgress(i / markedObjects.Count);
                    }
                }

                // Reconstruct SliderVelocity
                List<TimingPointsChange> timingPointsChanges = new List<TimingPointsChange>();
                // Add Hitobject stuff
                foreach (HitObject ho in beatmap.HitObjects) {
                    // SliderVelocity changes
                    if (ho.IsSlider) {
                        if (markedObjects.Contains(ho) && arg.DelegateToBpm) {
                            var tpAfter = timing.GetRedlineAtTime(ho.Time).Copy();
                            var tpOn = tpAfter.Copy();

                            tpAfter.Offset = ho.Time;
                            tpOn.Offset = ho.Time - 1;  // This one will be on the slider

                            tpAfter.OmitFirstBarLine = true;
                            tpOn.OmitFirstBarLine = true;

                            // Express velocity in BPM
                            tpOn.MpB *= ho.SliderVelocity / -100;
                            // NaN SV results in removal of slider ticks
                            ho.SliderVelocity = arg.RemoveSliderTicks ? double.NaN : -100;

                            // Add redlines
                            timingPointsChanges.Add(new TimingPointsChange(tpOn, mpb: true, unInherited: true, omitFirstBarLine: true, fuzzyness: Precision.DoubleEpsilon));
                            timingPointsChanges.Add(new TimingPointsChange(tpAfter, mpb: true, unInherited: true, omitFirstBarLine: true, fuzzyness: Precision.DoubleEpsilon));

                            ho.Time -= 1;
                        }

                        TimingPoint tp = ho.TimingPoint.Copy();
                        tp.Offset = ho.Time;
                        tp.MpB = ho.SliderVelocity;
                        timingPointsChanges.Add(new TimingPointsChange(tp, mpb: true, fuzzyness: Precision.DoubleEpsilon));
                    }
                }

                // Add the new SliderVelocity changes
                TimingPointsChange.ApplyChanges(timing, timingPointsChanges);

                // Save the file
                editor.SaveFile();
            }

            // Complete progressbar
            if (worker != null && worker.WorkerReportsProgress)
            {
                worker.ReportProgress(100);
            }

            // Do stuff
            RunFinished?.Invoke(this, new RunToolCompletedEventArgs(true, reader != null, arg.Quick));

            // Make an accurate message
            string message = "";
            if (Math.Abs(slidersCompleted) == 1)
            {
                message += "成功补完 " + slidersCompleted + " 个滑条！";
            }
            else
            {
                message += "成功补完 " + slidersCompleted + " 个滑条！";
            }
            return arg.Quick ? "" : message;
        }
        public SliderCompletionatorVm GetSaveData() {
            return ViewModel;
        }

        public void SetSaveData(SliderCompletionatorVm saveData) {
            DataContext = saveData;
        }

        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "slidercompletionatorproject.json");

        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Slider Completionator Projects");
    }
}
