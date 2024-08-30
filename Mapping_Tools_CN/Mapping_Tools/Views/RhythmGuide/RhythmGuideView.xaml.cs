using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Viewmodels;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace Mapping_Tools.Views.RhythmGuide {

    /// <summary>
    /// Interactielogica voor RhythmGuideView.xaml
    /// </summary>
    [VerticalContentScroll]
    public partial class RhythmGuideView : ISavable<RhythmGuideVm> {
        public static readonly string ToolName = "节奏向导";

        public static readonly string ToolDescription =
            $@"根据多张谱面的节奏制作包含圆圈的谱面，为下音效作参考。" +
            $@"{Environment.NewLine}您可以添加这些圆圈到已有谱面或制作新的谱面。" +
            $@"{Environment.NewLine}使用文件浏览器选择谱面时可以同时选择多个谱面。";

        /// <summary>
        /// 
        /// </summary>
        public RhythmGuideView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.ContentViews.Width;
            Height = MainWindow.AppWindow.ContentViews.Height;
            DataContext = new RhythmGuideVm();
            ProjectManager.LoadProject(this, message: false);
        }

        public RhythmGuideVm ViewModel => (RhythmGuideVm) DataContext;

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = GenerateRhythmGuide((Classes.Tools.RhythmGuide.RhythmGuideGeneratorArgs) e.Argument, bgw, e);
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            // Remove logical focus to trigger LostFocus on any fields that didn't yet update the ViewModel
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(this), null);

            foreach (var fileToCopy in ViewModel.GuideGeneratorArgs.Paths) {
                BackupManager.SaveMapBackup(fileToCopy);
            }

            BackgroundWorker.RunWorkerAsync(ViewModel.GuideGeneratorArgs);
            CanRun = false;
        }

        private static string GenerateRhythmGuide(Classes.Tools.RhythmGuide.RhythmGuideGeneratorArgs args, BackgroundWorker worker, DoWorkEventArgs _) {
            Classes.Tools.RhythmGuide.GenerateRhythmGuide(args);

            // Complete progress bar
            if (worker != null && worker.WorkerReportsProgress) {
                worker.ReportProgress(100);
            }
            return args.ExportMode == Classes.Tools.RhythmGuide.ExportMode.完全覆盖导出谱面 ? "" : "完成！";
        }

        public RhythmGuideVm GetSaveData() {
            return ViewModel;
        }

        public void SetSaveData(RhythmGuideVm saveData) {
            DataContext = saveData;
        }

        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "rhythmguideproject.json");

        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Rhythm Guide Projects");
    }
}