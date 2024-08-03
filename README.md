# mapping-tools-cn

汉化 [Mapping_Tools](https://github.com/OliBomby/Mapping_Tools) 专用项目

考虑到 Mapping_Tools 更新频繁且无官方语言切换模块，故制作此项目

## 文件说明

- 文件夹 Mapping_Tools 为原项目文件夹

- 文件夹 Mapping_Tools_CN 为翻译后的项目文件夹

- 文件 BuildTranslationJson.js 用于制作和更新翻译JSON文件

- 文件 Translate.js 用于生成汉化后的项目

## 汉化步骤

1. 更新原项目

   使用git更新Mapping_Tools

2. 生成翻译JSON

   使用nodejs运行BuildTranslationJson.js

   会自动继承现有翻译内容，不用担心原项目更新后丢失原有翻译

3. 汉化文本

   修改 Translations 文件夹下的 translate.json

4. 生成项目

   使用nodejs运行Translate.js，生成Mapping_Tools_CN项目文件

5. 生成程序

   在Mapping_Tools_CN中生成程序并发布
