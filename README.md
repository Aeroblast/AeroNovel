# Aeronovel

自用小说工具链，在txt的基础上使用一种类似bbcode的标记。包括VSCode着色插件，工具用于生成epub、Discuz帖子用bbcode、txt和其他辅助功能。

## 写法

### 文件夹结构

一本小说放入一个文件夹处理。文件夹有以下内容：

* Images 文件夹
    * cover.jpg 
    * *.jpg / *.png 其他图片资源
* *.atxt / *.txt 主要内容物。文本文档必须以两位数字开头。数字后面的文本将作为章节标题，可以用URL转码表示。具体格式见下文。
* toc.txt 必须。定义目录。具体格式见下文。
* meta.txt 必须。包含元数据，生成epub时将会大部分照搬。模板{urn:uuid}可以生成一个uuid。
* style.css 可选。定义样式。
* patch_t2s 文件夹。可选。用于繁体转换的特殊规则。
    * Images 文件夹。可选。内容为替换用的图片。
    * patch.csv 可选。内容为特殊情况的替换文本。

索引文本时，以文本文件的开头数字为索引和顺序。

### 文本文件

文件以.atxt结尾，就是plain text加标记。以.txt结尾也会被工具处理，只是vscode插件不会给高亮。

在生成epub时，生成XHTML文件会尝试使用便于理解的文件名，尝试将中文数字转换为阿拉伯数字，以及常用的标题转换，比如“序章”会变成“prologue”。纯英文文件名会照搬。命名为info的文档将会被加入生成信息，日期。

以 行 为单位进行处理，一行转换为一个段落。具体可以参考atxt-format.md。

### toc.txt

该文件将会用于生成EPUB的目录。一行一个目录项，使用数字即可表示链接目标。

需要层级表示时，以`[名称]章节索引`为标题开始添加子层级项目。比如`[正文]10`；以`[/]`单独一行返回上一级。`/`后面写点别的也不影响，比如`[/正文]`便于编辑。

### meta.txt

可以随便找个epub的opf文件看看opf的metadata部分。特殊处理：写{urn:uuid}将会生成一个id放进去。建议固定指定cover.jpg为封面。

### 繁体转简体

这部分比较复杂而且没人用，真的有需求就自己看源码吧（

## VSCode

### 插件
VSCode插件，可以给文本着色。另有几个右键搜索命令。
将整个`Aeronoveltxt-vscode`文件夹放入`C:\Users\username\.vscode\extensions`，重启VSCode即可。

### VSCode设置
建议设置一下，否则问题比较多。默认设置下有几个问题：
+ 跳出来补全
+ Word Warp比较诡异

可以在工作目录下设置仅目录范围，或在vscode的user settings设置全局项目。

建议把`vscode-settings.json`重命名，放到工作目录，即`\.vscode\settings.json`。


## AeroNovelTool

主要的生成工具。含有以下命令：

`epub 文件夹 [t2s]`在运行目录生成epub。EPUB采用EPUB2，文件夹结构类似Sigil。
* 文件夹：存放小说的文件夹，目录内容参考上一节。
* t2s ：可选选项。写了会运行繁体转简体。

`txt 文件夹`在运行目录生成单一的txt。

`bbcode 文件夹`在运行目录生成bbcode，以前是按章节，现在是使用index系统。各种论坛细微设定很乱，有错没办法（摊手）

`epub2comment epub文件`在运行目录生成txt，输出在文件夹。将epub内容按xhtml生成txt，每个段落生成一行`##`注释，然后加一行空行。用于翻译。

`epub2atxt epub文件` 在运行目录生成txt，输出在文件夹。将epub内容按xhtml生成txt，每个段落生成一行文本。用于重新校对。

`html2comment`

`atxt2bbcode`

下载类功能暂时变动中




