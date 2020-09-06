using System;
using System.IO;
using System.Collections.Generic;
using AeroEpubViewer.Epub;
class Program
{
    static void Main(string[] args)
    {
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
                        e.meta.ForEach((x) => { if (x.name == "dcterms:modified") dateString = x.value.Replace("-", "").Substring(0, 8); });
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
                    Epub2Comment.Proc(args[1]);
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
                        Log.log("[Info]output_kakuyomu2comment.txt");
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
                            Log.log("[Error]什么网站");
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
                    Log.log("[Warn]Nothing happens. Usage:epub/txt/bbcode/epub2comment/epub2atxt/html2comment/atxt2bbcode");
                    break;
            }
        }
        else
        {
            Log.log("[Warn]Usage:epub/txt/bbcode/restore/epub2comment");
        }
    }
    static bool FileExist(string path)
    {
        if (File.Exists(path)) return true;
        Log.log("[Error]File not exist:" + path);
        return false;
    }
    static bool DirectoryExist(string path)
    {
        if (Directory.Exists(path)) return true;
        Log.log("[Error]Dir not exist:" + path);
        return false;
    }
    static void Save(TextEpubItemFile i, string dir)
    {
        string p = dir + "/" + i.fullName;
        File.WriteAllText(p, i.text);
        Log.log("[Info]Saved:" + p);
    }
}
public class AeroNovel
{
    public static string filename_reg = "([0-9][0-9])(.*?)\\.[a]{0,1}txt";
}

