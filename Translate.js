const {translateRepo} = require("./TranslationLib");

async function translate() {
    const skipPaths = [".git", ".github"];
    await translateRepo("./Mapping_Tools",  "./Translations/translate.json", "./Mapping_Tools_CN", skipPaths );
}

console.log("正在翻译项目...");

translate();
