using Editor_Reader;
using Mapping_Tools.Classes;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.SystemTools.QuickRun;
using Mapping_Tools.Classes.ToolHelpers;
using Mapping_Tools.Classes.Tools.PatternGallery;
using Mapping_Tools.Components.Dialogs.CustomDialog;
using Mapping_Tools.Viewmodels;
using MaterialDesignThemes.Wpf;
using System;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Mapping_Tools.Views.PatternGallery {
    /// <summary>
    /// Interactielogica voor PatternGalleryView.xaml
    /// </summary>
    [SmartQuickRunUsage(SmartQuickRunTargets.Always)]
    //[HiddenTool]
    public partial class PatternGalleryView : ISavable<PatternGalleryVm>, IHasExtraAutoSaveTarget, IQuickRun, IHaveExtraProjectMenuItems {
        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "patterngalleryproject.json");

        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Pattern Gallery Projects");

        public string ExtraAutoSavePath => Path.Combine(ViewModel.FileHandler.GetCollectionFolderPath(), "project.json");

        public static readonly string ToolName = "Pattern 展览馆";
        public static readonly string ToolDescription =
            $@"导入导出谱面的Pattern，制作成Pattern收藏夹并与其他人分享。"+Environment.NewLine+ 
            @"使用底部按钮添加或删除Pattern。"+Environment.NewLine+ 
            @"选择一个或多个Pattern并点击运行按钮，或者直接双击Pattern，来导出Pattern到当前谱面。"+Environment.NewLine+ 
            @"界面右边的选项可以定制导出规则。"+Environment.NewLine+
            @"在“项目”菜单中进行保存/读取/重命名/导入/导出Pattern收藏夹。";

        /// <summary>
        /// 
        /// </summary>
        public PatternGalleryView()
        {
            InitializeComponent();
            DataContext = new PatternGalleryVm();
            Width = MainWindow.AppWindow.ContentViews.Width;
            Height = MainWindow.AppWindow.ContentViews.Height;
            ProjectManager.LoadProject(this, message: false);
            InitializeOsuPatternFileHandler();
        }

        public PatternGalleryVm ViewModel => (PatternGalleryVm)DataContext;

        public event EventHandler RunFinished;

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var bgw = sender as BackgroundWorker;
            e.Result = ExportPattern((PatternGalleryVm) e.Argument, bgw, e);
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            RunTool(ViewModel.ExportTimeMode == ExportTimeMode.当前时间
                ? new[] { IOHelper.GetCurrentBeatmapOrCurrentBeatmap() }
                : MainWindow.AppWindow.GetCurrentMaps(), quick: false);
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

            ViewModel.Paths = paths;
            ViewModel.Quick = quick;

            BackgroundWorker.RunWorkerAsync(DataContext);

            CanRun = false;
        }

        private string ExportPattern(PatternGalleryVm args, BackgroundWorker worker, DoWorkEventArgs _) {
            EditorReader reader;
            double exportTime = 0;
            bool usePatternOffset = false;
            switch (args.ExportTimeMode) {
                case ExportTimeMode.当前时间:
                    try {
                        reader = EditorReaderStuff.GetFullEditorReader();
                        exportTime = reader.EditorTime();
                    }
                    catch (Exception e) {
                        throw new Exception("无法获取当前编辑器时间。", e);
                    }
                    break;
                case ExportTimeMode.Pattern时间:
                    reader = EditorReaderStuff.GetFullEditorReaderOrNot();
                    usePatternOffset = true;
                    break;
                case ExportTimeMode.自定义时间:
                    reader = EditorReaderStuff.GetFullEditorReaderOrNot();
                    exportTime = args.CustomExportTime;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ExportTimeMode), "遇到无效值");
            }
           
            var editor = EditorReaderStuff.GetNewestVersionOrNot(args.Paths[0], reader);

            var patternCount = args.Patterns.Count(o => o.IsSelected);

            if (patternCount == 0)
                throw new Exception("未选择需要导出的Pattern。");

            var patternPlacer = args.OsuPatternPlacer;
            foreach (var pattern in args.Patterns.Where(o => o.IsSelected)) {
                var patternBeatmap = args.FileHandler.GetPatternBeatmap(pattern.FileName);

                if (usePatternOffset) {
                    patternPlacer.PlaceOsuPattern(patternBeatmap, editor.Beatmap, protectBeatmapPattern:false);
                } else {
                    patternPlacer.PlaceOsuPatternAtTime(patternBeatmap, editor.Beatmap, exportTime, false);
                }

                // Increase pattern use count and time
                pattern.UseCount++;
                pattern.LastUsedTime = DateTime.Now;
            }

            editor.SaveFile();

            // Complete progressbar
            if (worker != null && worker.WorkerReportsProgress) worker.ReportProgress(100);

            // Do stuff
            RunFinished?.Invoke(this, new RunToolCompletedEventArgs(true, reader != null, args.Quick));

            return "成功导出Pattern！";
        }

        private void InitializeOsuPatternFileHandler() {
            // Make sure the file handler always uses the right pattern files folder
            if (ViewModel.FileHandler != null) {
                ViewModel.FileHandler.BasePath = DefaultSaveFolder;
                ViewModel.FileHandler.EnsureCollectionFolderExists();
            }
        }

        public PatternGalleryVm GetSaveData()
        {
            return (PatternGalleryVm) DataContext;
        }

        public void SetSaveData(PatternGalleryVm saveData)
        {
            // Save the current project to its collection folder if it has patterns
            if (ViewModel.Patterns.Count > 0) {
                ProjectManager.SaveProject(this, ExtraAutoSavePath);
            }

            DataContext = saveData;
            InitializeOsuPatternFileHandler();
        }

        public MenuItem[] GetMenuItems() {
            var renameMenu = new MenuItem {
                Header = "重命名收藏夹（_R）", Icon = new PackIcon { Kind = PackIconKind.Rename },
                ToolTip = "重命名该收藏夹和Pattern文件夹中该收藏夹的文件夹名称。。"
            };
            renameMenu.Click += DoRenameCollection;

            var importMenu = new MenuItem {
                Header = "导入收藏夹（_I）", Icon = new PackIcon { Kind = PackIconKind.Import },
                ToolTip = "将收藏夹zip文件导入到项目文件夹。"
            };
            importMenu.Click += DoImportCollection;

            var exportMenu = new MenuItem {
                Header = "导出收藏夹（_E）", Icon = new PackIcon { Kind = PackIconKind.Export },
                ToolTip = "导出该收藏夹到导出文件夹，导出的文件可以再使用导入菜单进行导入。"
            };
            exportMenu.Click += DoExportCollection;

            return new[] { renameMenu, importMenu, exportMenu };
        }

        private async void DoRenameCollection(object sender, RoutedEventArgs e) {
            try {
                var viewModel = new CollectionRenameVm {
                    NewName = ViewModel.CollectionName, 
                    NewFolderName = ViewModel.FileHandler.CollectionFolderName
                };

                var dialog = new CustomDialog(viewModel, 0);
                var result = await DialogHost.Show(dialog, "RootDialog");

                if (!(bool)result) return;

                ViewModel.CollectionName = viewModel.NewName;
                ViewModel.FileHandler.RenameCollectionFolder(viewModel.NewFolderName);

                await Task.Factory.StartNew(() => MainWindow.MessageQueue.Enqueue("重命名收藏夹成功！"));
            } catch (ArgumentException) { } catch (Exception ex) {
                ex.Show();
            }
        }

        private async void DoImportCollection(object sender, RoutedEventArgs e) {
            try {
                var path = IOHelper.ZipFileDialog();
                if (string.IsNullOrEmpty(path)) return;

                var result1 = MessageBox.Show(
                    "将导入的收藏夹与当前收藏夹合并吗？",
                    "加载新收藏夹",
                    MessageBoxButton.YesNo);

                if (result1 == MessageBoxResult.Yes) {
                    using ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Read);
                    foreach (var patternEntry in archive.Entries.Where(o => o.FullName.EndsWith(".osu"))) {
                        patternEntry.ExtractToFile(ViewModel.FileHandler.GetPatternPath(patternEntry.Name));
                    }
                    // Load project from zip archive
                    var projectEntry = archive.Entries.Single(o => o.FullName.EndsWith(".json"));
                    var project = ProjectManager.LoadJson<PatternGalleryVm>(projectEntry.Open());

                    // Add the patterns from the imported project to the current project
                    foreach (var pattern in project.Patterns) {
                        ViewModel.Patterns.Add(pattern);
                    }
                } else {
                    string archiveFolderName;
                    using (ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Read)) {
                        // Assuming the first folder in the zip file is the collection folder
                        archiveFolderName = archive.Entries[0].FullName.Split(new[] {'/', '\\'}, StringSplitOptions.RemoveEmptyEntries)[0];

                        if (ViewModel.FileHandler.CollectionFolderExists(archiveFolderName)) {
                            throw new DuplicateNameException($"文件夹 {ViewModel.FileHandler.BasePath} 中已存在收藏夹 \"{archiveFolderName}\" 。");
                        }

                        archive.ExtractToDirectory(ViewModel.FileHandler.BasePath);
                    }

                    await Task.Factory.StartNew(() => MainWindow.MessageQueue.Enqueue("成功导入收藏夹！"));

                    var result2 = MessageBox.Show(
                        "立刻加载导入的新收藏夹吗？\n 警告：未保存的更改将会丢失。",
                        "加载新收藏夹",
                        MessageBoxButton.YesNo);

                    if (result2 != MessageBoxResult.Yes) {
                        return;
                    }

                    string collectionFolderPath = Path.Combine(ViewModel.FileHandler.BasePath, archiveFolderName);
                    // Get the first .json file in the imported collection folder
                    string savePath = Directory.GetFiles(collectionFolderPath).First(o => Path.GetExtension(o).ToLower() == ".json");
                    var project = ProjectManager.LoadJson<PatternGalleryVm>(savePath);

                    SetSaveData(project);
                }
            } catch (ArgumentException) { } catch (Exception ex) {
                ex.Show();
            }
        }

        private async void DoExportCollection(object sender, RoutedEventArgs e) {
            try {
                string exportFolder = MainWindow.ExportPath;
                string saveName = ViewModel.CollectionName;
                string savePath = Path.Combine(exportFolder, saveName + ".zip");

                using (FileStream zipToOpen = new FileStream(savePath, FileMode.OpenOrCreate)) {
                    using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create)) {
                        // Write the save json in the archive
                        var saveEntryName = Path.Combine(ViewModel.FileHandler.CollectionFolderName, saveName + ".json");
                        ZipArchiveEntry saveEntry = archive.CreateEntry(saveEntryName);
                        using (StreamWriter writer = new StreamWriter(saveEntry.Open())) {
                            ProjectManager.WriteJson(writer, GetSaveData());
                        }

                        // Add the folder of pattern files
                        foreach (var pattern in ViewModel.Patterns) {
                            var patternFilePath = ViewModel.FileHandler.GetPatternPath(pattern.FileName);
                            var entryName = ViewModel.FileHandler.GetPatternRelativePath(pattern.FileName);
                            archive.CreateEntryFromFile(patternFilePath, entryName);
                        }
                    }
                }

                await Task.Factory.StartNew(() => MainWindow.MessageQueue.Enqueue("成功导出收藏夹！"));
                ShowSelectedInExplorer.FilesOrFolders(savePath);
            } catch (ArgumentException) { } catch (Exception ex) {
                ex.Show();
            }
        }

        private void PatternRow_PreviewMouseUp(object sender, MouseButtonEventArgs e) {
            try {
                if (sender is not ListBoxItem { Content: OsuPattern pattern } || e.ChangedButton != MouseButton.Left) {
                    return;
                }

                // Select only this pattern
                ViewModel.SetSelectAll(false);
                pattern.IsSelected = true;
            } catch (Exception ex) { ex.Show(); }
        }

        private void PatternRow_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            try {
                if (sender is ListBoxItem { Content: OsuPattern pattern }) {
                    // Select only this pattern
                    ViewModel.SetSelectAll(false);
                    pattern.IsSelected = true;
                }
                QuickRun();
            } catch (Exception ex) { ex.Show(); }
        }

        private void CollectionName_MouseDown(object sender, MouseButtonEventArgs e) {
            DoRenameCollection(sender, e);
        }
    }
}
