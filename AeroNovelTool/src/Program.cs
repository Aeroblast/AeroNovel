using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AeroEpub.Epub;
class Program
{
    static void Main(string[] args)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("AeroNovelTool by AE Ver." + Version.date);
        Console.ForegroundColor = ConsoleColor.White;
        if (args.Length >= 2)
        {
            switch (args[0].ToLower())
            {
                case "epub":
                    {
                        if (!DirectoryExist(args[1])) return;
                        var gen = new AeroNovelEpub.GenEpub();
                        for (int i = 2; i < args.Length; i++)
                        {
                            switch (args[i])
                            {
                                case "--t2s":
                                    gen.cc_option = AeroNovelEpub.ChineseConvertOption.T2S; break;
                                case "--no-info":
                                    gen.addInfo = ConfigValue.disable;
                                    break;
                                case "--no-indent-adjust":
                                    gen.indentAdjust = ConfigValue.disable;
                                    break;
                            }
                        }

                        EpubFile e = gen.Gen(args[1]);
                        List<string> creators = new List<string>();
                        string dateString = DateTime.Today.ToString("yyyyMMdd");
                        e.dc_creators.ForEach((x) =>
                        {
                            if (x.refines.Count > 0)
                            {
                                foreach (var refine in x.refines)
                                {
                                    if (refine.name == "role")
                                    {
                                        if (refine.value != "aut") return;
                                    }
                                }
                            }
                            creators.Add(x.value);
                        });
                        try
                        {
                            e.meta.ForEach((x) => { if (x.name == "dcterms:modified") dateString = x.value.Replace("-", "").Substring(0, 8); });
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            Log.Warn("Error at getting modified date in metadata");
                        }

                        e.filename = $"[{string.Join(",", creators)}] {e.title} [{dateString}]";
                        if (args.Length >= 3 && DirectoryExist(args[2]))
                            e.Save(args[2]);
                        else
                            e.Save("");
                    }
                    break;
                case "txt":
                    if (!DirectoryExist(args[1])) return;
                    GenTxt.Gen(args[1]);
                    break;
                case "bbcode":
                    if (!DirectoryExist(args[1])) return;
                    if (args.Length >= 3)
                        if (DirectoryExist(args[2]))
                        {
                            GenBbcode.output_path = Path.Combine(args[2], GenBbcode.output_path);
                            GenBbcode.output_path_single = Path.Combine(args[2], GenBbcode.output_path_single);
                        }
                    GenBbcode.GenSingle(args[1]);
                    break;
                case "epub2comment":
                    if (!FileExist(args[1])) return;
                    Epub2Comment e2c = new Epub2Comment(args[1]);
                    if (args.Length > 2)
                    {
                        switch (args[2])
                        {
                            case "BlackMagic-Cloud":
                                e2c.setTextTranslation = new BlackMagic_Cloud();
                                break;
                            case "BlackMagic-Dog":
                                e2c.setTextTranslation = new BlackMagic_Dog();
                                break;
                            case "Glossary":
                                if (args.Length > 3)
                                    e2c.glossaryDocPath = args[3];
                                else
                                    Log.Error("Should give glossary document.");
                                break;
                        }
                    }
                    e2c.Proc();
                    break;
                case "epub2atxt":
                    if (!FileExist(args[1])) return;
                    Epub2Atxt.Proc(args[1]);
                    break;
                case "html2comment":
                    if (!FileExist(args[1])) return;
                    Html2Comment.Proc(args[1]);
                    break;
                case "atxt2bbcode":
                    if (!FileExist(args[1])) return;
                    GenBbcode.Proc(args[1]);
                    break;
                case "kakuyomu2comment":
                    {
                        var xhtml = WebSource.KakuyomuEpisode(args[1]);
                        var atxt = Html2Comment.ProcXHTML(xhtml.text);
                        File.WriteAllText("output_kakuyomu2comment.txt", atxt);
                        Log.Note("output_kakuyomu2comment.txt");
                    }
                    break;
                case "websrc":
                    {
                        TextEpubItemFile[] xhtmls;
                        string dirname = "output_websrc_";
                        if (args[1].Contains("kakuyomu.jp"))
                        {
                            xhtmls = WebSource.KakuyomuAuto(args[1]);
                            dirname += "kakuyomu";
                        }
                        else
                        {
                            Log.Error("什么网站");
                            break;
                        }
                        if (xhtmls != null)
                        {
                            Directory.CreateDirectory(dirname);
                            foreach (var xhtml in xhtmls)
                                Save(xhtml, dirname);
                        }


                    }
                    break;
                case "atxtcc":
                    {
                        if (!FileExist(args[1])) return;
                        bool t2s = false, s2t = false, replace = false;
                        for (int i = 2; i < args.Length; i++)
                        {
                            switch (args[i])
                            {
                                case "t2s": t2s = true; break;
                                case "s2t": s2t = true; break;
                                case "replace": replace = true; break;
                            }
                        }
                        if (t2s)
                            AeroNovelEpub.AtxtChineseConvert.ProcT2C(args[1], replace);
                        else if (s2t)
                        {
                            //Not Implemented
                        }

                    }
                    break;
                case "atxt2inlinehtml":
                    var outputPath = "";
                    if (args.Length > 2)
                        if (DirectoryExist(args[2]))
                        {
                            outputPath = args[2];
                        }
                        else
                        {
                            break;
                        }
                    if (File.Exists(args[1]))
                    {
                        var outputFile = $"output_inlineHTML_{Path.GetFileNameWithoutExtension(args[1])}.txt";
                        if (outputPath != "")
                        {
                            outputFile = Path.Combine(outputPath, outputFile);
                        }
                        Atxt2InlineHTML.ConvertSave(args[1], outputFile);
                        break;
                    }
                    if (Directory.Exists(args[1]))
                    {
                        if (outputPath == "")
                            outputPath = "output_inlineHTML_" + Path.GetFileName(args[1]);
                        Directory.CreateDirectory(outputPath);
                        Atxt2InlineHTML.ConvertSaveDir(args[1], outputPath);
                        break;
                    }
                    Log.Warn("Nothing happens. Make sure there is a file or folder to process.");
                    break;
                case "analyze":
                    if (Directory.Exists(args[1]))
                    {
                        if (args.Length >= 3)
                        {
                            int chap = 0;
                            if (!int.TryParse(args[2], out chap))
                            {
                                Log.Error("Chapter number!");
                            }
                            Statistic.AnalyzeProject(args[1], chap);
                            break;
                        }
                        Statistic.AnalyzeProject(args[1]);
                        break;
                    }
                    break;
                default:
                    Log.Warn("Nothing happens. " + usage);
                    break;
            }
        }
        else
        {
            Log.Warn(usage);
        }
    }
    const string usage = @"Usage:
