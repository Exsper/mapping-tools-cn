const fs = require("fs").promises;
const path = require("path");

function getContentBetweenQuotes(str) {
    let lines = str.split("\r\n");
    // 排除注释
    lines = lines.filter((line) => !(line.trim().startsWith("//")));
    // 排除Debug
    lines = lines.filter((line) => !(line.trim().startsWith("Console.WriteLine")));



    let match = [];
    lines.map((line, index) => {
        // 匹配 $"...{}..." 使用贪婪匹配模式
        let quoteRegex1 = /\$"(.*)"|`(.*)`/g;
        let m1 = line.match(quoteRegex1);
        let _line = line;
        // 删除已匹配字符
        if (m1) m1.map((ma) => {
            _line = _line.replaceAll(ma, "");
        });
        // 普通引号匹配
        let quoteRegex2 = /"([^"]*)"|`([^`]*)`/g;
        let m2 = _line.match(quoteRegex2);

        let m = [];
        if (m1) m.push(...m1);
        if (m2) m.push(...m2);
        if (m.length <= 0) return;

        // 排除一个字或者没有字
        m = m.filter((text) => (text.length > 3));
        // 排除 name="" 或 cref="" 或 href="" 的情况
        m = m.filter((text) => !(line.includes("name=" + text) || line.includes("cref=" + text) || line.includes("href=" + text)));
        // 排除 case "xxx" 的情况
        m = m.filter((text) => !(line.includes("case " + text)));
        // 排除 if语句 的情况
        // m = m.filter((text) => !(line.includes("== " + text) || line.includes("!= " + text)));
        // 排除 索引 的情况
        m = m.filter((text) => !(line.includes("[" + text + "]")));

        match.push(...m);
    });

    return Array.from(new Set(match));
}

function getContentInXaml(str) {
    let match = [];
    // ToolTip
    let quoteRegex = /ToolTip="([^"]*)"/g;
    let results = str.match(quoteRegex);
    if (results) match.push(...results);
    // Header
    quoteRegex = /Header="([^"]*)"/g;
    results = str.match(quoteRegex);
    if (results) match.push(...results);
    // Text
    quoteRegex = /Text="([^"]*)"/g;
    results = str.match(quoteRegex);
    if (results) match.push(...results);
    // ComboBoxItem Content
    quoteRegex = /Content="([^"]*)"/g;
    results = str.match(quoteRegex);
    if (results) match.push(...results);
    // materialDesign:HintAssist.Hint
    quoteRegex = /materialDesign:HintAssist\.Hint="([^"]*)"/g;
    results = str.match(quoteRegex);
    if (results) match.push(...results);
    // TextBlock
    results = str.split("\n").map((line) => line.trim()).filter((line) => /^[^<](.*)[^>]$/.test(line)).filter((line) => !/[a-zA-Z]+="(.+)"/.test(line));
    if (results.length > 0) match.push(...results);
    results = str.split("\n").map((line) => line.trim()).filter((line) => /<TextBlock>([^<>]*)<\/TextBlock>/.test(line));
    if (results) match.push(...results);
    // FontFamily
    quoteRegex = /FontFamily="([^"]*)"/g;
    results = str.match(quoteRegex);
    if (results) match.push(...results);
    quoteRegex = /TextElement.FontWeight="([^"]*)"/g;
    results = str.match(quoteRegex);
    if (results) match.push(...results);

    // 当文字长度过短而且没有引号和空格时，为了防止误替换，附带前面几个空格
    match = match.map((text) => {
        if (text.indexOf("`") < 0 && text.indexOf("\"") < 0 && text.length < 10) {
            return "    " + text;
        }
        else return text;
    });

    return Array.from(new Set(match));
}

async function readFile(filePath) {
    try {
        const data = await fs.readFile(filePath, 'utf8');
        return data;
    } catch (err) {
        console.error("无法读取文件：" + filePath + "\n", err);
        return null;
    }
}

/**
 * @param {string} filepath 
 * @param {Array<string>} skipPaths 
 * @returns 
 */
function isPathInSkip(filepath, skipPaths) {
    return skipPaths.some((skipPath) => filepath.endsWith(skipPath));
}

async function buildFileList(folder, skipPaths = []) {
    let filePaths = [];
    try {
        const files = await fs.readdir(folder);
        for (let i = 0; i < files.length; i++) {
            let filePath = "./" + path.join(folder, files[i]);
            let stats = await fs.stat(filePath);
            if (stats.isDirectory()) {
                if (isPathInSkip(filePath, skipPaths)) continue;
                else filePaths.push(...await buildFileList(filePath, skipPaths));
            }
            else {
                if (isPathInSkip(filePath, skipPaths)) continue;
                else filePaths.push(filePath);
            }
        }
        return filePaths;
    }
    catch (err) {
        console.error("无法读取文件夹：" + folder + "\n", err);
        return [];
    }
}

/**
 * @param {string} orgfolder 
 * @param {Array<string>} skipPaths 忽略翻译的文件（夹）路径
 * @param {Array<{filePath:string, skipText:string}>} skipTexts [{filePath: 忽略文本所在文件，不提供则默认所有文件, skipText: 忽略文本，filePath中出现该文本则不加入翻译Json}, ...]
 */
