﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.SystemTools.QuickRun;
using Mapping_Tools.Classes.ToolHelpers;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Classes.Tools.MapCleanerStuff;
using Mapping_Tools.Components.TimeLine;
using Mapping_Tools.Viewmodels;

namespace Mapping_Tools.Views.MapCleaner {
    [SmartQuickRunUsage(SmartQuickRunTargets.Always)]
    [VerticalContentScroll]
    [HorizontalContentScroll]
    public partial class CleanerView : IQuickRun, ISavable<MapCleanerVm> {
        private List<double> timingpointsRemoved;
        private List<double> timingpointsAdded;
        private List<double> timingpointsChanged;
        private double endTimeMonitor;
        private TimeLine tl;

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler RunFinished;

        /// <summary>
        /// 
        /// </summary>
        public static readonly string ToolName = "谱面清除器";

        /// <summary>
        /// 
        /// </summary>
        public static readonly string ToolDescription = $@"该工具可以清理谱面的无用绿线和一些针对整张谱面的其他功能。{Environment.NewLine}谱面清洁器通过分析所有时间轴的影响，然后移除原有时间轴并以更好的方式重建，来清理无用绿线。在此过程中绿线会自动对齐到使用它们的物件上。";

        /// <summary>
        /// Initializes the Map Cleaner view to <see cref="MainWindow"/>
        /// </summary>
        public CleanerView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.ContentViews.Width;
            Height = MainWindow.AppWindow.ContentViews.Height;
            DataContext = new MapCleanerVm();
            ProjectManager.LoadProject(this, message: false);

            // It's important to see the results of map cleaner
            Verbose = true;
        }

        public MapCleanerVm ViewModel => (MapCleanerVm) DataContext;

        private void Start_Click(object sender, RoutedEventArgs e) {
            RunTool(MainWindow.AppWindow.GetCurrentMaps(), quick: false);
        }

        /// <summary>
        /// 
        /// </summary>
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

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Run_Program((MapCleanerVm) e.Argument, bgw, e);
        }

        protected override void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if (e.Error == null) {
                FillTimeLine();
            }
            base.BackgroundWorker_RunWorkerCompleted(sender, e);
        }

        private string Run_Program(MapCleanerVm args, BackgroundWorker worker, DoWorkEventArgs _) {
            var result = new MapCleanerResult();

            var reader = EditorReaderStuff.GetFullEditorReaderOrNot();

            if (args.Paths.Length == 1) {
                var editor = EditorReaderStuff.GetNewestVersionOrNot(args.Paths[0], reader);

                List<TimingPoint> orgininalTimingPoints = editor.Beatmap.BeatmapTiming.TimingPoints.Select(tp => tp.Copy()).ToList();
                int oldTimingPointsCount = editor.Beatmap.BeatmapTiming.TimingPoints.Count;

                result.Add(Classes.Tools.MapCleanerStuff.MapCleaner.CleanMap(editor, args.MapCleanerArgs, worker));

                // Update result with removed count
                int removed = oldTimingPointsCount - editor.Beatmap.BeatmapTiming.TimingPoints.Count;
                result.TimingPointsRemoved += removed;

                var newTimingPoints = editor.Beatmap.BeatmapTiming.TimingPoints;
                Monitor_Differences(orgininalTimingPoints, newTimingPoints);

                // Save the file
                editor.SaveFile();
            } else {
                foreach (string path in args.Paths) {
                    var editor = EditorReaderStuff.GetNewestVersionOrNot(path, reader);

                    int oldTimingPointsCount = editor.Beatmap.BeatmapTiming.TimingPoints.Count;

                    result.Add(Classes.Tools.MapCleanerStuff.MapCleaner.CleanMap(editor, args.MapCleanerArgs, worker));

                    // Update result with removed count
                    int removed = oldTimingPointsCount - editor.Beatmap.BeatmapTiming.TimingPoints.Count;
                    result.TimingPointsRemoved += removed;

                    // Save the file
                    editor.SaveFile();
                }
            }

            // Do stuff
            RunFinished?.Invoke(this, new RunToolCompletedEventArgs(true, reader != null, args.Quick));

            // Make an accurate message
            string message = $"成功{(result.TimingPointsRemoved < 0 ? "添加" : "移除")}了 {Math.Abs(result.TimingPointsRemoved)} 条{(Math.Abs(result.TimingPointsRemoved) == 1 ? "绿线" : "绿线")}" +
                (args.MapCleanerArgs.ResnapObjects ? $"并重新对齐了 {result.ObjectsResnapped} 个{(result.ObjectsResnapped == 1 ? "物件" : "物件")}" : "") + 
                (args.MapCleanerArgs.RemoveUnusedSamples ? $"并移除了 {result.SamplesRemoved} 个未使用的{(result.SamplesRemoved == 1 ? "音效组" : "音效组")}" : "") + "！";
            return args.Quick ? string.Empty : message;
        }

        private void Monitor_Differences(IReadOnlyList<TimingPoint> originalTimingPoints, IReadOnlyList<TimingPoint> newTimingPoints) {
            // Take note of all the changes
            timingpointsChanged = new List<double>();

            var originalInNew = (from first in originalTimingPoints
                                 join second in newTimingPoints
                                 on first.Offset equals second.Offset
                                 select first).ToList();

            var newInOriginal = (from first in originalTimingPoints
                                 join second in newTimingPoints
                                 on first.Offset equals second.Offset
                                 select second).ToList();
            
            foreach (TimingPoint tp in originalInNew) {
                bool different = true;
                List<TimingPoint> newTPs = newInOriginal.Where(o => Math.Abs(o.Offset - tp.Offset) < Precision.DoubleEpsilon).ToList();
                if (newTPs.Count == 0) { different = false; }
                foreach (TimingPoint newTp in newTPs) {
                    if (tp.Equals(newTp)) { different = false; }
                }
                if (different) { timingpointsChanged.Add(tp.Offset); }
            }

            List<double> originalOffsets = new List<double>();
            List<double> newOffsets = new List<double>();
            foreach (var originalTimingPoint in originalTimingPoints) {
                originalOffsets.Add(originalTimingPoint.Offset);
            }
            foreach (var newTimingPoint in newTimingPoints) {
                newOffsets.Add(newTimingPoint.Offset);
            }

            timingpointsRemoved = originalOffsets.Except(newOffsets).ToList();
            timingpointsAdded = newOffsets.Except(originalOffsets).ToList();
            double endTimeOriginal = originalTimingPoints.Count > 0 ? originalTimingPoints.Last().Offset : 0;
            double endTimeNew = newTimingPoints.Count > 0 ? newTimingPoints.Last().Offset : 0;
            endTimeMonitor = Math.Max(endTimeOriginal, endTimeNew);
        }

        private void FillTimeLine() {
            tl?.MainCanvas.Children.Clear();
            try {
                tl = new TimeLine(MainWindow.AppWindow.MainContentGrid.ActualWidth, 100.0, endTimeMonitor);
                foreach (double timingS in timingpointsAdded) {
                    tl.AddElement(timingS, 1);
                }
                foreach (double timingS in timingpointsChanged) {
                    tl.AddElement(timingS, 2);
                }
                foreach (double timingS in timingpointsRemoved) {
                    tl.AddElement(timingS, 3);
                }
                TlHost.Children.Clear();
                TlHost.Children.Add(tl);
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

        public MapCleanerVm GetSaveData() {
            return ViewModel;
        }

        public void SetSaveData(MapCleanerVm saveData) {
            DataContext = saveData;
        }

        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "mapcleanerproject.json");

        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Map Cleaner Projects");
    }
}
