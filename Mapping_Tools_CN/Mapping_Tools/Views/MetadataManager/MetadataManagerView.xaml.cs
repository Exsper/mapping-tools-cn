using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.ToolHelpers;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Viewmodels;

namespace Mapping_Tools.Views.MetadataManager {
    /// <summary>
    /// Interactielogica voor MetadataManagerView.xaml
    /// </summary>
    public partial class MetadataManagerView : ISavable<MetadataManagerVm> {
        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "metadataproject.json");

        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Metadata Manager Projects");

        public static readonly string ToolName = "元数据管理器";

        public static readonly string ToolDescription = $@"为需要编辑每个难度的元数据节省时间，可以在本工具中编辑元数据，之后随时复制到多个难度。{Environment.NewLine}您也可以从谱面A中导入元数据，然后复制给谱面B。{Environment.NewLine}利用保存和加载项目设置，可以轻松处理多个谱面集。";

        public MetadataManagerView() {
            InitializeComponent();
            DataContext = new MetadataManagerVm();
            Width = MainWindow.AppWindow.ContentViews.Width;
            Height = MainWindow.AppWindow.ContentViews.Height;
            ProjectManager.LoadProject(this, message: false);
        }

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Copy_Metadata((MetadataManagerVm) e.Argument, bgw, e);
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            // Remove logical focus to trigger LostFocus on any fields that didn't yet update the ViewModel
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(this), null);

            var filesToCopy = ((MetadataManagerVm)DataContext).ExportPath.Split('|');
            foreach (var fileToCopy in filesToCopy) {
                BackupManager.SaveMapBackup(fileToCopy);
            }

            BackgroundWorker.RunWorkerAsync((MetadataManagerVm)DataContext);
            CanRun = false;
        }


        private static string Copy_Metadata(MetadataManagerVm arg, BackgroundWorker worker, DoWorkEventArgs _) {
            var paths = arg.ExportPath.Split('|');
            var mapsDone = 0;

            var reader = EditorReaderStuff.GetFullEditorReaderOrNot();

            foreach (var path in paths) {
                var editor = EditorReaderStuff.GetNewestVersionOrNot(path, reader);
                var beatmap = editor.Beatmap;

                beatmap.Metadata["ArtistUnicode"].Value = arg.Artist;
                beatmap.Metadata["Artist"].Value = arg.RomanisedArtist;
                beatmap.Metadata["TitleUnicode"].Value = arg.Title;
                beatmap.Metadata["Title"].Value = arg.RomanisedTitle;
                beatmap.Metadata["Creator"].Value = arg.BeatmapCreator;
                beatmap.Metadata["Source"].Value = arg.Source;
                beatmap.Metadata["Tags"].Value = arg.Tags;

                beatmap.General["PreviewTime"] = new TValue(arg.PreviewTime.ToRoundInvariant());
                if (arg.UseComboColours) {
                    beatmap.ComboColours = new List<ComboColour>(arg.ComboColours);
                    beatmap.SpecialColours.Clear();
                    foreach (var specialColour in arg.SpecialColours) {
                        beatmap.SpecialColours.Add(specialColour.Name, specialColour);
                    }
                }

                if (arg.ResetIds) {
                    beatmap.Metadata["BeatmapID"].Value = @"0";
                    beatmap.Metadata["BeatmapSetID"].Value = @"-1";
                }

                // Save the file with name update because we updated the metadata
                editor.SaveFileWithNameUpdate();

                // Update progressbar
                if (worker != null && worker.WorkerReportsProgress) {
                    worker.ReportProgress(++mapsDone * 100 / paths.Length);
                }
            }

            // Make an accurate message
            var message = $"成功导出元数据到 {mapsDone} 张{(mapsDone == 1 ? "谱面" : "谱面")}！";
            return message;
        }

        public MetadataManagerVm GetSaveData() {
            return (MetadataManagerVm)DataContext;
        }

        public void SetSaveData(MetadataManagerVm saveData) {
            DataContext = saveData;
        }
    }
}