async function buildTemplate(orgfolder, skipPaths = [], skipTexts = []) {
    let filePaths = await buildFileList(orgfolder, skipPaths);
    let template = {};
    let globalSkipTextsUnused = [];

    skipTexts.map((skipText) => {
        if (!skipText.filePath) globalSkipTextsUnused.push(skipText.skipText);
    });

    for (let i = 0; i < filePaths.length; i++) {
        try {
            let data = await readFile(filePaths[i]);
            let strings = (filePaths[i].endsWith(".xaml")) ? getContentInXaml(data) : getContentBetweenQuotes(data);
            if (!strings || strings.length <= 0) {
                // console.info(filePaths[i] + " 不含字符串");
                continue;
            }
            else {
                let skipTextsInThisFile = [];
                let skipTextsUnused = [];
                skipTexts.map((skipText) => {
                    if (!skipText.filePath) skipTextsInThisFile.push(skipText.skipText);
                    else {
                        let skipFilePath = "./" + path.join(skipText.filePath);
                        if (skipFilePath === filePaths[i]) {
                            skipTextsInThisFile.push(skipText.skipText);
                            skipTextsUnused.push(skipText.skipText);
                        }
                    }
                })

                let fileStrings = {};
                strings.map((string) => {
                    if (!skipTextsInThisFile.includes(string)) fileStrings[string] = string;
                    else {
                        skipTextsUnused = skipTextsUnused.filter(item => item !== string);
                        globalSkipTextsUnused = globalSkipTextsUnused.filter(item => item !== string);
                    }
                });
                if (Object.keys(fileStrings).length > 0) template[filePaths[i]] = fileStrings;
                if (skipTextsUnused.length > 0) console.log(filePaths[i] + " 的排除规则中多余 " + skipTextsUnused.join(", ") + " ，请检查");
            }
        }
        catch (err) {
            console.error("无法读取文件：" + filePaths[i] + "\n", err);
            continue;
        }
    }
    if (globalSkipTextsUnused.length > 0) console.log("全局字符串排除规则中多余 " + globalSkipTextsUnused.join(", ") + " ，请检查");
    return template;
}

/**
 * @param {string} orgfolder 
 * @param {string} outputPath 
 * @param {Array<{filePath:string, extraText:Array<string>}>} extraTemplate 额外翻译内容 [ {filePath: 翻译文本所在文件， extraText: [需要翻译的文本, ...] }, ...]
 * @param {Array<string>} skipPaths 忽略翻译的文件（夹）路径
 * @param {Array<{filePath:string, skipText:string}>} skipTexts [{filePath: 忽略文本所在文件，不提供则默认所有文件, skipText: 忽略文本，filePath中出现该文本则不加入翻译Json}, ...]
 */
async function createTemplate(orgfolder, outputPath, extraTemplate = [], skipPaths = [], skipTexts = []) {
    let template = await buildTemplate(orgfolder, skipPaths, skipTexts);
    let fileExist = false;
    // 融合extraTemplate
    for (let i = 0; i < extraTemplate.length; i++) {
        let key = extraTemplate[i].filePath;
        if (template[key] === undefined) {
            template[key] = {};
        }
        for (let j = 0; j < extraTemplate[i].extraText.length; j++) {
            let subkey = extraTemplate[i].extraText[j];
            if (template[key][subkey] === undefined) {
                template[key][subkey] = subkey;
            }
            else {
                console.warn("文件 " + key + " 中的额外翻译条目 " + subkey + " 已被自动提取，不需要手动添加。")
            }
        }
    }
    try {
        // 继承旧翻译
        fileExist = await fs.access(outputPath, fs.constants.F_OK);
        console.warn("已存在文件，将对原文件进行改动");
        let existData = JSON.parse(await fs.readFile(outputPath, 'utf8'));
        Object.keys(existData).forEach(key => {
            if (template[key] === undefined) {
                console.warn("文件 " + key + " 在新项目中不存在，将删除旧文件中的所有翻译条目");
            }
            else {
                Object.keys(existData[key]).forEach(subkey => {
                    if (template[key][subkey] === undefined) {
                        console.warn("文件 " + key + " 中的翻译条目 " + subkey + " 在新项目中不存在，将删除旧翻译条目");
                    }
                    else {
                        template[key][subkey] = existData[key][subkey];
                    }
                });
            }
        });
        // 数量统计
        let filecount = 0;
        let textcount = 0;
        let translatedcount = 0;
        Object.keys(template).forEach(path => {
            filecount++;
            Object.keys(template[path]).forEach(text => {
                textcount++;
                if (template[path][text] !== text) translatedcount++;
            });
        });
        try {
            await fs.writeFile(outputPath, JSON.stringify(template));
            console.log("已更新翻译文件：" + outputPath);
            console.log("目前共有 " + filecount + " 个文件，共计 " + textcount + " 个翻译条目！");
            console.log("当前已经翻译 " + translatedcount + " 个条目，占比 " + (translatedcount * 100 / textcount).toFixed(2) + "% ，还有 " + (textcount - translatedcount) + " 个条目未翻译");
        }
        catch (err) {
            console.error("无法更新翻译文件：" + outputPath + "\n", err);
        }
    }
    catch (ex) {
        try {
            await fs.writeFile(outputPath, JSON.stringify(template));
            console.log("已生成翻译文件：" + outputPath);
            console.log("目前共有 " + filecount + " 个文件， " + textcount + " 个翻译条目！");
        }
        catch (err) {
            console.error("无法创建翻译文件：" + outputPath + "\n", err);
        }
    }
}