epub 【项目文件夹】 【输出目录(可选)】
bbcode 【项目文件夹】 【输出目录(可选)】
atxt2bbcode 【atxt文件】
epub2comment 【生肉epub文件】 【可选选项...】
　　选项'Glossary' 【名词表文件】
　　选项'BlackTranslationMagic' 
atxt2inlinehtml　【atxt或项目文件夹】
atxtcc 【txt文件】 【't2s'】
html2comment 【xhtml文件】
";
    static bool FileExist(string path)
    {
        if (File.Exists(path)) return true;
        Log.Error("File not exist:" + path);
        return false;
    }
    static bool DirectoryExist(string path)
    {
        if (Directory.Exists(path)) return true;
        Log.Error("Dir not exist:" + path);
        return false;
    }
    static void Save(TextEpubItemFile i, string dir)
    {
        string p = dir + "/" + i.fullName;
        File.WriteAllText(p, i.text);
        Log.Note("Saved:" + p);
    }
}
public class AeroNovel
{
    public static string regStr_filename = "([0-9][0-9])(.*?)\\.[a]{0,1}txt";
    public static string regStr_filename_xhtml = "([0-9][0-9])(.*?)\\.xhtml";
    public static string regStr_filename_noext = "([0-9][0-9])(.*)";
    public static bool isIndexedTxt(string path)
    {
        return Regex.Match(Path.GetFileName(path), regStr_filename).Success;
    }

}
public enum ConfigValue
{
    unset = 0,
    active = 1,
    disable = 2
}

public class ProjectConfig
{
    public List<JoinCommand> joinCommands = new List<JoinCommand>();
    public int joinBlankLine = 0;
    public ConfigValue indentAdjust;
    public ConfigValue addInfo;

    public ProjectConfig(string[] content)
    {
        foreach (var line in content)
        {
            var sep = line.IndexOf(':');
            if (sep < 0) continue;
            var cmd = line.Substring(0, sep);
            var arg = line.Substring(sep + 1);
            switch (cmd)
            {
                case "join":
                    joinCommands.Add(new JoinCommand(arg));
                    break;
                case "join_blank_line":
                    int.TryParse(arg, out joinBlankLine);
                    break;
                case "indent_adjust":
                    indentAdjust = GetConfigValue(arg); break;
                case "add_info":
                    addInfo = GetConfigValue(arg); break;
            }
        }
        joinCommands.Sort((c1, c2) => c1.start.CompareTo(c2.start));
    }
    ConfigValue GetConfigValue(string s)
    {
        s = s.ToLower();
        switch (s)
        {
            case "true":
            case "yes":
            case "1":
            case "active":
            case "enable":
                return ConfigValue.active;
            case "false":
            case "no":
            case "0":
            case "disable":
                return ConfigValue.disable;
            default:
                return ConfigValue.unset;

        }
    }
}
public class JoinCommand
{
    public string start, end;
    public bool used = false;
    public string title;
    static Regex regex = new Regex("([0-9]{2})-([0-9]{2})(.*)");

    public JoinCommand(string cmd)
    {
        var r = regex.Match(cmd);
        if (!r.Success) throw new Exception("Join Command Fail: " + cmd);
        start = r.Groups[1].Value;
        end = r.Groups[2].Value;
        title = r.Groups[3].Value;
    }

    public override string ToString()
    {
        return $"JoinCommand: {start}-{end} {title}";
    }
}

