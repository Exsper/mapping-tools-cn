using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.SystemTools.QuickRun;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Components.TimeLine;
using Mapping_Tools.Viewmodels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Mapping_Tools.Classes.ToolHelpers;

namespace Mapping_Tools.Views.AutoFailDetector {
    [SmartQuickRunUsage(SmartQuickRunTargets.Always)]
    [VerticalContentScroll]
    public partial class AutoFailDetectorView : IQuickRun {
        private List<double> unloadingObjects;
        private List<double> potentialUnloadingObjects;
        private List<double> potentialDisruptors;
        private double endTimeMonitor;
        private TimeLine tl;

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler RunFinished;

        /// <summary>
        /// 
        /// </summary>
        public static readonly string ToolName = "自动失败检测";

        /// <summary>
        /// 
        /// </summary>
        public static readonly string ToolDescription = $"检测谱面中是否存在不正确的物件，导致osu!无法正确计算分数。{Environment.NewLine} 引起自动失败（Auto-fail）最常见的原因是在滑条中放置了其他击打物件，导致同一时间有多个击打物件，俗称“2B”图。{Environment.NewLine} 使用自定义AR和OD来检查当开启HardRock后会发生什么。";

        /// <summary>
        /// Initializes the Map Cleaner view to <see cref="MainWindow"/>
        /// </summary>
        public AutoFailDetectorView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.ContentViews.Width;
            Height = MainWindow.AppWindow.ContentViews.Height;
            DataContext = new AutoFailDetectorVm();

            // It's important to see the results
            Verbose = true;
        }

        public AutoFailDetectorVm ViewModel => (AutoFailDetectorVm) DataContext;

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

            //BackupManager.SaveMapBackup(paths);

            ViewModel.Paths = paths;
            ViewModel.Quick = quick;

            BackgroundWorker.RunWorkerAsync(ViewModel);
            CanRun = false;
        }

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Run_Program((AutoFailDetectorVm) e.Argument, bgw, e);
        }

        protected override void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if (e.Error == null) {
                FillTimeLine();
            }
            base.BackgroundWorker_RunWorkerCompleted(sender, e);
        }

        private string Run_Program(AutoFailDetectorVm args, BackgroundWorker worker, DoWorkEventArgs _) {
            var reader = EditorReaderStuff.GetFullEditorReaderOrNot();
            var editor = EditorReaderStuff.GetNewestVersionOrNot(args.Paths[0], reader);
            var beatmap = editor.Beatmap;

            // Get approach time and radius of the 50 score hit window
            var ar = args.ApproachRateOverride == -1
                ? editor.Beatmap.Difficulty["ApproachRate"].DoubleValue
                : args.ApproachRateOverride;
            var approachTime = (int) Beatmap.GetApproachTime(ar);

            var od = args.OverallDifficultyOverride == -1
                ? editor.Beatmap.Difficulty["OverallDifficulty"].DoubleValue
                : args.OverallDifficultyOverride;
            var window50 = (int) Math.Ceiling(200 - 10 * od);

            // Start time and end time
            var mapStartTime = (int) beatmap.GetMapStartTime();
            var mapEndTime = (int) beatmap.GetMapEndTime();
            var autoFailTime = (int) beatmap.GetAutoFailCheckTime();

            // Detect auto-fail
            var autoFailDetector = new Classes.Tools.AutoFailDetector(beatmap.HitObjects,
                mapStartTime, mapEndTime, autoFailTime,
                approachTime, window50, args.PhysicsUpdateLeniency);

            var autoFail = autoFailDetector.DetectAutoFail();

            if (worker != null && worker.WorkerReportsProgress) worker.ReportProgress(33);

            // Fix auto-fail
            if (args.GetAutoFailFix) {
                var placedFix = autoFailDetector.AutoFailFixDialogue(args.AutoPlaceFix);

                if (placedFix) {
                    editor.SaveFile();
                }
            }

            if (worker != null && worker.WorkerReportsProgress) worker.ReportProgress(67);

            // Set the timeline lists
            unloadingObjects = args.ShowUnloadingObjects ? autoFailDetector.UnloadingObjects : new List<double>();
            potentialUnloadingObjects = args.ShowPotentialUnloadingObjects ? autoFailDetector.PotentialUnloadingObjects : new List<double>();
            potentialDisruptors = args.ShowPotentialDisruptors ? autoFailDetector.Disruptors : new List<double>();

            // Set end time for the timeline
            endTimeMonitor = mapEndTime;

            // Complete progressbar
            if (worker != null && worker.WorkerReportsProgress) worker.ReportProgress(100);

            // Do stuff
            RunFinished?.Invoke(this, new RunToolCompletedEventArgs(true, false, args.Quick));

            return autoFail ? $"检测到 {autoFailDetector.UnloadingObjects.Count} 个卸载物件和 {autoFailDetector.PotentialUnloadingObjects.Count} 个潜在卸载物件！" :
                autoFailDetector.PotentialUnloadingObjects.Count > 0 ? $"未检测到自动失败（Auto-fail），但存在 {autoFailDetector.PotentialUnloadingObjects.Count} 个潜在卸载物件。" : 
                "未检测到自动失败（Auto-fail）。";
        }


        private void FillTimeLine() {
            tl?.MainCanvas.Children.Clear();
            try {
                tl = new TimeLine(MainWindow.AppWindow.MainContentGrid.ActualWidth, 100.0, endTimeMonitor);
                foreach (double timingS in potentialUnloadingObjects) {
                    tl.AddElement(timingS, 1);
                }
                foreach (double timingS in potentialDisruptors) {
                    tl.AddElement(timingS, 4);
                }
                foreach (double timingS in unloadingObjects) {
                    tl.AddElement(timingS, 3);
                }
                TlHost.Children.Clear();
                TlHost.Children.Add(tl);
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

    }
}
