using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.ToolHelpers;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Viewmodels;

namespace Mapping_Tools.Views.TimingCopier {
    /// <summary>
    /// Interactielogica voor TimingCopierView.xaml
    /// </summary>
    [VerticalContentScroll]
    public partial class TimingCopierView : ISavable<TimingCopierVm> {
        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "timingcopierproject.json");

        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Timing Copier Projects");

        public static readonly string ToolName = "Timing 复制器";

        public static readonly string ToolDescription = $@"将A谱面的timing复制到B谱面。{Environment.NewLine}程序有 3 种工作模式处理移动物件（打击物件/时间轴/书签）以适配新timing：{Environment.NewLine}“物件间的节拍数保持不变”模式在移动并保证物件间节拍数不变后，也会按照指定的节拍细分自动对齐。注意使用前确保所有物件都已对齐，并且当新timing更改了物件间的节拍数时请勿使用该模式。{Environment.NewLine}“仅重新对齐”模式将物件对齐到新timing指定的节拍细分上。该模式不对齐书签。{Environment.NewLine}“不移动物件”模式不会移动任何物件。";

        public TimingCopierView() {
            InitializeComponent();
            DataContext = new TimingCopierVm();
            Width = MainWindow.AppWindow.ContentViews.Width;
            Height = MainWindow.AppWindow.ContentViews.Height;
            ProjectManager.LoadProject(this, message: false);
        }

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Copy_Timing((TimingCopierVm) e.Argument, bgw, e);
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            // Remove logical focus to trigger LostFocus on any fields that didn't yet update the ViewModel
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(this), null);

            string filesToCopy = ((TimingCopierVm)DataContext).ExportPath;
            BackupManager.SaveMapBackup(filesToCopy.Split('|'));

            BackgroundWorker.RunWorkerAsync(DataContext);
            CanRun = false;
        }

        private string Copy_Timing(TimingCopierVm arg, BackgroundWorker worker, DoWorkEventArgs _) {
            string[] paths = arg.ExportPath.Split('|');
            int mapsDone = 0;

            var reader = EditorReaderStuff.GetFullEditorReaderOrNot();

            foreach (string exportPath in paths) {
                var editorTo = EditorReaderStuff.GetNewestVersionOrNot(exportPath, reader);
                var editorFrom = EditorReaderStuff.GetNewestVersionOrNot(arg.ImportPath, reader);

                Beatmap beatmapTo = editorTo.Beatmap;
                Beatmap beatmapFrom = editorFrom.Beatmap;

                Timing timingTo = beatmapTo.BeatmapTiming;
                Timing timingFrom = beatmapFrom.BeatmapTiming;

                // Get markers for hitobjects if mode 1 is used
                List<Marker> markers = new List<Marker>();
                if (arg.ResnapMode == "物件间的节拍数保持不变") {
                    markers = GetMarkers(beatmapTo, timingTo);
                }

                // Rid the beatmap of redlines
                // If a greenline exists at the same time as a redline then the redline ceizes to exist
                // Else convert the redline to a greenline: Inherited = false & MpB = -100
                List<TimingPoint> removeList = new List<TimingPoint>();
                foreach (TimingPoint redline in timingTo.Redlines) {
                    TimingPoint greenlineHere = timingTo.GetGreenlineAtTime(redline.Offset);

                    if (greenlineHere.Offset != redline.Offset) {
                        var newGreenline = redline.Copy();
                        newGreenline.Uninherited = false;
                        newGreenline.MpB = -100;

                        timingTo.Add(newGreenline);
                    }

                    removeList.Add(redline);
                }
                foreach (TimingPoint tp in removeList) {
                    timingTo.Remove(tp);
                }

                // Make new timing points changes
                List<TimingPointsChange> timingPointsChanges = new List<TimingPointsChange>();

                // Add redlines
                var redlines = timingFrom.Redlines;
                foreach (TimingPoint tp in redlines) {
                    timingPointsChanges.Add(new TimingPointsChange(tp, mpb: true, meter: true, unInherited: true, omitFirstBarLine: true, fuzzyness: Precision.DoubleEpsilon));
                }

                // Apply timing changes
                TimingPointsChange.ApplyChanges(timingTo, timingPointsChanges);

                if (arg.ResnapMode == "物件间的节拍数保持不变" && redlines.Count > 0) {
                    redlines = timingTo.Redlines;
                    List<double> newBookmarks = new List<double>();
                    double lastTime = redlines.FirstOrDefault().Offset;
                    foreach (Marker marker in markers) {
                        // Get redlines between this and last marker
                        TimingPoint redline = timingTo.GetRedlineAtTime(lastTime, redlines.FirstOrDefault());

                        double beatsFromLastTime = marker.BeatsFromLastMarker;
                        while (true) {
                            List<TimingPoint> redlinesBetween = redlines.Where(o => o.Offset <= lastTime + redline.MpB * beatsFromLastTime && o.Offset > lastTime).ToList();

                            if (redlinesBetween.Count == 0) break;

                            TimingPoint first = redlinesBetween.First();
                            double diff = first.Offset - lastTime;
                            beatsFromLastTime -= diff / redline.MpB;

                            redline = first;
                            lastTime = first.Offset;
                        }

                        // Last time is the time of the last redline in between
                        double newTime = lastTime + redline.MpB * beatsFromLastTime;
                        newTime = timingTo.Resnap(newTime, arg.BeatDivisors, firstTp: redlines.FirstOrDefault());
                        marker.Time = newTime;

                        lastTime = marker.Time;
                    }

                    // Add the bookmarks
                    foreach (Marker marker in markers)
                    {
                        // Check whether the marker is a bookmark
                        if (marker.Object is double) {
                            // Don't resnap bookmarks
                            newBookmarks.Add((double)marker.Object);
                        }
                    }
                    beatmapTo.SetBookmarks(newBookmarks);
                } else if (arg.ResnapMode == "仅重新对齐" && redlines.Count > 0) {
                    // Resnap hitobjects
                    foreach (HitObject ho in beatmapTo.HitObjects)
                    {
                        ho.ResnapSelf(timingTo, arg.BeatDivisors, firstTp: redlines.FirstOrDefault());
                        ho.ResnapEnd(timingTo, arg.BeatDivisors, firstTp: redlines.FirstOrDefault());
                    }

                    // Resnap greenlines
                    foreach (TimingPoint tp in timingTo.Greenlines)
                    {
                        tp.ResnapSelf(timingTo, arg.BeatDivisors, firstTp: redlines.FirstOrDefault());
                    }
                    timingTo.Sort();
                } else {
                    // Don't move objects
                }

                // Fix SV for if new redlines were added
                timingPointsChanges = new List<TimingPointsChange>();

                foreach (var ho in beatmapTo.HitObjects.Where(ho => ho.IsSlider)) {
                    TimingPoint tp = ho.TimingPoint.Copy();
                    tp.Offset = ho.Time;
                    tp.MpB = ho.SliderVelocity;
                    timingPointsChanges.Add(new TimingPointsChange(tp, mpb: true, fuzzyness: Precision.DoubleEpsilon));
                }

                // Apply timing changes
                TimingPointsChange.ApplyChanges(timingTo, timingPointsChanges);

                // Save the file
                editorTo.SaveFile();

                // Update progressbar
                if (worker != null && worker.WorkerReportsProgress) {
                    worker.ReportProgress(++mapsDone * 100 / paths.Length);
                }
            }

            // Make an accurate message
            string message = $"成功复制timing到 {mapsDone} 张{(mapsDone == 1 ? "谱面" : "谱面")}！";
            return message;
        }

        private List<Marker> GetMarkers(Beatmap beatmap, Timing timing) {
            List<Marker> markers = new List<Marker>();
            var redlines = timing.Redlines;

            foreach (HitObject ho in beatmap.HitObjects) {
                markers.Add(new Marker(ho));
            }
            foreach (double bookmark in beatmap.GetBookmarks()) {
                markers.Add(new Marker(bookmark));
            }
            foreach (TimingPoint greenline in timing.TimingPoints) {
                markers.Add(new Marker(greenline));
            }

            // Sort the markers
            markers = markers.OrderBy(o => o.Time).ToList();

            if (markers.Count == 0)
                return markers;

            // Calculate the beats between this marker and the last marker
            // If there is a redline in between then calculate beats from last marker to the redline and beats from redline to this marker
            // Time the same is 0
            double lastTime = redlines.First().Offset;
            foreach (Marker marker in markers) {
                // Get redlines between this and last marker
                List<TimingPoint> redlinesBetween = redlines.Where(o => o.Offset < marker.Time && o.Offset > lastTime).ToList();
                TimingPoint redline = timing.GetRedlineAtTime(lastTime);

                double beatsFromLastMarker = 0;
                foreach (TimingPoint redlineBetween in redlinesBetween) {
                    beatsFromLastMarker += (redlineBetween.Offset - lastTime) / redline.MpB;
                    redline = redlineBetween;
                    lastTime = redlineBetween.Offset;
                }
                beatsFromLastMarker += (marker.Time - lastTime) / redline.MpB;

                // Set the variable
                marker.BeatsFromLastMarker = beatsFromLastMarker;

                lastTime = marker.Time;
            }

            return markers;
        }

        private class Marker
        {
            public object Object { get; private set; }
            public double BeatsFromLastMarker { get; set; }
            public double Time { get => GetTime(); set => SetTime(value); }

            private double GetTime() {
                switch (Object) {
                    case double d:
                        return d;
                    case HitObject hitObject:
                        return hitObject.Time;
                    case TimingPoint point:
                        return point.Offset;
                    default:
                        return -1;
                }
            }

            private void SetTime(double value) {
                switch (Object) {
                    case double _:
                        Object = value;
                        break;
                    case HitObject hitObject:
                        hitObject.Time = value;
                        break;
                    case TimingPoint point:
                        point.Offset = value;
                        break;
                }
            }

            public Marker(object obj) {
                Object = obj;
                BeatsFromLastMarker = 0;
            }
        }

        public TimingCopierVm GetSaveData() {
            return (TimingCopierVm)DataContext;
        }

        public void SetSaveData(TimingCopierVm saveData) {
            DataContext = saveData;
        }
    }
}
