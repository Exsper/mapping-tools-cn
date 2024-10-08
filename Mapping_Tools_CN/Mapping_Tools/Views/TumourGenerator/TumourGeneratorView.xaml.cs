﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.SystemTools.QuickRun;
using Mapping_Tools.Classes.ToolHelpers;
using Mapping_Tools.Classes.ToolHelpers.Sliders.Newgen;
using Mapping_Tools.Classes.Tools.TumourGenerating.Options;
using Mapping_Tools.Components.Dialogs;
using Mapping_Tools.Viewmodels;
using MaterialDesignThemes.Wpf;

namespace Mapping_Tools.Views.TumourGenerator {
    //[HiddenTool]
    [SmartQuickRunUsage(SmartQuickRunTargets.AnySelection)]
    public partial class TumourGeneratorView : ISavable<TumourGeneratorVm>, IQuickRun {
        public static readonly string ToolName = "凸点生成器v2";

        public static readonly string ToolDescription = @"Tumour Generator 2 generates copious amounts of tumours on sliders.
You can adjust the shape and size of tumours and choose where they should be placed along the path.
You can also generate multiple layers of tumours. A layer will either merge the tumours with the tumours of the previous layer or place the tumours on top of the previous tumours.
Enable the Advanced toggle and click the ... button to edit how a parameter changes over time. Use the arrow keys on the sliders to do fine adjustments.
To get started, select a slider in your beatmap and click 'Preview slider' to preview changes or click the run button to instantly generate tumours on the selected sliders.";

        private bool initialized;

        private TumourGeneratorVm ViewModel => (TumourGeneratorVm)DataContext;

        public TumourGeneratorView() {
            InitializeComponent();
            SetSaveData(new TumourGeneratorVm());
            Width = MainWindow.AppWindow.ContentViews.Width;
            Height = MainWindow.AppWindow.ContentViews.Height;
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            if (initialized) return;

            ProjectManager.LoadProject(this, message: false);
            initialized = true;
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            RunTool(SelectionModeBox.SelectedIndex == 0
                ? new[] {IOHelper.GetCurrentBeatmapOrCurrentBeatmap()}
                : MainWindow.AppWindow.GetCurrentMaps());
        }

        private bool ValidateToolInput(out string message) {
            message = string.Empty;
            return true;
        }

        private async void RunTool(string[] paths, bool quick = false, bool reload = false) {
            if (!CanRun) return;

            // Remove logical focus to trigger LostFocus on any fields that didn't yet update the ViewModel
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(this), null);

            if (!ValidateToolInput(out var message)) {
                var dialog = new MessageDialog(message);
                await DialogHost.Show(dialog, "RootDialog");
                return;
            }

            BackupManager.SaveMapBackup(paths);

            ViewModel.Paths = paths;
            ViewModel.Quick = quick;
            ViewModel.Reload = reload;
            foreach (var tumourLayer in ViewModel.TumourLayers) {
                tumourLayer.Freeze();
            }

