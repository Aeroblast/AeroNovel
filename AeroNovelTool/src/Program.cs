using System;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length >= 2)
        {
            if (!Directory.Exists(args[1])&&!File.Exists(args[1]))
            {
                Console.WriteLine("not exsits:" + args[1]);
                return;
            }
            switch (args[0].ToLower())
            {
                case "epub":
                    {
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
                    GenTxt.Gen(args[1]);
                    break;
                case "bbcode":
                    GenBbcode.Gen(args[1]);
                    break;
                case "restore":
                    Publish.Restore(args[1]);
                    break;
                case "epub2comment":
                    Epub2Comment.Proc(args[1]);
                    break;
                case "epub2atxt":
                    Epub2Atxt.Proc(args[1]);
                    break;
                default:
                    Log.log("[Warn]Nothing happens. Usage:epub/txt/bbcode/restore/epub2comment");
                    break;
            }
        }
        else
        {
            Log.log("[Warn]Usage:epub/txt/bbcode/restore/epub2comment");
        }
    }
}
public class AeroNovel
{
    public static string filename_reg = "([0-9][0-9])(.*?)\\.[a]{0,1}txt";
}