async function emptyDistFolder(distfolder) {
    let filePaths = await fs.readdir(distfolder);
    for (let i = 0; i < filePaths.length; i++) {
        filePaths[i] = "./" + path.join(distfolder, filePaths[i]);
    }
    for (let i = 0; i < filePaths.length; i++) {
        let filePath = filePaths[i];
        try {
            let stats = await fs.stat(filePath);
            if (stats.isFile()) {
                // 删除文件
                // console.log("删除文件：" + filePath)
                await fs.unlink(filePath);
            } else {
                // 递归删除子文件夹
                await emptyDistFolder(filePath);
                // 删除文件夹
                // console.log("删除文件夹：" + filePath)
                await fs.rmdir(filePath);
            }

        }
        catch (err) {
            console.error("无法读取文件：" + filePaths[i] + "\n", err);
            continue;
        }
    }
}

async function translateRepo(orgfolder, transJsonPath, distfolder, skipPaths = []) {
    // 清理dist文件夹
    await emptyDistFolder(distfolder);
    // 读取翻译Json
    let existTranslation = null;
    try {
        existTranslation = JSON.parse(await fs.readFile(transJsonPath, 'utf8'));
    }
    catch (ex) {
        console.error("无法读取翻译文件：" + transJsonPath + "\n", err);
        console.warn("将直接复制原项目到目标文件夹");
    }
    let comboBoxTranslation = null;
    try {
        comboBoxTranslation = JSON.parse(await fs.readFile("./Translations/comboBoxTranslate.json", 'utf8'));
    }
    catch (ex) {
        console.error("无法读取combobox翻译文件：" + "./Translations/comboBoxTranslate.json" + "\n", err);
    }
    // 数量统计
    let textcount = 0;
    let translatedcount = 0;
    Object.keys(existTranslation).forEach(path => {
        Object.keys(existTranslation[path]).forEach(text => {
            textcount++;
            if (existTranslation[path][text] !== text) translatedcount++;
        });
    });
    console.log("当前已经翻译 " + translatedcount + " 个条目，占比 " + (translatedcount * 100 / textcount).toFixed(2) + "% ，还有 " + (textcount - translatedcount) + " 个条目未翻译");
    // 复制文件
    let filePaths = await buildFileList(orgfolder, skipPaths);
    for (let i = 0; i < filePaths.length; i++) {
        let destPath = "./" + path.join(distfolder, ...filePaths[i].split("\\").slice(1));
        try {
            // 确保目录存在
            try {
                await fs.access(path.dirname(destPath));
            }
            catch (e) {
                await fs.mkdir(path.dirname(destPath), { recursive: true });
            }
            if (existTranslation === null || (existTranslation[filePaths[i]] === undefined && comboBoxTranslation[filePaths[i]] === undefined)) {
                // 直接复制文件
                await fs.copyFile(filePaths[i], destPath);
                continue;
            }
            // 读取文件
            let data = await readFile(filePaths[i]);
            // 翻译文本
            if (existTranslation[filePaths[i]]) {
                Object.keys(existTranslation[filePaths[i]]).forEach(subkey => {
                    let transText = existTranslation[filePaths[i]][subkey];
                    // 因为 MainWindow.xaml.cs 内 UpdateManager 和 制作者名单 都出现了"OliBomby"
                    // 而更新器的地址需要更改为本项目，需要将UpdateManager的参数改为本人Github用户名
                    // 故需要防止在翻译 MainWindow.xaml.cs 时将制作者名单中的"OliBomby"也替换掉
                    if (filePaths[i] === "./Mapping_Tools\\Mapping_Tools\\MainWindow.xaml.cs" && subkey === "\"OliBomby\"")
                        data = data.replace(subkey, transText);
                    else data = data.replaceAll(subkey, transText);
                });
            }
            // combobox翻译
            if (comboBoxTranslation[filePaths[i]]) {
                Object.keys(comboBoxTranslation[filePaths[i]]).forEach(subkey => {
                    let transText = comboBoxTranslation[filePaths[i]][subkey];
                    data = data.replaceAll(subkey, transText);
                });
            }
            // 写入文件
            await fs.writeFile(destPath, data, "utf8");
        }
        catch (err) {
            console.error("无法写入文件：从 " + filePaths[i] + " 到 " + destPath + "\n", err);
            continue;
        }
    }
    console.log("工作完成！翻译后的项目： " + distfolder);
}

module.exports.buildTemplate = buildTemplate;
module.exports.createTemplate = createTemplate;
module.exports.emptyDistFolder = emptyDistFolder;
module.exports.translateRepo = translateRepo;
