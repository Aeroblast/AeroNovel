using System;
using System.IO;

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
                        if (args.Length >= 3)
                            if (args[2] == "t2s")
                                gen = new AeroNovelEpub.GenEpub(AeroNovelEpub.ChineseConvertOption.T2S);

                        Epub e = gen.Gen(args[1]);
                        e.filename = "[" + e.creator + "] " + e.title;
                        e.Save("");
                    }
                    break;
                case "txt":
                    if (!DirectoryExist(args[1])) return;
                    GenTxt.Gen(args[1]);
                    break;
                case "bbcode":
                    if (!DirectoryExist(args[1])) return;
                    GenBbcode.Gen(args[1]);
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
                        var atxt = Html2Comment.ProcXHTML(xhtml.data);
                        File.WriteAllText("output_kakuyomu2comment.txt", atxt);
                        Log.log("[Info]output_kakuyomu2comment.txt");
                    }
                    break;
                case "websrc":
                    {
                        TextItem[] xhtmls;
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
    static void Save(TextItem i, string dir)
    {
        string p = dir + "/" + i.fullName;
        File.WriteAllText(p, i.data);
        Log.log("[Info]Saved:" + p);
    }
}
public class AeroNovel
{
    public static string filename_reg = "([0-9][0-9])(.*?)\\.[a]{0,1}txt";
}

