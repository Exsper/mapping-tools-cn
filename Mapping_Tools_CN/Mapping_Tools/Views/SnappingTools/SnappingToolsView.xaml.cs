using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Mapping_Tools.Classes;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectCollection;
using Mapping_Tools.Classes.Tools.SnappingTools.Serialization;
using Mapping_Tools.Viewmodels;
using MaterialDesignThemes.Wpf;

namespace Mapping_Tools.Views.SnappingTools {
    public partial class SnappingToolsView : ISavable<SnappingToolsProject>, IHaveExtraProjectMenuItems {

        public static readonly string ToolName = "几何仪表盘";

        public static readonly string ToolDescription = $@"在编辑器当前屏幕上，根据显示的物件位置实时生成几何相关的辅助物件。长按快捷键（默认为M）使光标吸附到最近的辅助物件上。{Environment.NewLine}⚠ 必须先在首选项中指定osu!用户配置文件地址，工具才能正常运行。";

        private double scrollOffset;
        private bool resetScroll = true;

        public SnappingToolsVm ViewModel
        {
            get => (SnappingToolsVm)DataContext;
            set => DataContext = value;
        }
        public SnappingToolsProjectWindow ProjectWindow;

        public SnappingToolsView()
        {
            DataContext = new SnappingToolsVm();
            InitializeComponent();
            Width = MainWindow.AppWindow.ContentViews.Width;
            Height = MainWindow.AppWindow.ContentViews.Height;
            ProjectManager.LoadProject(this, message: false);
        }

        private void PreferencesButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var preferencesWindow = new SnappingToolsPreferencesWindow(ViewModel.Project.GetCurrentPreferences());
            var result = preferencesWindow.ShowDialog();
            if (result.GetValueOrDefault())
            {
                ViewModel.Project.SetCurrentPreferences(preferencesWindow.Preferences);
            }
        }

        private void ProjectsButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (ProjectWindow == null)
            {
                ProjectWindow = new SnappingToolsProjectWindow(ViewModel.GetProject());
                ProjectWindow.Closed += ProjectWindowOnClosed;
                ProjectWindow.Show();
            }
            else
            {
                ProjectWindow.Activate();
            }
        }

        private void ProjectWindowOnClosed(object sender, EventArgs e)
        {
            ProjectWindow = null;
        }

        public SnappingToolsProject GetSaveData() => ViewModel.GetProject();

        public void SetSaveData(SnappingToolsProject saveData)
        {
            ViewModel.SetProject(saveData);
        }

        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "geometrydashboardproject.json");
        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Geometry Dashboard Projects");

        private void UIElement_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scv = (ScrollViewer)sender;
            scrollOffset = scv.VerticalOffset - e.Delta;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        public override void Activate()
        {
            ViewModel.Activate();
            base.Activate();
        }

        public override void Deactivate()
        {
            ViewModel.Deactivate();
            base.Deactivate();
        }

        public override void Dispose()
        {
            ViewModel.Dispose();
            base.Dispose();
        }

        private void ScrollViewer_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var scv = (ScrollViewer)sender;
            if (resetScroll && Math.Abs(scrollOffset - scv.VerticalOffset) > Precision.DoubleEpsilon)
            {
                scv.ScrollToVerticalOffset(scrollOffset);
            }
        }

        private void UIElement_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            resetScroll = !Equals(e.Source, GeneratorsScrollViewer);
        }

        #region IHaveExtraMenuItems members

        public MenuItem[] GetMenuItems() {
            var saveObjects = new MenuItem {
                Header = "保存辅助物件（_S）", Icon = new PackIcon { Kind = PackIconKind.ContentSaveOutline },
                ToolTip = "保存已锁定的辅助物件到文件。"
            };
            saveObjects.Click += SaveLockedRelevantObjectsFromFile;

            var loadObjects = new MenuItem {
                Header = "加载辅助物件（_L）", Icon = new PackIcon { Kind = PackIconKind.FolderOpenOutline },
                ToolTip = "从文件导入已锁定的辅助物件。"
            };
            loadObjects.Click += LoadLockedRelevantObjectsFromFile;

            return new[] { saveObjects, loadObjects };
        }

        private void SaveLockedRelevantObjectsFromFile(object sender, RoutedEventArgs e) {
            try {
                ProjectManager.SaveToolFile(this, ViewModel.GetLockedObjects(), true);

                Task.Factory.StartNew(() => MainWindow.MessageQueue.Enqueue("成功保存已锁定的辅助物件！"));
            } catch (ArgumentException) { } catch (Exception ex) {
                ex.Show();
            }
        }

        private void LoadLockedRelevantObjectsFromFile(object sender, RoutedEventArgs e) {
            try {
                var objects = ProjectManager.LoadToolFile<SnappingToolsProject, RelevantObjectCollection>(this, true);
                ViewModel.SetLockedObjects(objects);

                Task.Factory.StartNew(() => MainWindow.MessageQueue.Enqueue("成功加载已锁定的辅助物件！"));
            } catch (ArgumentException) { } catch (Exception ex) {
                ex.Show();
            }
        }

        #endregion
    }
}
