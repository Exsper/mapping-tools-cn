const {createTemplate} = require("./TranslationLib");

async function create() {
    const skipPaths = [".git", ".github", "lib",
        ".editorconfig", ".gitattributes", ".gitignore", "LICENCE", "README.md",
        ".sln", ".DotSettings", ".iss", ".resx", ".csproj", ".config",
        "Mapping_Tools_Tests", "Mapping_Tools\\Data", "Mapping_Tools\\Classes\\MathUtil",

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
    ];
    const skipTexts = [
        // {filePath: "./source-repo/test/index.js", skipText: `"test"`}
    ];
    await createTemplate("./Mapping_Tools", "./Translations/translate.json", skipPaths, skipTexts);
}

create();