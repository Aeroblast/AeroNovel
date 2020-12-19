using System;
using System.IO;
using System.Collections.Generic;
using AeroEpubViewer.Epub;
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
                            if (args[i] == "t2s")
                                gen = new AeroNovelEpub.GenEpub(AeroNovelEpub.ChineseConvertOption.T2S);

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
                            case "BlackTranslatingMagic":
                                e2c.castBlackTranslatingMagic = true;
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
epub2comment 【生肉epub文件】
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
    public static string filename_reg = "([0-9][0-9])(.*?)\\.[a]{0,1}txt";
}