            BackgroundWorker.RunWorkerAsync(ViewModel);
            CanRun = false;
        }

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = TumourGenerate((TumourGeneratorVm) e.Argument, bgw);
        }

        private string TumourGenerate(TumourGeneratorVm arg, BackgroundWorker worker) {
            int slidersTumourated = 0;

            // Initialize the Tumour Generator
            var tumourGenerator = new Classes.Tools.TumourGenerating.TumourGenerator {
                TumourLayers = arg.TumourLayers,
                JustMiddleAnchors = arg.JustMiddleAnchors,
                Scalar = arg.Scale,
                Reconstructor = new Reconstructor {
                    DebugConstruction = arg.DebugConstruction
                }
            };
            
            // Load sliders from the selector
            var reader = EditorReaderStuff.GetFullEditorReaderOrNot(out var editorReaderException1);

            if (arg.ImportModeSetting == TumourGeneratorVm.ImportMode.选中的 && editorReaderException1 != null) {
                throw new Exception("无法获取选中物件。", editorReaderException1);
            }

            foreach (string path in arg.Paths) {
                var editor = EditorReaderStuff.GetNewestVersionOrNot(path, reader, out var selected, out var editorReaderException2);

                if (arg.ImportModeSetting == TumourGeneratorVm.ImportMode.选中的 && editorReaderException2 != null) {
                    throw new Exception("无法获取选中物件。", editorReaderException2);
                }

                Beatmap beatmap = editor.Beatmap;
                Timing timing = beatmap.BeatmapTiming;

                List<HitObject> markedObjects = arg.ImportModeSetting switch {
                    TumourGeneratorVm.ImportMode.选中的 => selected,
                    TumourGeneratorVm.ImportMode.书签处 => beatmap.GetBookmarkedObjects(),
                    TumourGeneratorVm.ImportMode.指定时间处 => beatmap.QueryTimeCode(arg.TimeCode).ToList(),
                    TumourGeneratorVm.ImportMode.所有物件 => beatmap.HitObjects,
                    _ => throw new ArgumentException("意料外的导入模式。")
                };

                for (int i = 0; i < markedObjects.Count; i++) {
                    HitObject ho = markedObjects[i];

                    // Generate copious amounts of tumours on each slider
                    var tumourated = tumourGenerator.TumourGenerate(ho);

                    if (tumourated)
                        slidersTumourated++;

                    if (worker is { WorkerReportsProgress: true }) {
                        worker.ReportProgress(100 * i / markedObjects.Count / arg.Paths.Length);
                    }
                }

                if (arg.FixSv) {
                    // Reconstruct SliderVelocity (stolen from completionator)
                    List<TimingPointsChange> timingPointsChanges = new List<TimingPointsChange>();
                    // Add Hitobject stuff
                    foreach (HitObject ho in beatmap.HitObjects) {
                        // SliderVelocity changes
                        if (ho.IsSlider) {
                            if (markedObjects.Contains(ho) && arg.DelegateToBpm) {
                                var tpAfter = timing.GetRedlineAtTime(ho.Time).Copy();
                                var tpOn = tpAfter.Copy();

                                tpAfter.Offset = ho.Time;
                                tpOn.Offset = ho.Time - 1; // This one will be on the slider

                                tpAfter.OmitFirstBarLine = true;
                                tpOn.OmitFirstBarLine = true;

                                // Express velocity in BPM
                                tpOn.MpB *= ho.SliderVelocity / -100;
                                // NaN SV results in removal of slider ticks
                                ho.SliderVelocity = arg.RemoveSliderTicks ? double.NaN : -100;

                                // Add redlines
                                timingPointsChanges.Add(new TimingPointsChange(tpOn, mpb: true, unInherited: true,
                                    omitFirstBarLine: true, fuzzyness: Precision.DoubleEpsilon));
                                timingPointsChanges.Add(new TimingPointsChange(tpAfter, mpb: true, unInherited: true,
                                    omitFirstBarLine: true, fuzzyness: Precision.DoubleEpsilon));

                                ho.Time -= 1;
                            }

                            TimingPoint tp = ho.TimingPoint.Copy();
                            tp.Offset = ho.Time;
                            tp.MpB = ho.SliderVelocity;
                            timingPointsChanges.Add(new TimingPointsChange(tp, mpb: true,
                                fuzzyness: Precision.DoubleEpsilon));
                        }
                    }

                    // Add the new SliderVelocity changes
                    TimingPointsChange.ApplyChanges(timing, timingPointsChanges);
                }

                // Save the file
                editor.SaveFile();
            }

            // Complete progressbar
            if (worker is {WorkerReportsProgress: true}) worker.ReportProgress(100);

            // Do stuff
            RunFinished?.Invoke(this, new RunToolCompletedEventArgs(true,  arg.Reload, arg.Quick));

            return arg.Quick ? string.Empty : $"已为 {slidersTumourated} 个滑条添加凸点！";
        }

        public TumourGeneratorVm GetSaveData() {
            foreach (var tumourLayer in ViewModel.TumourLayers) {
                tumourLayer.Freeze();
            }

            return ViewModel;
        }

        public void SetSaveData(TumourGeneratorVm saveData) {
            if (saveData.TumourLayers.Count == 0) {
                // Make sure there is always at least one tumour layer
                saveData.TumourLayers.Add(TumourLayer.GetDefaultLayer());
            }
            DataContext = saveData;
            ViewModel.RegeneratePreview();
        }
        
        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "tumourgeneratorproject.json");

        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Tumour Generator Projects");

        public void QuickRun() {
            var currentMap = IOHelper.GetCurrentBeatmapOrCurrentBeatmap();

            RunTool(new[] { currentMap }, true, true);
        }

        public event EventHandler RunFinished;

        private void LayerNameBox_OnPreviewMouseDown(object sender, MouseButtonEventArgs e) {
            if (sender is TextBox { Tag: TumourLayer t}) {
                ViewModel.CurrentLayer = t;
            }
        }
    }
}
