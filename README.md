# AeroNovel

自用小说工具链。在普通.txt文本文档（一行一个段落的小说文本）的基础上增加一系列标记，直观地编辑文本，并生成多种目标格式。包括用于转换文本的命令行工具和提供着色等功能的VSCode插件。

## 使用命令行工具 AeroNovelTool
一些预想的使用场景：
+ 将一般txt文档转换为一些网站用的行内HTML。可以利用类似bbcode的标记改变颜色、加粗、注音。
+ 将生肉EPUB转换为对照稿txt，完成后使用命令行工具转化为txt(bbcode)或HTML。
+ 将生肉EPUB转换为对照稿，将文件夹视作一个项目，准备目录、元数据、图片、图床链接等信息，应对多种发布格式（bbcode、HTML、EPUB）。

提供一些常用命令的批处理 (*.bat) 文件，把相应的文件夹或文件拖到.bat上即可使用，不必关心命令行选项。

## VSCode
### VScode插件
提供功能：
+ 给文本着色
+ 几个右键搜索命令。
+ `↓`键跳过##标记的行。

将整个`Aeronoveltxt-vscode`文件夹放入`C:\Users\username\.vscode\extensions`，重启VSCode即可。

### VSCode设置
建议设置一下，否则问题比较多。默认设置下有几个问题：
+ 跳出来补全
+ Word Warp比较诡异

请参考代码中`vscode-settings.json`的相关设置，设置全局项目，或在项目文件夹设定`\.vscode\settings.json`。

简易项目文件夹设定指南：
把`vscode-settings.json`重命名，放到工作目录。
比如，你的内容放在`E:\novel\`里，使用VSCode的“打开文件夹”(Open Folder)打开`E:\novel\`，那么把`vscode-settings.json`改成`settings.json`，放到`E:\novel\.vscode\settings.json`。可以编辑这个文件，修改`editor.wordWrapColumn`以调整一行有多少字，其他字体、字号也可以改。
