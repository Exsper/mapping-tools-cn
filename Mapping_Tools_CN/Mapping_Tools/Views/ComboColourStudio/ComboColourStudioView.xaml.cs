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
using Mapping_Tools.Classes.Tools.ComboColourStudio;
using Mapping_Tools.Components.Dialogs;
using Mapping_Tools.Viewmodels;
using MaterialDesignThemes.Wpf;

namespace Mapping_Tools.Views.ComboColourStudio {
    /// <summary>
    /// Interactielogica voor ComboColourStudioView.xaml
    /// </summary>
    public partial class ComboColourStudioView : ISavable<ComboColourProject> {
        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "combocolourproject.json");

        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Combo Colour Studio Projects");

        public static readonly string ToolName = "Combo色彩工作室";

        public static readonly string ToolDescription = $@"本工具可以轻松定制谱面combo颜色，也叫做colour haxing。{Environment.NewLine}定义Combo颜色区间很像在osu!编辑器里设置时间轴，只需要添加一个新的颜色轴然后设置combo颜色次序即可。{Environment.NewLine}您也可以定义爆发（Burst）颜色轴，只用于单combo，适合于用颜色强调特定的Pattern。{Environment.NewLine}要开始，点击左下角加号添加combo颜色或者从已有谱面中导入颜色。combo颜色可以通过点击修改。{Environment.NewLine}点击右下角的加号添加颜色轴。要编辑颜色次序可以双击对应单元格。";

        private ComboColourStudioVm ViewModel => (ComboColourStudioVm) DataContext;

        public ComboColourStudioView() {
            InitializeComponent();
            DataContext = new ComboColourStudioVm();
            Width = MainWindow.AppWindow.ContentViews.Width;
            Height = MainWindow.AppWindow.ContentViews.Height;
            ProjectManager.LoadProject(this, message: false);
        }

        private async void ImportColoursButton_OnClick(object sender, RoutedEventArgs e) {
            var sampleDialog = new BeatmapImportDialog();

            var result = await DialogHost.Show(sampleDialog, "RootDialog");

            if ((bool) result) {
                ViewModel.Project.ImportComboColoursFromBeatmap(sampleDialog.Path);
            }
        }

