using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.SystemTools.QuickRun;
using Mapping_Tools.Classes.ToolHelpers;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Viewmodels;

namespace Mapping_Tools.Views.HitsoundPreviewHelper
{
    /// <summary>
    /// Interactielogica voor HitsoundCopierView.xaml
    /// </summary>
    [SmartQuickRunUsage(SmartQuickRunTargets.Always)]
    public partial class HitsoundPreviewHelperView : ISavable<HitsoundPreviewHelperVm>, IQuickRun
    {
        public event EventHandler RunFinished;

        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "hspreviewproject.json");

        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Hitsound Preview Projects");

        public static readonly string ToolName = "音效预览助手";
        public static readonly string ToolDescription =
            $@"本工具可以给当前谱面中的物件按物件的位置下音效。" +
            $@"这样您在下音效时就不用手动分配物件然后导入到音效工作室了。" +
            $@"{Environment.NewLine}本工具专为特定的下音效方式服务，" +
            $@"把物件按音效划分在不同的屏幕位置以对应不同的音效层。" +
            $@"比如使用mania谱面并把每个轨道作为一个单独的音效。";

        /// <summary>
        /// 
        /// </summary>
        public HitsoundPreviewHelperView()
        {
            InitializeComponent();
            DataContext = new HitsoundPreviewHelperVm();
            Width = MainWindow.AppWindow.ContentViews.Width;
            Height = MainWindow.AppWindow.ContentViews.Height;
            ProjectManager.LoadProject(this, message: false);
        }

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var bgw = sender as BackgroundWorker;
            e.Result = PlaceHitsounds((Arguments) e.Argument, bgw, e);
        }

        private struct Arguments
        {
            public string[] Paths;
            public bool Quick;
            public List<HitsoundZone> Zones;

            public Arguments(string[] paths, bool quick, List<HitsoundZone> zones)
            {
                Paths = paths;
                Quick = quick;
                Zones = zones;
            }
        }

        private string PlaceHitsounds(Arguments args, BackgroundWorker worker, DoWorkEventArgs _)
        {
            if (args.Zones.Count == 0)
                return "当前没有任何区域！";

            var reader = EditorReaderStuff.GetFullEditorReaderOrNot();

            foreach (string path in args.Paths)
            {
                var editor = EditorReaderStuff.GetNewestVersionOrNot(path, reader);
                Beatmap beatmap = editor.Beatmap;
                Timeline timeline = beatmap.GetTimeline();

                for (int i = 0; i < timeline.TimelineObjects.Count; i++)
                {
                    var tlo = timeline.TimelineObjects[i];

                    var column = args.Zones.FirstOrDefault();
                    double best = double.MaxValue;
                    foreach (var c in args.Zones)
                    {
                        double dist = c.Distance(tlo.Origin.Pos);
                        if (dist < best)
                        {
                            best = dist;
                            column = c;
                        }
                    }

                    if (column == null) continue;

                    tlo.Filename = column.Filename;
                    tlo.SampleSet = column.SampleSet;
                    tlo.AdditionSet = column.AdditionsSet;
                    tlo.CustomIndex = column.CustomIndex;
                    tlo.SampleVolume = 0;
                    tlo.SetHitsound(column.Hitsound);
                    tlo.HitsoundsToOrigin();

                    UpdateProgressBar(worker, (int) (100f * i / beatmap.HitObjects.Count));
                }

                // Save the file
                editor.SaveFile();
            }

            // Do stuff
            RunFinished?.Invoke(this, new RunToolCompletedEventArgs(true, reader != null, args.Quick));

            return args.Quick ? "" : "完成！";
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            RunTool(MainWindow.AppWindow.GetCurrentMaps(), quick: false);
        }

        public void QuickRun()
        {
            RunTool(new[] {IOHelper.GetCurrentBeatmapOrCurrentBeatmap()}, quick: true);
        }


        private void RunTool(string[] paths, bool quick = false)
        {
            if (!CanRun) return;

            // Remove logical focus to trigger LostFocus on any fields that didn't yet update the ViewModel
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(this), null);

            BackupManager.SaveMapBackup(paths);

            BackgroundWorker.RunWorkerAsync(new Arguments(paths, quick,
                ((HitsoundPreviewHelperVm) DataContext).Items.ToList()));

            CanRun = false;
        }

        public HitsoundPreviewHelperVm GetSaveData()
        {
            return (HitsoundPreviewHelperVm) DataContext;
        }

        public void SetSaveData(HitsoundPreviewHelperVm saveData)
        {
            DataContext = saveData;

        }

    }
}
