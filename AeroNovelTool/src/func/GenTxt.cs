using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO.Compression;

class GenTxt
{
    public static void Gen(string dir)
    {

        string txt = "";

        string[] files = Directory.GetFiles(dir);
        foreach (string f in files)
        {
            Match m = Regex.Match(Path.GetFileName(f), AeroNovel.filename_reg);
            if (!m.Success) continue;
            string no = m.Groups[1].Value;
            string chaptitle = m.Groups[2].Value;
            string[] lines = File.ReadAllLines(f);
            string body = Body(lines);
            txt += chaptitle + "\r\n" + body + "\r\n=====================\r\n";
            Console.WriteLine("Added " + chaptitle);

        }
        File.WriteAllText("txt_output.txt", txt);

    }
    public static string Body(string[] txt)
    {
                    const string reg_noteref = "\\[note\\]";
            const string reg_notecontent = "\\[note=(.*?)\\]";
            const string reg_img = "\\[img\\](.*?)\\[\\/img\\]";
            const string reg_illu = "\\[illu\\](.*?)\\[\\/illu\\]";
            const string reg_class = "\\[class=(.*?)\\](.*?)\\[\\/class\\]";
            const string reg_chapter = "\\[chapter=(.*?)\\](.*?)\\[\\/chapter\\]";
            Dictionary<string, string> reg_dic = new Dictionary<string, string>
            {
                {"\\[align=(.*?)\\](.*?)\\[\\/align\\]","$2"},
                {reg_noteref,"[注]"},
                {reg_notecontent,"[$1]"},
                {reg_img,"[图片：$1]"},
                {reg_illu,"[图片：$1]"},
                {reg_class,"　$2"},
                {reg_chapter,"$2"},
                {"\\[b\\](.*?)\\[\\/b\\]","$1"},
                {"\\[title\\](.*?)\\[\\/title\\]","$1"},
                {"\\[ruby=(.*?)\\](.*?)\\[\\/ruby\\]","$2（$1）"},
                {"\\[pagebreak\\]",""},
                {"/\\*.*?\\*/",""},
                {"///.*",""},
                {"\\[emphasis\\](.*?)\\[\\/emphasis\\]","$1"},
                {"\\[s\\](.*?)\\[\\/s\\]","$1"},
                {"\\[i\\](.*?)\\[\\/i\\]","$1"},
                {"\\[color=(.*?)\\](.*?)\\[\\/color\\]","$2"},
                {"\\[size=(.*?)\\](.*?)\\[\\/size\\]","$2"}
            };
        string html = "";
        foreach (string line in txt)
        {
            if(line.StartsWith("##"))continue;
            string r = line;
            bool aligned=false;
            Match m = Regex.Match("", "1");
            do
            {
                foreach (var kw in reg_dic)
                {
                    m = Regex.Match(r, kw.Key);
                    if (m.Success)
                    {
                        Regex reg = new Regex(kw.Key);
                        switch (kw.Key)
                        {
                            case "\\[align=(.*?)\\](.*?)\\[\\/align\\]"://align
                                r = reg.Replace(r, kw.Value);
                                switch (m.Groups[1].Value)
                                {
                                    case "right": r = "　　　　　　" + r; break;
                                    case "center": r = "　　　　" + r; break;
                                }
                                aligned=true;
                                break;
                            default:
                                r = reg.Replace(r, kw.Value);
                                break;
                        }
                        break;
                    }

                }
            } while (m.Success);
            if (r.Length > 0&&!aligned)
                switch (r[0])
                {
                    case '「':
                    case '『':
                    case '＜':
                    case '《':
                        r = "　" + r;
                        break;
                    default:
                        r = "　　" + r;
                        break;
                }
            html += r + "\r\n";
        }
        return html;
    }

}