        private async void ImportColourHaxButton_OnClick(object sender, RoutedEventArgs e) {
            var sampleDialog = new BeatmapImportDialog();

            var result = await DialogHost.Show(sampleDialog, "RootDialog");

            if ((bool) result) {
                ViewModel.Project.ImportColourHaxFromBeatmap(sampleDialog.Path);
            }
        }

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Export_ComboColours((ComboColourStudioVm) e.Argument, bgw, e);
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            // Remove logical focus to trigger LostFocus on any fields that didn't yet update the ViewModel
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(this), null);

            ViewModel.ExportPath = MainWindow.AppWindow.GetCurrentMapsString();

            var filesToCopy = ViewModel.ExportPath.Split('|');
            foreach (var fileToCopy in filesToCopy) {
                BackupManager.SaveMapBackup(fileToCopy);
            }

            BackgroundWorker.RunWorkerAsync(ViewModel);
            CanRun = false;
        }


        private static string Export_ComboColours(ComboColourStudioVm arg, BackgroundWorker worker, DoWorkEventArgs _) {
            var paths = arg.ExportPath.Split('|');
            var mapsDone = 0;

            var orderedColourPoints = arg.Project.ColourPoints.OrderBy(o => o.Time).ToList();
            var orderedComboColours = arg.Project.ComboColours.OrderBy(o => o.Name).ToList();

            var reader = EditorReaderStuff.GetFullEditorReaderOrNot();

            foreach (var path in paths) {
                var editor = EditorReaderStuff.GetNewestVersionOrNot(path, reader);
                var beatmap = editor.Beatmap;

                // Setting the combo colours
                beatmap.ComboColours = new List<ComboColour>(arg.Project.ComboColours);

                // Setting the combo skips
                if (beatmap.HitObjects.Count > 0 && orderedColourPoints.Count > 0) {
                    int lastColourPointColourIndex = -1;
                    var lastColourPoint = orderedColourPoints[0];
                    int lastColourIndex = 0;
                    var exceptions = new List<ColourPoint>();
                    foreach (var newCombo in beatmap.HitObjects.Where(o => o.ActualNewCombo && !o.IsSpinner)) {
                        int comboLength = GetComboLength(newCombo, beatmap.HitObjects);
                        //Console.WriteLine(comboLength);

                        // Get the colour point for this new combo
                        var colourPoint = GetColourPoint(orderedColourPoints, newCombo.Time, exceptions, comboLength <= arg.Project.MaxBurstLength);
                        var colourSequence = colourPoint.ColourSequence.ToList();

                        // Add the colour point to the exceptions so it doesnt get used again
                        if (colourPoint.Mode == ColourPointMode.爆发) {
                            exceptions.Add(colourPoint);
                        }

                        // Get the last colour index on the sequence of this colour point
                        lastColourPointColourIndex = lastColourPointColourIndex == -1 || lastColourPoint.Equals(colourPoint) ? 
                            lastColourPointColourIndex : 
                            colourSequence.FindIndex(o => o.Name == orderedComboColours[lastColourIndex].Name);

                        // Get the next colour index on this colour point
                        // Check if colourSequence count is 0 to prevent division by 0
                        var colourPointColourIndex = lastColourPointColourIndex == -1 || colourSequence.Count == 0
                            ? 0
                            : lastColourPoint.Equals(colourPoint) ? 
                            MathHelper.Mod(lastColourPointColourIndex + 1, colourSequence.Count) :
                            // If the colour point changed try going back to index 0
                            lastColourPointColourIndex == 0 && colourSequence.Count > 1 ? 1 : 0;

                        //Console.WriteLine("colourPointColourIndex: " + colourPointColourIndex);
                        //Console.WriteLine("colourPointColour: " + colourPoint.ColourSequence[colourPointColourIndex].Name);

                        // Find the combo index of the chosen colour in the sequence
                        // Check if the colourSequence count is 0 to prevent an out-of-range exception
                        var colourIndex = colourSequence.Count == 0 ? MathHelper.Mod(lastColourIndex + 1, orderedComboColours.Count) :
                            orderedComboColours.FindIndex(o => o.Name == colourSequence[colourPointColourIndex].Name);

                        if (colourIndex == -1) {
                            throw new ArgumentException($"无法使用颜色轴内的颜色 {colourSequence[colourPointColourIndex].Name} 在offset时间 {colourPoint.Time} 因为该颜色不在combo颜色之中。");
                        }

                        //Console.WriteLine("colourIndex: " + colourIndex);

                        var comboIncrease = MathHelper.Mod(colourIndex - lastColourIndex, arg.Project.ComboColours.Count);

                        // Do -1 combo skip since it always does +1 combo colour for each new combo which is not on a spinner
                        newCombo.ComboSkip = MathHelper.Mod(comboIncrease - 1, arg.Project.ComboColours.Count);

                        // Set new combo to true for the case this is the first object and new combo is false
                        if (!newCombo.NewCombo && newCombo.ComboSkip != 0) {
                            newCombo.NewCombo = true;
                        }

                        //Console.WriteLine("comboSkip: " + newCombo.ComboSkip);

                        lastColourPointColourIndex = colourPointColourIndex;
                        lastColourPoint = colourPoint;
                        lastColourIndex = colourIndex;
                    }
                }

                // Save the file
                editor.SaveFile();

                // Update progressbar
                if (worker != null && worker.WorkerReportsProgress) {
                    worker.ReportProgress(++mapsDone * 100 / paths.Length);
                }
            }

            // Make an accurate message
            var message = $"成功导出颜色到 {mapsDone} 个{(mapsDone == 1 ? "谱面" : "谱面")}!";
            return message;
        }

        private static int GetComboLength(HitObject newCombo, List<HitObject> hitObjects) {
            int count = 1;
            var index = hitObjects.IndexOf(newCombo);

            if (index == -1) {
                return 0;
            }

            while (++index < hitObjects.Count) {
                var hitObject = hitObjects[index];
                if (hitObject.NewCombo) {
                    return count;
                }

                count++;
            }

            return count;
        }

        private static ColourPoint GetColourPoint(IReadOnlyList<ColourPoint> colourPoints, double time, IReadOnlyCollection<ColourPoint> exceptions, bool includeBurst) {
            return colourPoints.Except(exceptions).LastOrDefault(o => o.Time <= time + 5 && (o.Mode != ColourPointMode.爆发 || o.Time >= time - 5 && includeBurst)) ?? 
                                                                      colourPoints.Except(exceptions).FirstOrDefault(o => o.Mode != ColourPointMode.爆发) ?? 
                                                                      colourPoints[0];
        }

        public ComboColourProject GetSaveData() {
            return ViewModel.Project;
        }

        public void SetSaveData(ComboColourProject saveData) {
            ViewModel.Project = saveData;
        }
    }
}
