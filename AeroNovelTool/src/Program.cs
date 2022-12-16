using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AeroEpub.Epub;
class Program
{
    static void Main(string[] args)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("AeroNovelTool Ver." + Version.date);
        Console.WriteLine("https://github.com/Aeroblast/AeroNovel");
        Console.ForegroundColor = ConsoleColor.White;
        if (args.Length >= 2)
        {
            switch (args[0].ToLower())
            {
                case "epub":
                    {
                        if (!DirectoryExist(args[1])) return;
                        var gen = new AeroNovelEpub.GenEpub(args[1]);
                        EpubFile e = gen.Gen();
                        string dateString = DateTime.Today.ToString("yyyyMMdd");
                        var creators = e.dc_creators.Where(x => x.refines.Find(r => r.name == "role" && r.value == "aut") != null).Select(x => x.value).ToArray();
                        try
                        {
                            var modifiedDate = e.meta.Find(x => x.name == "dcterms:modified");
                            if (modifiedDate != null) dateString = modifiedDate.value.Replace("-", "").Substring(0, 8);
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
                case "reviewepub":
                    {
                        if (!DirectoryExist(args[1])) return;
                        var gen = new AeroNovelEpub.GenReviewEpub();

                        EpubFile e = gen.Gen(args[1]);
                        string dateString = DateTime.Today.ToString("yyyyMMdd");
                        var creators = e.dc_creators.Where(x => x.refines.Find(r => r.name == "role" && r.value == "aut") != null).Select(x => x.value).ToArray();
                        try
                        {
                            var modifiedDate = e.meta.Find(x => x.name == "dcterms:modified");
                            if (modifiedDate != null) dateString = modifiedDate.value.Replace("-", "").Substring(0, 8);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            Log.Warn("Error at getting modified date in metadata");
                        }

                        e.filename = $"[{string.Join(",", creators)}] {e.title} [{dateString}][Review]";
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
                    {
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
                            var outputFile = $"output_bbcode_{Path.GetFileNameWithoutExtension(args[1])}.txt";
                            if (outputPath != "")
                            {
                                outputFile = Path.Combine(outputPath, outputFile);
                            }
                            GenBbcode.ConvertFile(args[1], outputFile);
                            break;
                        }
                        if (Directory.Exists(args[1]))
                        {
                            var outputFile = $"output_bbcode_{Path.GetFileNameWithoutExtension(args[1])}.txt";
                            if (outputPath != "")
                            {
                                outputFile = Path.Combine(outputPath, outputFile);
                            }
                            GenBbcode.ConvertDir(args[1], outputFile);
                            break;
                        }
                        Log.Warn("Nothing happens. Make sure there is a file or folder to process.");

                    }
                    break;
                case "inlinehtml":
                    {
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
                            GenInlineHTML.ConvertFile(args[1], outputFile);
                            break;
                        }
                        if (Directory.Exists(args[1]))
                        {
                            if (outputPath == "")
                                outputPath = "output_inlineHTML_" + Path.GetFileName(args[1]);
                            Directory.CreateDirectory(outputPath);
                            GenInlineHTML.ConvertDir(args[1], outputPath);
                            break;
                        }
                        Log.Warn("Nothing happens. Make sure there is a file or folder to process.");
                    }
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
                case "verifyhistory":
                    Log.Note("Verify raw untouched on Git history");
                    if (Directory.Exists(args[1]))
                    {
                        Log.Note("Project: " + Path.GetFileName(args[1]));
                        var project = new AtxtProject(args[1], false);
                        project.CollectSource();
                        project.srcs.ForEach(s => s.GitVerifyRawUntouchedOnHistory());
                    }
                    else
                    {
                        Log.Error("The directroy doesn't exist: " + Path.GetFileName(args[1]));
                    }
                    break;
                case "verifyuncommitted":
                    Log.Note("Verify raw untouched on uncommitted Git changes.");
                    if (Directory.Exists(args[1]))
                    {
                        Log.Note("Project: " + Path.GetFileName(args[1]));
                        var project = new AtxtProject(args[1], false);
                        project.CollectSource();
                        project.srcs.ForEach(s => s.GitVerifyRawUntouchedOnUncommitted());
                    }
                    else
                    {
                        Log.Error("The directroy doesn't exist: " + Path.GetFileName(args[1]));
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
inlinehtml　【atxt或项目文件夹】 【输出目录(可选)】
epub2comment 【生肉epub文件】 【可选选项...】
　　选项'Glossary' 【名词表文件】
　　选项'BlackTranslationMagic' 
atxtcc 【txt文件】 【't2s'】
analyze 【项目文件夹】
reviewepub 【项目文件夹】 【输出目录(可选)】
verifyhistory 【项目文件夹】
verifyuncommitted 【项目文件夹】
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
    public static string regStr_filename = "^([0-9][0-9])(.*?)\\.[a]{0,1}txt$";
    public static string regStr_filename_xhtml = "^([0-9][0-9])(.*?)\\.xhtml$";
    public static string regStr_filename_noext = "^([0-9][0-9])(.*)";
    public static bool isIndexedTxt(string path)
    {
        return Regex.Match(Path.GetFileName(path), regStr_filename).Success;
    }

}