const { createTemplate } = require("./TranslationLib");

async function create() {
    const extraTemplate = [
        {
            filePath: "./Mapping_Tools\\Mapping_Tools\\Views\\SnappingTools\\SnappingToolsView.xaml",
            extraText: ["This tool derives virtual points, lines and circles from the hit objects in your editor and overlays them over the editor. "]
        },
        // 替换版本号
        {
            filePath: "./Mapping_Tools\\Mapping_Tools\\Mapping_Tools.csproj",
            extraText: ["1.12.19"]
        }
    ]

    const skipPaths = [".git", ".github", "lib",
        ".editorconfig", ".gitattributes", ".gitignore", "LICENCE", "README.md",
        ".sln", ".DotSettings", ".iss", ".resx", ".csproj", ".config",
        "Mapping_Tools_Tests", "Mapping_Tools\\Data", "Mapping_Tools\\Classes\\MathUtil",

        "Mapping_Tools\\Mapping_Tools\\Properties\\Annotations.cs",
        "Mapping_Tools\\Mapping_Tools\\Properties\\Resources.Designer.cs",
        "Mapping_Tools\\Mapping_Tools\\Properties\\Settings.Designer.cs",
        "Mapping_Tools\\Mapping_Tools\\Properties\\Settings.settings",

        "Mapping_Tools\\Classes\\BeatmapHelper\\Beatmap.cs",
        "Mapping_Tools\\Classes\\BeatmapHelper\\BeatDivisors\\RationalBeatDivisor.cs",
        "Mapping_Tools\\Classes\\BeatmapHelper\\BeatmapEditor.cs",
        "Mapping_Tools\\Classes\\BeatmapHelper\\Editor.cs",
        "Mapping_Tools\\Classes\\BeatmapHelper\\Events\\Event.cs",
        "Mapping_Tools\\Classes\\BeatmapHelper\\FileFormatHelper.cs",
        "Mapping_Tools\\Classes\\BeatmapHelper\\SliderPathStuff\\PathApproximator.cs",
        "Mapping_Tools\\Classes\\BeatmapHelper\\Storyboard.cs",
        "Mapping_Tools\\Classes\\BeatmapHelper\\TimelineObject.cs",
        "Mapping_Tools\\Classes\\BeatmapHelper\\TValue.cs",
        "Mapping_Tools\\Classes\\HitsoundStuff\\CustomIndex.cs",
        "Mapping_Tools\\Classes\\HitsoundStuff\\SampleGeneratingArgs.cs",
        "Mapping_Tools\\Classes\\HitsoundStuff\\SamplePackage.cs",
        "Mapping_Tools\\Classes\\HitsoundStuff\\SampleSchema.cs",
        "Mapping_Tools\\Classes\\SystemTools\\Hotkey.cs",
        "Mapping_Tools\\Classes\\ToolHelpers\\Sliders\\BezierConverter.cs",
        "Mapping_Tools\\Mapping_Tools\\Classes\\HitsoundStuff\\HitsoundExporter.cs",
        "Mapping_Tools\\Mapping_Tools\\Classes\\HitsoundStuff\\LayerImportArgs.cs",
        "Mapping_Tools\\Mapping_Tools\\Classes\\HitsoundStuff\\MidiExporter.cs",
        "Mapping_Tools\\Mapping_Tools\\Classes\\HitsoundStuff\\Sample.cs",

    ];

    const skipTexts = [
        { filePath: "./Mapping_Tools\\Mapping_Tools\\App.xaml.cs", skipText: "\"crash-log.txt\"" },
        { filePath: "./Mapping_Tools\\Mapping_Tools\\Classes\\BeatmapHelper\\HitObject.cs", skipText: "\"slide\"" },
        { filePath: "./Mapping_Tools\\Mapping_Tools\\Classes\\BeatmapHelper\\HitObject.cs", skipText: "\"whistle\"" },
        { filePath: "./Mapping_Tools\\Mapping_Tools\\Classes\\BeatmapHelper\\HitObject.cs", skipText: "\"tick\"" },
        { skipText: "\"gdi32.dll\"" },
        { skipText: "\".wav\"" },
        { skipText: "\".ogg\"" },
        { skipText: "\".mp3\"" },
        { skipText: "\"*.*\"" },
        { filePath: "./Mapping_Tools\\Mapping_Tools\\Classes\\HitsoundStuff\\HitsoundImporter.cs", skipText: "\"SB: \"" },
        { filePath: "./Mapping_Tools\\Mapping_Tools\\Classes\\HitsoundStuff\\HitsoundImporter.cs", skipText: "\"hitnormal\"" },
        { filePath: "./Mapping_Tools\\Mapping_Tools\\Classes\\HitsoundStuff\\HitsoundImporter.cs", skipText: "\"hitwhistle\"" },
        { filePath: "./Mapping_Tools\\Mapping_Tools\\Classes\\HitsoundStuff\\HitsoundImporter.cs", skipText: "\"hitfinish\"" },
        { filePath: "./Mapping_Tools\\Mapping_Tools\\Classes\\HitsoundStuff\\HitsoundImporter.cs", skipText: "\"hitclap\"" },
        { filePath: "./Mapping_Tools\\Mapping_Tools\\Classes\\HitsoundStuff\\HitsoundImporter.cs", skipText: "\"^(normal|soft|drum)-(hit(normal|whistle|finish|clap)|slidertick|sliderslide)\"" },
        { filePath: "./Mapping_Tools\\Mapping_Tools\\Classes\\HitsoundStuff\\HitsoundImporter.cs", skipText: "\"Format {mf.FileFormat}, \"" },
        { filePath: "./Mapping_Tools\\Mapping_Tools\\Classes\\HitsoundStuff\\HitsoundImporter.cs", skipText: "\"Tracks {mf.Tracks}, \"" },
        { filePath: "./Mapping_Tools\\Mapping_Tools\\Classes\\HitsoundStuff\\HitsoundImporter.cs", skipText: "\"Delta Ticks Per Quarter Note {mf.DeltaTicksPerQuarterNote}\"" },
        { filePath: "./Mapping_Tools\\Mapping_Tools\\Classes\\HitsoundStuff\\HitsoundImporter.cs", skipText: "\"Percussion\"" },
        { filePath: "./Mapping_Tools\\Mapping_Tools\\Classes\\HitsoundStuff\\HitsoundImporter.cs", skipText: "\"Undefined\"" },
        { skipText: "\"yyyy-MM-dd HH-mm-ss\"" },
        { skipText: "\"__\"" },
        { skipText: "\"temp.osu\"" },
        { filePath: "./Mapping_Tools\\Mapping_Tools\\Classes\\SystemTools\\ListenerManager.cs", skipText: "\"PB\"" },
        { filePath: "./Mapping_Tools\\Mapping_Tools\\Classes\\SystemTools\\ListenerManager.cs", skipText: "\"user32.dll\"" },
        { skipText: "\"osu!\"" },
        { filePath: "./Mapping_Tools\\Mapping_Tools\\Classes\\SystemTools\\ListenerManager.cs", skipText: "\"^{L 10}\"" },
        { filePath: "./Mapping_Tools\\Mapping_Tools\\Classes\\SystemTools\\ListenerManager.cs", skipText: "\"{ENTER}\"" },
        { skipText: "\"*.osu\"" },
        { skipText: "\"<Current Tool>\"" },
        { filePath: "./Mapping_Tools\\Mapping_Tools\\Classes\\SystemTools\\SettingsManager.cs", skipText: "\"config.json\"" },
        { skipText: "\"RootDialog\"" },
        { skipText: "\"ToggleButton\"" },
        { skipText: "\"explorer.exe\"" },




    ];
    await createTemplate("./Mapping_Tools", "./Translations/translate.json", extraTemplate, skipPaths, skipTexts);
}

console.log("正在抽取项目字符串...");

create();