﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Mapping_Tools.Classes;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.ToolHelpers;
using Mapping_Tools.Viewmodels;
using MaterialDesignThemes.Wpf;
using NAudio.Wave;

namespace Mapping_Tools.Views.HitsoundStudio
{

    /// <summary>
    /// Interactielogica voor HitsoundStudioView.xaml
    /// </summary>
    public partial class HitsoundStudioView : ISavable<HitsoundStudioVm>, IHaveExtraProjectMenuItems
    {
        private HitsoundStudioVm settings;

        private bool suppressEvents;

        private List<HitsoundLayer> selectedLayers;
        private HitsoundLayer selectedLayer;

        private static IWavePlayer outputDevice;

        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "hsstudioproject.json");

        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Hitsound Studio Projects");

        public static readonly string ToolName = "音效工作室";

        public static readonly string ToolDescription = $@"音效工作室可以从多个外部源文件导入数据并转换成osu!Std音效专用谱面，之后可以复制给其他谱面。{Environment.NewLine}它把音效表示为一系列音效层，每一层包含一个独特的声音、音效组和打击音效，以及表示声音在何时播放的时间列表。";

        public HitsoundStudioView()
        {
            InitializeComponent();
            Width = MainWindow.AppWindow.ContentViews.Width;
            Height = MainWindow.AppWindow.ContentViews.Height;
            settings = new HitsoundStudioVm();
            DataContext = settings;
            LayersList.SelectedIndex = 0;
            Num_Layers_Changed();
            GetSelectedLayers();
            ProjectManager.LoadProject(this, message: false);

            // This tool is verbose because of the 'show results' option
            Verbose = true;
        }

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var bgw = sender as BackgroundWorker;
            e.Result = Make_Hitsounds((HitsoundStudioVm)e.Argument, bgw, e);
        }

        private string Make_Hitsounds(HitsoundStudioVm arg, BackgroundWorker worker, DoWorkEventArgs _) {
            string result = string.Empty;

            bool validateSampleFile =
                !(arg.SingleSampleExportFormat == HitsoundExporter.SampleExportFormat.MidiChords ||
                  arg.MixedSampleExportFormat == HitsoundExporter.SampleExportFormat.MidiChords);

            var comparer = new SampleGeneratingArgsComparer(validateSampleFile);

            if (arg.HitsoundExportModeSetting == HitsoundStudioVm.HitsoundExportMode.Standard) {
                // Convert the multiple layers into packages that have the samples from all the layers at one specific time
                // Don't add default sample when exporting midi files because that's not a final export.
                List<SamplePackage> samplePackages = HitsoundConverter.ZipLayers(arg.HitsoundLayers, arg.DefaultSample, arg.ZipLayersLeniency, validateSampleFile);
                UpdateProgressBar(worker, 10);

                // Balance the volume between greenlines and samples
                HitsoundConverter.BalanceVolumes(samplePackages, 0, false);
                UpdateProgressBar(worker, 20);

                // Load the samples so validation can be done
                HashSet<SampleGeneratingArgs> allSampleArgs = new HashSet<SampleGeneratingArgs>(comparer);
                foreach (SamplePackage sp in samplePackages) {
                    allSampleArgs.UnionWith(sp.Samples.Select(o => o.SampleArgs));
                }

                var loadedSamples = SampleImporter.ImportSamples(allSampleArgs, comparer);
                UpdateProgressBar(worker, 30);

                // Convert the packages to hitsounds that fit on an osu standard map
                CompleteHitsounds completeHitsounds =
                    HitsoundConverter.GetCompleteHitsounds(samplePackages, loadedSamples, 
                        arg.UsePreviousSampleSchema ? arg.PreviousSampleSchema.GetCustomIndices() : null, 
                        arg.AllowGrowthPreviousSampleSchema, arg.FirstCustomIndex, validateSampleFile, comparer);
                UpdateProgressBar(worker, 60);

                // Save current sample schema
                if (!arg.UsePreviousSampleSchema) {
                    arg.PreviousSampleSchema = new SampleSchema(completeHitsounds.CustomIndices);
                } else if (arg.AllowGrowthPreviousSampleSchema) {
                    arg.PreviousSampleSchema.MergeWith(new SampleSchema(completeHitsounds.CustomIndices));
                }

                if (arg.ShowResults) {
                    // Count the number of samples
                    int samples = completeHitsounds.CustomIndices.SelectMany(ci => ci.Samples.Values)
                        .Count(h => h.Any(o => 
                            SampleImporter.ValidateSampleArgs(o, loadedSamples, validateSampleFile)));

                    // Count the number of changes of custom index
                    int greenlines = 0;
                    int lastIndex = -1;
                    foreach (var hit in completeHitsounds.Hitsounds.Where(hit => hit.CustomIndex != lastIndex)) {
                        lastIndex = hit.CustomIndex;
                        greenlines++;
                    }

                    result = $"音效组数量：{completeHitsounds.CustomIndices.Count}，" +
                             $"音效数量：{samples}，绿线数量：{greenlines}";
                }

                if (arg.DeleteAllInExportFirst && (arg.ExportSamples || arg.ExportMap)) {
                    // Delete all files in the export folder before filling it again
                    DirectoryInfo di = new DirectoryInfo(arg.ExportFolder);
                    foreach (FileInfo file in di.GetFiles()) {
                        file.Delete();
                    }
                }

                UpdateProgressBar(worker, 70);

                // Export the hitsound map and sound samples
                if (arg.ExportMap) {
                    HitsoundExporter.ExportHitsounds(completeHitsounds.Hitsounds, 
                        arg.BaseBeatmap, arg.ExportFolder, arg.HitsoundDiffName, arg.HitsoundExportGameMode, true, false);
                }

                UpdateProgressBar(worker, 80);

                if (arg.ExportSamples) {
                    HitsoundExporter.ExportCustomIndices(completeHitsounds.CustomIndices, arg.ExportFolder,
                        loadedSamples, arg.SingleSampleExportFormat, arg.MixedSampleExportFormat, comparer);
                }

                UpdateProgressBar(worker, 99);
            } else if (arg.HitsoundExportModeSetting == HitsoundStudioVm.HitsoundExportMode.Coinciding) {
                List<SamplePackage> samplePackages = HitsoundConverter.ZipLayers(arg.HitsoundLayers, arg.DefaultSample, 0, false);

                HitsoundConverter.BalanceVolumes(samplePackages, 0, false, true);
                UpdateProgressBar(worker, 20);

                Dictionary<SampleGeneratingArgs, SampleSoundGenerator> loadedSamples = null;
                Dictionary<SampleGeneratingArgs, string> sampleNames = arg.UsePreviousSampleSchema ? arg.PreviousSampleSchema?.GetSampleNames(comparer) : null;
                Dictionary<SampleGeneratingArgs, Vector2> samplePositions = null;
                var hitsounds = HitsoundConverter.GetHitsounds(samplePackages, ref loadedSamples, ref sampleNames, ref samplePositions,
                    arg.HitsoundExportGameMode == GameMode.Mania, arg.AddCoincidingRegularHitsounds, arg.AllowGrowthPreviousSampleSchema,
                    validateSampleFile, comparer);

                // Save current sample schema
                if (!arg.UsePreviousSampleSchema || arg.PreviousSampleSchema == null) {
                    arg.PreviousSampleSchema = new SampleSchema(sampleNames);
                } else if (arg.AllowGrowthPreviousSampleSchema) {
                    arg.PreviousSampleSchema.MergeWith(new SampleSchema(sampleNames));
                }

                // Load the samples so validation can be done
                UpdateProgressBar(worker, 50);

                if (arg.ShowResults) {
                    result = "音效组数量：0，" +
                             $"音效数量：{loadedSamples.Count}，绿线数量：0";
                }

                if (arg.DeleteAllInExportFirst && (arg.ExportSamples || arg.ExportMap)) {
                    // Delete all files in the export folder before filling it again
                    DirectoryInfo di = new DirectoryInfo(arg.ExportFolder);
                    foreach (FileInfo file in di.GetFiles()) {
                        file.Delete();
                    }
                }
                UpdateProgressBar(worker, 60);

                if (arg.ExportMap) {
                    HitsoundExporter.ExportHitsounds(hitsounds, 
                        arg.BaseBeatmap, arg.ExportFolder, arg.HitsoundDiffName, arg.HitsoundExportGameMode, false, false);
                }
                UpdateProgressBar(worker, 70);

                if (arg.ExportSamples) {
                    HitsoundExporter.ExportLoadedSamples(loadedSamples, arg.ExportFolder, sampleNames, arg.SingleSampleExportFormat, comparer);
                }
            } else if (arg.HitsoundExportModeSetting == HitsoundStudioVm.HitsoundExportMode.Storyboard) {
                List<SamplePackage> samplePackages = HitsoundConverter.ZipLayers(arg.HitsoundLayers, arg.DefaultSample, 0, false);

                HitsoundConverter.BalanceVolumes(samplePackages, 0, false, true);
                UpdateProgressBar(worker, 20);

                Dictionary<SampleGeneratingArgs, SampleSoundGenerator> loadedSamples = null;
                Dictionary<SampleGeneratingArgs, string> sampleNames = arg.UsePreviousSampleSchema ? arg.PreviousSampleSchema?.GetSampleNames(comparer) : null;
                Dictionary<SampleGeneratingArgs, Vector2> samplePositions = null;
                var hitsounds = HitsoundConverter.GetHitsounds(samplePackages, ref loadedSamples, ref sampleNames, ref samplePositions,
                    false, false, arg.AllowGrowthPreviousSampleSchema, validateSampleFile, comparer);

                // Save current sample schema
                if (!arg.UsePreviousSampleSchema || arg.PreviousSampleSchema == null) {
                    arg.PreviousSampleSchema = new SampleSchema(sampleNames);
                } else if (arg.AllowGrowthPreviousSampleSchema) {
                    arg.PreviousSampleSchema.MergeWith(new SampleSchema(sampleNames));
                }

                // Load the samples so validation can be done
                UpdateProgressBar(worker, 50);

                if (arg.ShowResults) {
                    result = "音效组数量：0，" +
                             $"音效数量：{loadedSamples.Count}，绿线数量：0";
                }

                if (arg.DeleteAllInExportFirst && (arg.ExportSamples || arg.ExportMap)) {
                    // Delete all files in the export folder before filling it again
                    DirectoryInfo di = new DirectoryInfo(arg.ExportFolder);
                    foreach (FileInfo file in di.GetFiles()) {
                        file.Delete();
                    }
                }
                UpdateProgressBar(worker, 60);

                if (arg.ExportMap) {
                    HitsoundExporter.ExportHitsounds(hitsounds, 
                        arg.BaseBeatmap, arg.ExportFolder, arg.HitsoundDiffName, arg.HitsoundExportGameMode, false, true);
                }
                UpdateProgressBar(worker, 70);

                if (arg.ExportSamples) {
                    HitsoundExporter.ExportLoadedSamples(loadedSamples, arg.ExportFolder, sampleNames, arg.SingleSampleExportFormat, comparer);
                }
            } else if (arg.HitsoundExportModeSetting == HitsoundStudioVm.HitsoundExportMode.Midi) {
                List<SamplePackage> samplePackages = HitsoundConverter.ZipLayers(arg.HitsoundLayers, arg.DefaultSample, 0, false);
                var beatmap = EditorReaderStuff.GetNewestVersionOrNot(arg.BaseBeatmap).Beatmap;

                if (arg.ShowResults) {
                    result = $"物件（Note）数量：{samplePackages.SelectMany(o => o.Samples).Count()}，" +
                             $"音量调整次数：{(arg.AddGreenLineVolumeToMidi ? beatmap.BeatmapTiming.TimingPoints.Count : 0)}";
                }

                UpdateProgressBar(worker, 20);

                if (arg.DeleteAllInExportFirst &&  arg.ExportMap) {
                    // Delete all files in the export folder before filling it again
                    DirectoryInfo di = new DirectoryInfo(arg.ExportFolder);
                    foreach (FileInfo file in di.GetFiles()) {
                        file.Delete();
                    }
                }

                UpdateProgressBar(worker, 40);

                if (arg.ExportMap) {
                    MidiExporter.ExportAsMidi(samplePackages, beatmap, Path.Combine(arg.ExportFolder, arg.HitsoundDiffName + ".mid"), arg.AddGreenLineVolumeToMidi);
                }
            }

            // Open export folder
            if (arg.ExportSamples || arg.ExportMap) {
                System.Diagnostics.Process.Start("explorer.exe", arg.ExportFolder);
            }

            // Collect garbage
            GC.Collect();

            UpdateProgressBar(worker, 100);

            return result;
        }

        private async void Start_Click(object sender, RoutedEventArgs e) {
            var dialog = new HitsoundStudioExportDialog(settings);
            var result = await DialogHost.Show(dialog, "RootDialog");

            if (!(bool) result) return;

            // Remove logical focus to trigger LostFocus on any fields that didn't yet update the ViewModel
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(this), null);

            if (string.IsNullOrWhiteSpace(settings.BaseBeatmap))
            {
                MessageBox.Show("请先选择一个基准谱面。");
                return;
            }

            if (settings.UsePreviousSampleSchema && settings.PreviousSampleSchema == null) {
                MessageBox.Show("无法使用上一次的采样规划，因为它并不是由上一次运行生成的。请取消勾选“使用上次采样规划”并重新运行。");
                return;
            }

            if (!Directory.Exists(settings.ExportFolder))
            {
                var folderResult = MessageBox.Show(
                    $"文件夹路径 \"{settings.ExportFolder}\" 不存在。\n创建新文件夹？",
                    "未找到输出路径。", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (folderResult == MessageBoxResult.Yes) {
                    try {
                        Directory.CreateDirectory(settings.ExportFolder);
                    } catch (Exception ex) {
                        ex.Show();
                        return;
                    }
                } else {
                    return;
                }
            }

            BackgroundWorker.RunWorkerAsync(settings);
            CanRun = false;
        }

        private void GetSelectedLayers()
        {
            selectedLayers = new List<HitsoundLayer>();

            if (LayersList.SelectedItems.Count == 0)
            {
                selectedLayer = null;
                return;
            }

            foreach (HitsoundLayer hsl in LayersList.SelectedItems)
            {
                selectedLayers.Add(hsl);
            }

            selectedLayer = selectedLayers[0];
        }

        private void SelectedSamplePathBrowse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = IOHelper.SampleFileDialog();
                if (path != "")
                {
                    SelectedSamplePathBox.Text = path;
                }
            } catch (Exception ex) { ex.Show(); }
        }

        private void SelectedImportSamplePathBrowse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = IOHelper.SampleFileDialog();
                if (path != "")
                {
                    SelectedImportSamplePathBox.Text = path;
                    SelectedStoryboardImportSamplePathBox.Text = path;
                }
            } catch (Exception ex) { ex.Show(); }
        }

        private void SelectedImportPathBrowse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = IOHelper.FileDialog();
                if (!string.IsNullOrEmpty(path))
                {
                    SelectedImportPathBox.Text = path;
                }
            } catch (Exception ex) { ex.Show(); }
        }

        private void SelectedImportPathLoad_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = IOHelper.GetCurrentBeatmap();
                if (path != "")
                {
                    SelectedImportPathBox.Text = path;
                }
            }
            catch (Exception ex) { ex.Show(); }
        }

        private void DefaultSampleBrowse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = IOHelper.SampleFileDialog();
                if (path != "")
                {
                    settings.DefaultSample.SampleArgs.Path = path;
                    DefaultSamplePathBox.Text = path;
                }
            } catch (Exception ex) { ex.Show(); }
        }

        private void BaseBeatmapBrowse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string[] paths = IOHelper.BeatmapFileDialog(restore: !SettingsManager.Settings.CurrentBeatmapDefaultFolder);
                if (paths.Length != 0)
                {
                    settings.BaseBeatmap = paths[0];
                }
            } catch (Exception ex) { ex.Show(); }
        }

        private void BaseBeatmapLoad_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = IOHelper.GetCurrentBeatmap();
                if (path != "")
                {
                    settings.BaseBeatmap = path;
                }
            } catch (Exception ex) { ex.Show(); }
        }

        private void ReloadFromSource_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var seperatedByImportArgsForReloading = new Dictionary<ImportReloadingArgs, List<HitsoundLayer>>(new ImportReloadingArgsComparer());

                foreach (var layer in selectedLayers)
                {
                    var reloadingArgs = layer.ImportArgs.GetImportReloadingArgs();
                    if (seperatedByImportArgsForReloading.TryGetValue(reloadingArgs, out List<HitsoundLayer> value))
                    {
                        value.Add(layer);
                    }
                    else
                    {
                        seperatedByImportArgsForReloading.Add(reloadingArgs, new List<HitsoundLayer> { layer });
                    }
                }

                foreach (var pair in seperatedByImportArgsForReloading)
                {
                    var reloadingArgs = pair.Key;
                    var layers = pair.Value;

                    var importedLayers = HitsoundImporter.ImportReloading(reloadingArgs);

                    layers.ForEach(o => o.Reload(importedLayers));
                }

                UpdateEditingField();
            }
            catch (Exception ex) { ex.Show(); }
        }

        private void LayersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (suppressEvents) return;

            GetSelectedLayers();
            UpdateEditingField();
        }

        private void UpdateEditingField()
        {
            if (selectedLayers.Count == 0) { return; }

            suppressEvents = true;

            // Populate the editing fields
            SelectedNameBox.Text = selectedLayers.AllToStringOrDefault(o => o.Name);
            SelectedSampleSetBox.Text = selectedLayers.AllToStringOrDefault(o => o.SampleSetString);
            SelectedHitsoundBox.Text = selectedLayers.AllToStringOrDefault(o => o.HitsoundString);
            TimesBox.Text = selectedLayers.AllToStringOrDefault(o => o.Times, HitsoundLayerExtension.DoubleListToStringConverter);

            SelectedSamplePathBox.Text = selectedLayers.AllToStringOrDefault(o => o.SampleArgs.Path);
            SelectedSampleVolumeBox.Text = selectedLayers.AllToStringOrDefault(o => o.SampleArgs.Volume * 100, CultureInfo.InvariantCulture);
            SelectedSamplePanningBox.Text = selectedLayers.AllToStringOrDefault(o => o.SampleArgs.Panning, CultureInfo.InvariantCulture);
            SelectedSamplePitchShiftBox.Text = selectedLayers.AllToStringOrDefault(o => o.SampleArgs.PitchShift, CultureInfo.InvariantCulture);
            SelectedSampleBankBox.Text = selectedLayers.AllToStringOrDefault(o => o.SampleArgs.Bank);
            SelectedSamplePatchBox.Text = selectedLayers.AllToStringOrDefault(o => o.SampleArgs.Patch);
            SelectedSampleInstrumentBox.Text = selectedLayers.AllToStringOrDefault(o => o.SampleArgs.Instrument);
            SelectedSampleKeyBox.Text = selectedLayers.AllToStringOrDefault(o => o.SampleArgs.Key);
            SelectedSampleLengthBox.Text = selectedLayers.AllToStringOrDefault(o => o.SampleArgs.Length, CultureInfo.InvariantCulture);
            SelectedSampleVelocityBox.Text = selectedLayers.AllToStringOrDefault(o => o.SampleArgs.Velocity);

            SelectedImportTypeBox.Text = selectedLayers.AllToStringOrDefault(o => o.ImportArgs.ImportType);
            SelectedImportPathBox.Text = selectedLayers.AllToStringOrDefault(o => o.ImportArgs.Path);
            SelectedImportXCoordBox.Text = selectedLayers.AllToStringOrDefault(o => o.ImportArgs.X, CultureInfo.InvariantCulture);
            SelectedImportYCoordBox.Text = selectedLayers.AllToStringOrDefault(o => o.ImportArgs.Y, CultureInfo.InvariantCulture);
            SelectedImportSamplePathBox.Text = selectedLayers.AllToStringOrDefault(o => o.ImportArgs.SamplePath);
            SelectedHitsoundImportDiscriminateVolumesBox.IsChecked = selectedLayers.All(o => o.ImportArgs.DiscriminateVolumes);
            SelectedHitsoundImportDetectDuplicateSamplesBox.IsChecked = selectedLayers.All(o => o.ImportArgs.DetectDuplicateSamples);
            SelectedHitsoundImportRemoveDuplicatesBox.IsChecked = selectedLayers.All(o => o.ImportArgs.RemoveDuplicates);
            SelectedStoryboardImportSamplePathBox.Text = selectedLayers.AllToStringOrDefault(o => o.ImportArgs.SamplePath);
            SelectedStoryboardImportDiscriminateVolumesBox.IsChecked = selectedLayers.All(o => o.ImportArgs.DiscriminateVolumes);
            SelectedStoryboardImportRemoveDuplicatesBox.IsChecked = selectedLayers.All(o => o.ImportArgs.RemoveDuplicates);
            SelectedImportBankBox.Text = selectedLayers.AllToStringOrDefault(o => o.ImportArgs.Bank);
            SelectedImportPatchBox.Text = selectedLayers.AllToStringOrDefault(o => o.ImportArgs.Patch);
            SelectedImportKeyBox.Text = selectedLayers.AllToStringOrDefault(o => o.ImportArgs.Key);
            SelectedImportLengthBox.Text = selectedLayers.AllToStringOrDefault(o => o.ImportArgs.Length, CultureInfo.InvariantCulture);
            SelectedImportLengthRoughnessBox.Text = selectedLayers.AllToStringOrDefault(o => o.ImportArgs.LengthRoughness, CultureInfo.InvariantCulture);
            SelectedImportVelocityBox.Text = selectedLayers.AllToStringOrDefault(o => o.ImportArgs.Velocity);
            SelectedImportVelocityRoughnessBox.Text = selectedLayers.AllToStringOrDefault(o => o.ImportArgs.VelocityRoughness, CultureInfo.InvariantCulture);
            SelectedImportOffsetBox.Text = selectedLayers.AllToStringOrDefault(o => o.ImportArgs.Offset, CultureInfo.InvariantCulture);

            // Update visibility
            SoundFontArgsPanel.Visibility = selectedLayers.Any(o => o.SampleArgs.UsesSoundFont || string.IsNullOrEmpty(o.SampleArgs.GetExtension())) ? Visibility.Visible : Visibility.Collapsed;
            SelectedStackPanel.Visibility = selectedLayers.Any(o => o.ImportArgs.ImportType == ImportType.堆叠) ? Visibility.Visible : Visibility.Collapsed;
            SelectedHitsoundsPanel.Visibility = selectedLayers.Any(o => o.ImportArgs.ImportType == ImportType.音效) ? Visibility.Visible : Visibility.Collapsed;
            SelectedStoryboardPanel.Visibility = selectedLayers.Any(o => o.ImportArgs.ImportType == ImportType.故事板) ? Visibility.Visible : Visibility.Collapsed;
            SelectedMidiPanel.Visibility = selectedLayers.Any(o => o.ImportArgs.ImportType == ImportType.MIDI) ? Visibility.Visible : Visibility.Collapsed;
            ImportArgsPanel.Visibility = selectedLayers.Any(o => o.ImportArgs.CanImport) ? Visibility.Visible : Visibility.Collapsed;

            suppressEvents = false;
        }

        private void HitsoundLayer_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                SampleGeneratingArgs args = selectedLayer.SampleArgs;
                var mainOutputStream = SampleImporter.ImportSample(args);

                if (mainOutputStream == null)
                {
                    MessageBox.Show("无法加载指定采样。");
                    return;
                }
                
                outputDevice = new WasapiOut();
                outputDevice.PlaybackStopped += PlayerStopped;

                outputDevice.Init(mainOutputStream.GetSampleProvider());

                outputDevice.Play();
            }
            catch (FileNotFoundException) { MessageBox.Show("找不到指定采样。"); }
            catch (DirectoryNotFoundException) { MessageBox.Show("找不到指定采样的文件夹。"); }
            catch (Exception ex) { ex.Show(); }
        }

        private static void PlayerStopped(object sender, StoppedEventArgs e)
        {
            ((IWavePlayer)sender).Dispose();
        }

        private void Num_Layers_Changed()
        {
            if (settings.HitsoundLayers.Count == 0)
            {
                FirstGrid.ColumnDefinitions[0].Width = new GridLength(0);
                EditPanel.IsEnabled = false;
            }
            else if (FirstGrid.ColumnDefinitions[0].Width.Value < 100)
            {
                FirstGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                FirstGrid.ColumnDefinitions[2].Width = new GridLength(2, GridUnitType.Star);
                EditPanel.IsEnabled = true;
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                HitsoundLayerImportWindow importWindow = new HitsoundLayerImportWindow(settings.HitsoundLayers.Count);
                importWindow.ShowDialog();

                LayersList.SelectedItems.Clear();
                foreach (HitsoundLayer layer in importWindow.HitsoundLayers)
                {
                    if (layer != null)
                    {
                        settings.HitsoundLayers.Add(layer);
                        LayersList.SelectedItems.Add(layer);
                    }
                }

                RecalculatePriorities();
                Num_Layers_Changed();
                GetSelectedLayers();
            }
            catch (Exception ex)
            {
                ex.Show();
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ask for confirmation
                MessageBoxResult messageBoxResult = MessageBox.Show("确定吗？", "确认删除", MessageBoxButton.YesNo);
                if (messageBoxResult != MessageBoxResult.Yes) { return; }

                if (selectedLayers.Count == 0 || selectedLayers == null) { return; }

                suppressEvents = true;

                int index = settings.HitsoundLayers.IndexOf(selectedLayer);

                foreach (HitsoundLayer hsl in selectedLayers)
                {
                    settings.HitsoundLayers.Remove(hsl);
                }
                suppressEvents = false;

                LayersList.SelectedIndex = Math.Max(Math.Min(index - 1, settings.HitsoundLayers.Count - 1), 0);

                RecalculatePriorities();
                Num_Layers_Changed();
            }
            catch (Exception ex) { ex.Show(); }
        }

        private void Raise_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int repeats = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? 10 : 1;
                for (int n = 0; n < repeats; n++)
                {
                    suppressEvents = true;

                    int selectedIndex = settings.HitsoundLayers.IndexOf(selectedLayer);
                    List<HitsoundLayer> moveList = new List<HitsoundLayer>();
                    foreach (HitsoundLayer hsl in selectedLayers)
                    {
                        moveList.Add(hsl);
                    }

                    foreach (HitsoundLayer hsl in settings.HitsoundLayers)
                    {
                        if (moveList.Contains(hsl))
                        {
                            moveList.Remove(hsl);
                        }
                        else
                            break;
                    }

                    foreach (HitsoundLayer hsl in moveList)
                    {
                        int index = settings.HitsoundLayers.IndexOf(hsl);

                        //Dont move left if it is the first item in the list or it is not in the list
                        if (index <= 0)
                            continue;

                        //Swap with this item with the one to its left
                        settings.HitsoundLayers.Remove(hsl);
                        settings.HitsoundLayers.Insert(index - 1, hsl);
                    }

                    LayersList.SelectedItems.Clear();
                    foreach (HitsoundLayer hsl in selectedLayers)
                    {
                        LayersList.SelectedItems.Add(hsl);
                    }

                    suppressEvents = false;

                    RecalculatePriorities();
                    GetSelectedLayers();
                }
            }
            catch (Exception ex) { ex.Show(); }
        }

        private void Lower_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int repeats = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? 10 : 1;
                for (int n = 0; n < repeats; n++)
                {
                    suppressEvents = true;

                    int selectedIndex = settings.HitsoundLayers.IndexOf(selectedLayer);
                    List<HitsoundLayer> moveList = new List<HitsoundLayer>();
                    foreach (HitsoundLayer hsl in selectedLayers)
                    {
                        moveList.Add(hsl);
                    }

                    for (int i = settings.HitsoundLayers.Count - 1; i >= 0; i--)
                    {
                        HitsoundLayer hsl = settings.HitsoundLayers[i];
                        if (moveList.Contains(hsl))
                        {
                            moveList.Remove(hsl);
                        }
                        else
                            break;
                    }

                    for (int i = moveList.Count - 1; i >= 0; i--)
                    {
                        HitsoundLayer hsl = moveList[i];
                        int index = settings.HitsoundLayers.IndexOf(hsl);

                        //Dont move left if it is the first item in the list or it is not in the list
                        if (index >= settings.HitsoundLayers.Count - 1)
                            continue;

                        //Swap with this item with the one to its left
                        settings.HitsoundLayers.Remove(hsl);
                        settings.HitsoundLayers.Insert(index + 1, hsl);
                    }

                    LayersList.SelectedItems.Clear();
                    foreach (HitsoundLayer hsl in selectedLayers)
                    {
                        LayersList.SelectedItems.Add(hsl);
                    }

                    suppressEvents = false;

                    RecalculatePriorities();
                    GetSelectedLayers();
                }
            }
            catch (Exception ex) { ex.Show(); }
        }

        private void RecalculatePriorities()
        {
            for (int i = 0; i < settings.HitsoundLayers.Count; i++)
            {
                settings.HitsoundLayers[i].Priority = i;
            }
        }

        private void ValidateSamples_Click(object sender, RoutedEventArgs e) {
            var couldNotFind = new List<HitsoundLayer>();
            var invalidExtension = new List<HitsoundLayer>();
            var couldNotLoad = new List<(HitsoundLayer, Exception)>();

            var allSampleArgs = settings.HitsoundLayers.Select(o => o.SampleArgs).ToList();
            var sampleExceptions = SampleImporter.ValidateSamples(allSampleArgs);

            foreach (HitsoundLayer hitsoundLayer in settings.HitsoundLayers) {
                if (string.IsNullOrEmpty(hitsoundLayer.SampleArgs.Path))
                    continue;

                if (!sampleExceptions.TryGetValue(hitsoundLayer.SampleArgs, out var exception) || exception == null)
                    continue;

                switch (exception) {
                    case FileNotFoundException:
                        couldNotFind.Add(hitsoundLayer);
                        break;
                    case InvalidDataException:
                        invalidExtension.Add(hitsoundLayer);
                        break;
                    default:
                        couldNotLoad.Add((hitsoundLayer, exception));
                        break;
                }
            }


            if (couldNotFind.Count == 0 && invalidExtension.Count == 0 && couldNotLoad.Count == 0) {
                MessageBox.Show("所有采样均有效！");
                return;
            }

            var message = new StringBuilder();

            if (couldNotFind.Count > 0) {
                message.AppendLine("找不到以下采样：");
                message.AppendLine(string.Join(Environment.NewLine, couldNotFind.Select(o => o.Name)));
                message.AppendLine();
            }

            if (invalidExtension.Count > 0) {
                message.AppendLine("以下采样的文件类型无效：");
                message.AppendLine(string.Join(Environment.NewLine, invalidExtension.Select(o => o.Name)));
                message.AppendLine();
            }

            if (couldNotLoad.Count > 0) {
                message.AppendLine("加载以下采样时出现问题：");
                message.AppendLine(string.Join(Environment.NewLine, couldNotLoad.Select(o => $"{o.Item1.Name}: {o.Item2.Message}")));
                message.AppendLine();
            }

            MessageBox.Show(message.ToString());
        }

        #region HitsoundLayerChangeEventHandlers

        private void SelectedNameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;

            string t = (sender as TextBox).Text;
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.Name = t;
            }
        }

        private void SelectedSampleSetBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (suppressEvents) return;

            string t = ((sender as ComboBox).SelectedItem as ComboBoxItem).Content.ToString();
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.SampleSetString = t;
            }
        }

        private void SelectedHitsoundBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (suppressEvents) return;

            string t = ((sender as ComboBox).SelectedItem as ComboBoxItem).Content.ToString();
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.HitsoundString = t;
            }
        }

        private void TimesBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;
            if ((sender as TextBox).GetBindingExpression(TextBox.TextProperty).HasValidationError) return;

            try
            {
                List<double> t = (sender as TextBox).Text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(double.Parse).OrderBy(o => o).ToList();

                foreach (HitsoundLayer hitsoundLayer in selectedLayers)
                {
                    hitsoundLayer.Times = t;
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); Console.WriteLine(ex.StackTrace); }
        }

        private void SelectedSamplePathBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;

            string t = (sender as TextBox).Text;
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.SampleArgs.Path = t;
            }
            UpdateEditingField();
        }

        private void SelectedSampleVolumeBox_TextChanged(object sender, RoutedEventArgs e)
        {
            if (suppressEvents) return;

            double t = (sender as TextBox).GetDouble(100);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.SampleArgs.Volume = t / 100;
            }
            UpdateEditingField();
        }

        private void SelectedSamplePanningBox_TextChanged(object sender, RoutedEventArgs e)
        {
            if (suppressEvents) return;

            double t = (sender as TextBox).GetDouble(0);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.SampleArgs.Panning = t;
            }
            UpdateEditingField();
        }

        private void SelectedSamplePitchShiftBox_TextChanged(object sender, RoutedEventArgs e)
        {
            if (suppressEvents) return;

            double t = (sender as TextBox).GetDouble(0);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.SampleArgs.PitchShift = t;
            }
            UpdateEditingField();
        }

        private void SelectedSampleBankBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;

            int t = (sender as TextBox).GetInt(-1);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.SampleArgs.Bank = t;
            }
        }

        private void SelectedSamplePatchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;

            int t = (sender as TextBox).GetInt(-1);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.SampleArgs.Patch = t;
            }
        }

        private void SelectedSampleInstrumentBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;

            int t = (sender as TextBox).GetInt(-1);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.SampleArgs.Instrument = t;
            }
        }

        private void SelectedSampleKeyBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;

            int t = (sender as TextBox).GetInt(-1);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.SampleArgs.Key = t;
            }
        }

        private void SelectedSampleLengthBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;

            double t = (sender as TextBox).GetDouble(-1);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.SampleArgs.Length = t;
            }
        }

        private void SelectedSampleVelocityBox_TextChanged(object sender, RoutedEventArgs e)
        {
            if (suppressEvents) return;

            int t = (sender as TextBox).GetInt(127);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.SampleArgs.Velocity = t;
            }
            UpdateEditingField();
        }

        private void SelectedImportTypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (suppressEvents) return;

            string t = ((sender as ComboBox).SelectedItem as ComboBoxItem).Content.ToString();
            ImportType type = (ImportType)Enum.Parse(typeof(ImportType), t);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.ImportArgs.ImportType = type;
            }
            UpdateEditingField();
        }

        private void SelectedImportPathBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;

            string t = (sender as TextBox).Text;
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.ImportArgs.Path = t;
            }
        }

        private void SelectedImportXCoordBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;

            double t = (sender as TextBox).GetDouble(-1);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.ImportArgs.X = t;
            }
        }

        private void SelectedImportYCoordBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;

            double t = (sender as TextBox).GetDouble(-1);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.ImportArgs.Y = t;
            }
        }

        private void SelectedImportSamplePathBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;

            string t = (sender as TextBox).Text;
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.ImportArgs.SamplePath = t;
            }
        }

        private void SelectedImportBankBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;

            int t = (sender as TextBox).GetInt(-1);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.ImportArgs.Bank = t;
            }
        }

        private void SelectedImportPatchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;

            int t = (sender as TextBox).GetInt(-1);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.ImportArgs.Patch = t;
            }
        }

        private void SelectedImportKeyBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;

            int t = (sender as TextBox).GetInt(-1);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.ImportArgs.Key = t;
            }
        }

        private void SelectedImportLengthBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;

            int t = (sender as TextBox).GetInt(-1);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.ImportArgs.Length = t;
            }
        }

        private void SelectedImportVelocityBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;

            int t = (sender as TextBox).GetInt(-1);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.ImportArgs.Velocity = t;
            }
        }

        private void SelectedImportLengthRoughnessBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;

            double t = (sender as TextBox).GetDouble(1);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.ImportArgs.LengthRoughness = t;
            }
        }

        private void SelectedImportVelocityRoughnessBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;

            double t = (sender as TextBox).GetDouble(1);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.ImportArgs.VelocityRoughness = t;
            }
        }

        private void SelectedImportOffsetBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;

            double t = (sender as TextBox).GetDouble(0);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.ImportArgs.Offset = t;
            }
        }

        private void SelectedImportDiscriminateVolumesBox_OnChecked(object sender, RoutedEventArgs e) {
            if (suppressEvents) return;

            foreach (HitsoundLayer hitsoundLayer in selectedLayers) {
                hitsoundLayer.ImportArgs.DiscriminateVolumes = true;
            }
        }

        private void SelectedImportDiscriminateVolumesBox_OnUnchecked(object sender, RoutedEventArgs e) {
            if (suppressEvents) return;

            foreach (HitsoundLayer hitsoundLayer in selectedLayers) {
                hitsoundLayer.ImportArgs.DiscriminateVolumes = false;
            }
        }

        private void SelectedImportRemoveDuplicatesBox_OnChecked(object sender, RoutedEventArgs e) {
            if (suppressEvents) return;

            foreach (HitsoundLayer hitsoundLayer in selectedLayers) {
                hitsoundLayer.ImportArgs.RemoveDuplicates = true;
            }
        }

        private void SelectedImportRemoveDuplicatesBox_OnUnchecked(object sender, RoutedEventArgs e) {
            if (suppressEvents) return;

            foreach (HitsoundLayer hitsoundLayer in selectedLayers) {
                hitsoundLayer.ImportArgs.RemoveDuplicates = false;
            }
        }

        private void SelectedHitsoundImportDetectDuplicateSamplesBox_OnChecked(object sender, RoutedEventArgs e) {
            if (suppressEvents) return;

            foreach (HitsoundLayer hitsoundLayer in selectedLayers) {
                hitsoundLayer.ImportArgs.DetectDuplicateSamples = true;
            }
        }

        private void SelectedHitsoundImportDetectDuplicateSamplesBox_OnUnchecked(object sender, RoutedEventArgs e) {
            if (suppressEvents) return;

            foreach (HitsoundLayer hitsoundLayer in selectedLayers) {
                hitsoundLayer.ImportArgs.DetectDuplicateSamples = false;
            }
        }

        #endregion

        public HitsoundStudioVm GetSaveData()
        {
            return settings;
        }

        public void SetSaveData(HitsoundStudioVm saveData)
        {
            suppressEvents = true;

            settings = saveData;
            DataContext = settings;

            suppressEvents = false;

            LayersList.SelectedIndex = 0;
            Num_Layers_Changed();
        }

        #region IHaveExtraMenuItems members

        public MenuItem[] GetMenuItems() {
            var loadSampleSchemaMenu = new MenuItem {
                Header = "加载采样规划（_L）", Icon = new PackIcon {Kind = PackIconKind.FileMusic},
                ToolTip = "从项目文件中加载采样规划。"
            };
            loadSampleSchemaMenu.Click += LoadSampleSchemaFromFile;

            var bulkAssignSamplesMenu = new MenuItem {
                Header = "批量分配采样（_B）", Icon = new PackIcon {Kind = PackIconKind.MusicBoxMultiple},
                ToolTip = "批量分配采样给选中的音效层。" +
                          "文件名应为以下格式：[音色库bank]_[音色patch]_[音调key]_[长度]_[速度].[扩展名]. " +
                          "留空以表示任何值。" +
                          "例如：0_39__127.wav"
            };
            bulkAssignSamplesMenu.Click += BulkAssignSamples;

            return new[] {
                loadSampleSchemaMenu,
                bulkAssignSamplesMenu,
            };
        }

        private void LoadSampleSchemaFromFile(object sender, RoutedEventArgs e) {
            try {
                var project = ProjectManager.GetProject(this, true);
                settings.PreviousSampleSchema = project.PreviousSampleSchema;

                Task.Factory.StartNew(() => MainWindow.MessageQueue.Enqueue("成功加载采样规划！"));
            } catch (ArgumentException) { }
            catch (Exception ex) {
                ex.Show();
            }
        }

        private void BulkAssignSamples(object sender, RoutedEventArgs e) {
            try {
                var result = IOHelper.AudioFileDialog(true);

                foreach (string path in result) {
                    // The file name is expected to be in the following shape:
                    // [bank]_[patch]_[key]_[length]_[velocity].[extension]
                    // Example: 0_39_256_100.wav
                    var fileName = Path.GetFileNameWithoutExtension(path);
                    var split = fileName.Split('_');
                    int? bank = split.Length > 0 && int.TryParse(split[0], out var bankParsed) ? bankParsed : null;
                    int? patch = split.Length > 1 && int.TryParse(split[1], out var patchParsed) ? patchParsed : null;
                    int? key = split.Length > 2 && int.TryParse(split[2], out var keyParsed) ? keyParsed : null;
                    int? length = split.Length > 3 && int.TryParse(split[3], out var lengthParsed) ? lengthParsed : null;
                    int? velocity = split.Length > 4 && int.TryParse(split[4], out var velocityParsed) ? velocityParsed : null;

                    foreach (var layer in selectedLayers) {
                        if (bank.HasValue && bank.Value != layer.ImportArgs.Bank) continue;
                        if (patch.HasValue && patch.Value != layer.ImportArgs.Patch) continue;
                        if (key.HasValue && key.Value != layer.ImportArgs.Key) continue;
                        if (length.HasValue && length.Value != (int)Math.Round(layer.ImportArgs.Length)) continue;
                        if (velocity.HasValue && velocity.Value != layer.ImportArgs.Velocity) continue;

                        layer.SampleArgs.Path = path;
                    }
                }


            } catch (Exception ex) {
                ex.Show();
            }
        }

        #endregion
    }
}