# Mapping Tools 汉化工程

翻译进度：基本词条逐项翻译 ![进度](https://geps.dev/progress/70)

🚧**本汉化项目尚未完成，目前翻译进度：1433 个条目，占比 70.31% ，还有 605 个条目未翻译**🚧

汉化 [Mapping_Tools](https://github.com/OliBomby/Mapping_Tools) 专用项目，非汉化问题请转到原英文项目。

考虑到 Mapping_Tools 更新频繁且无官方语言切换模块，故制作此项目

使用翻译表修改项目文件字符串的方式进行翻译，解决因原项目更新导致需重复翻译的问题

## 目前进度

▶ 基本词条逐项翻译 ![进度](https://geps.dev/progress/70)

⏱ 细节词条补充翻译（主要处理下拉菜单内容，尽量翻译且不影响原功能）

⏱ 翻译项目功能检查

⏱ 汉化内容校对修正

⏱ 发布正式汉化版本

⏱ 后续版本跟进翻译

## 环境需求

- node.js

- dotnet 5.0

## 文件说明

- 文件夹 Mapping_Tools 为原项目文件夹

- 文件夹 Mapping_Tools_CN 为翻译后的项目文件夹

- 文件 BuildTranslationJson.js 用于制作和更新翻译JSON文件

- 文件 Translations/translate.json 生成的JSON翻译表

- 文件 Translate.js 用于生成汉化后的项目

## 汉化步骤

1. 更新原项目

   使用git更新Mapping_Tools

2. 生成翻译JSON

   使用nodejs运行BuildTranslationJson.js，或直接运行“2. 生成翻译表.ps1”

   **会自动继承现有翻译内容，不用担心原项目更新后丢失原有翻译**

3. 汉化文本

   修改 Translations 文件夹下的 translate.json

4. 生成项目

   使用nodejs运行Translate.js，或直接运行“4. 生成翻译项目.ps1”，生成Mapping_Tools_CN项目文件

5. 生成程序

   在Mapping_Tools_CN中生成程序并发布，或直接运行“5. 生成程序.ps1”

## 注意事项

- 因为使用翻译表（JSON文件）替换字符串的方式进行项目翻译，所以翻译表带有引号和格式，在翻译时请务必保留原格式
