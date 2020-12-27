using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.IO.Compression;
using System.Collections.Generic;
class GenBbcode
{
    public static string output_path = "output_bbcode/";
    public static string output_path_single = "output_bbcode_single.txt";
    static List<int> cat_page = new List<int>();
    static Dictionary<string, string> web_images = new Dictionary<string, string>();
    static Dictionary<string, string> macros;
    static string[] additional_msg = new string[] { };
    public static void Proc(string path)
    {
        string[] lines = File.ReadAllLines(path);
        string body = Body(lines);
        string outpath = "output_bbcode.txt";
        File.WriteAllText(outpath, body);
        Log.Note("Output: " + outpath);
    }
    public static void Gen(string dir)
    {
        string[] files = Directory.GetFiles(dir);
        Directory.CreateDirectory(output_path);
        ReadWebImages(dir);
        ReadConfig(dir);
        foreach (string f in files)
        {
            Match m = Regex.Match(Path.GetFileName(f), AeroNovel.filename_reg);
            if (!m.Success) continue;
            //string no = m.Groups[1].Value;
            string chaptitle = m.Groups[2].Value;
            string[] lines = File.ReadAllLines(f);
            string body = Body(lines);
            string outpath = output_path + Path.GetFileNameWithoutExtension(f) + ".txt";
            File.WriteAllText(outpath, body);
            Console.WriteLine(outpath);
        }
    }
    public static void GenSingle(string dir)
    {
        Log.Note("bbcode single file generation.");
        string[] files = Directory.GetFiles(dir);
        ReadWebImages(dir);
        ReadConfig(dir);
        string result = "";
        List<string> atxt = new List<string>();
        foreach (string f in files)
        {
            Match m = Regex.Match(Path.GetFileName(f), AeroNovel.filename_reg);
            if (!m.Success) continue;
            atxt.Add(f);
        }
        atxt.Sort();

        int index = 0;
        //string toc = "";
        foreach (var f in atxt)
        {
            Log.Info("Processing " + Path.GetFileName(f));
            Match m = Regex.Match(Path.GetFileName(f), AeroNovel.filename_reg);
            int no = int.Parse(m.Groups[1].Value);
            string chaptitle = m.Groups[2].Value;
            string[] lines = File.ReadAllLines(f);
            string body = Body(lines);
            result += body;
            bool contains = false;
            foreach (int i in cat_page) if (i == no) { contains = true; break; }
            if (!contains)
            {
                index++;
                //toc += string.Format("[#{0}]【{0}】{1}\r\n", index, chaptitle);
                //result += "[page]\r\n";
                //result += "【第" + (index + 1) + "页】\r\n";
                result += "======================\r\n";
            }

        }
        //File.WriteAllText("bbcode_output.txt", "[index]\r\n" + toc + "[/index]\r\n" + result);
        File.WriteAllText(output_path_single, result);
        Log.Note("Output:" + output_path_single);

    }
    static void ReadWebImages(string dir)
    {
        web_images = new Dictionary<string, string>();
        string path = Path.Combine(dir, "web_images");
        if (File.Exists(path + ".md"))
        {
            Log.Note("图床链接配置文档读取成功：" + path + ".md");
            string[] a = File.ReadAllLines(path + ".md");
            Regex md_img = new Regex("\\[(.+?)\\]\\((.+?)\\)");
            foreach (var x in a)
            {
                var b = md_img.Match(x);
                if (b.Success)
                {
                    web_images.Add(b.Groups[1].Value, b.Groups[2].Value);
                }
            }
        }
        if (File.Exists(path + ".txt"))
        {
            Log.Note("图床链接配置文档读取成功：" + path + ".txt");
            string[] a = File.ReadAllLines(path + ".txt");
            foreach (var x in a)
            {
                var b = x.Split(' ');
                if (b.Length > 1)
                {
                    web_images.Add(b[0], b[1]);
                }
            }
        }

    }
    static void ReadConfig(string dir)
    {
        string path = Path.Combine(dir, "web_config.txt");
        if (File.Exists(path))
        {
            string[] a = File.ReadAllLines(path);
            foreach (var x in a)
            {
                var s = x.Split('=');
                var b = Util.Trim(s[0]);
                switch (b)
                {
                    case "cat_page":
                        {
                            var i = s[1].Split(",");
                            foreach (var ii in i)
                            {
                                var c = Util.Trim(ii);
                                if (c != "") { cat_page.Add(int.Parse(c)); }
                            }
                        }
                        break;
                    case "additional_msg":
                        {
                            additional_msg = s[1].Split(' ');
                        }
                        break;
                }

            }
        }
        if (File.Exists(Path.Combine(dir, "macros.txt")))
        {
            Log.Info("Read macros.txt");
            string[] macros_raw = File.ReadAllLines(Path.Combine(dir, "macros.txt"));
            macros = new Dictionary<string, string>();
            foreach (string macro in macros_raw)
            {
                string[] s = macro.Split('\t');
                if (s.Length < 2)
                {
                    Log.Warn("Macro defination is not complete. Use tab to separate: " + macro);
                }
                else if (s.Length == 2)
                {
                    macros.Add(s[0], s[1]);
                }
                else//length>2
                {
                    macros.Add(s[0], s[2]);
                }

            }

        }
    }
    public static string Body(string[] txt)
    {

        const string reg_noteref = "\\[note\\]";
        const string reg_notecontent = "\\[note=(.*?)\\]";
        const string reg_img = "\\[img\\]((?!http).*?)\\[\\/img\\]";
        const string reg_illu = "\\[illu\\]((?!http).*?)\\[\\/illu\\]";
        const string reg_illu2 = "^#illu:(.*)";
        const string reg_imgchar = "\\[imgchar\\]((?!http).*?)\\[\\/imgchar\\]";
        const string reg_class = "\\[class=(.*?)\\](.*?)\\[\\/class\\]";
        const string reg_chapter = "\\[chapter=(.*?)\\](.*?)\\[\\/chapter\\]";
        Dictionary<string, string> reg_dic = new Dictionary<string, string>
            {
                {"/\\*.*?\\*/",""},
                {"///.*",""},
                //{"\\[align=(.*?)\\](.*?)\\[\\/align\\]","<p class=\"aligned\" style=\"text-align:$1\">$2</p>"},
                {"^\\[center\\](.*?)\\[\\/center\\]$","[align=center]$1[/align]"},
                {"^\\[right\\](.*?)\\[\\/right\\]$","[align=right]$1[/align]"},
                {"^\\[left\\](.*?)\\[\\/left\\]$","[align=left]$1[/align]"},
                {"^#left:(.*)","[align=left]$1[/align]"},
                {"^#center:(.*)","[align=center]$1[/align]"},
                {"^#right:(.*)","[align=right]$1[/align]"},
                {reg_noteref,"[color=#00ffff][ruby=注][size=1]　[/size][/ruby][/color]"},
                {reg_notecontent,"\r\n[align=right][size=1][color=#00ffff]$1[/color][/size][/align]"},
                {reg_img,""},
                {reg_illu,""},
                {reg_illu2,""},
                {reg_imgchar,""},
                {reg_class,"$2"},
                {reg_chapter,"$2"},
                //{"\\[b\\](.*?)\\[\\/b\\]","<b>$1</b>"},
                {"\\[title\\](.*?)\\[\\/title\\]","[size=5]$1[/size]"},
                {"^#title:(.*)","[size=5]$1[/size]"},
                //{"\\[ruby=(.*?)\\](.*?)\\[\\/ruby\\]","<ruby>$2<rt>$1</rt></ruby>"},
                {"\\[pagebreak\\]",""},
                {"\\[emphasis\\](.*?)\\[\\/emphasis\\]","[b]$1[/b]"},
                //{"\\[s\\](.*?)\\[\\/s\\]","<s>$1</s>"},
                //{"\\[i\\](.*?)\\[\\/i\\]","<i>$1</i>"},
                //{"\\[color=(.*?)\\](.*?)\\[\\/color\\]","<span style=\"color:$1\">$2</span>"},
                //{"\\[size=(.*?)\\](.*?)\\[\\/size\\]","<span style=\"font-size:$1em\">$2</span>"}
                {"^#class:(.*)",""},
                {"^#/class",""},
                {"^#h1:(.*)","[size=3][b]$1[/b][/size]"},
                {"^#h2:(.*)","[size=3][b]$1[/b][/size]"},
                {"^#h3:(.*)","[size=4][b]$1[/b][/size]"},
                {"^#h4:(.*)","[size=4][b]$1[/b][/size]"},
                {"^#h5:(.*)","[size=5][b]$1[/b][/size]"},
                {"^#h6:(.*)","[size=5][b]$1[/b][/size]"},
            };
        string bbcode = "";
        int addmessagecount = 0;
        foreach (string line in txt)
        {
            if (line.StartsWith("##")) continue;
            if (line.StartsWith("#HTML")) continue;
            string r = line;
            Match m = Regex.Match("", "1");

            //macros
            if (macros != null)
            {
                do
                {
                    foreach (var pair in macros)
                    {
                        m = Regex.Match(r, pair.Key);
                        if (m.Success)
                        {
                            Regex reg = new Regex(pair.Key);
                            r = reg.Replace(r, pair.Value);
                            break;
                        }
                    }
                } while (m.Success);
            }

            //regular
            do
            {
                foreach (var kv in reg_dic)
                {
                    m = Regex.Match(r, kv.Key);
                    if (m.Success)
                    {
                        Regex reg = new Regex(kv.Key);
                        switch (kv.Key)
                        {
                            case reg_img:
                            case reg_illu:
                            case reg_illu2:
                            case reg_imgchar:
                                {
                                    var a = m.Groups[1].Value;
                                    if (web_images.ContainsKey(a))
                                    {
                                        r = r.Replace(m.Value, "[img]" + web_images[a] + "[/img]");
                                    }
                                    else
                                    {
                                        if (kv.Key == reg_illu2 || kv.Key == reg_illu)
                                            r = r.Replace(m.Value, "【没传图床的图片：" + a + "】");
                                        r = r.Replace(m.Value, "");
                                        Log.Warn("没传图床的图片：" + a);
                                    }
                                }
                                break;
                            default:
                                r = reg.Replace(r, kv.Value);
                                break;
                        }
                        break;
                    }

                }
            } while (m.Success);
            string checkhead = Regex.Replace(r, "\\[.*?\\]", "");
            if (checkhead.Length > 0)
                switch (checkhead[0])
                {
                    case '「':
                    case '『':
                    case '＜':
                    case '《':
                    case '【':
                    case '（':
                        r = "　" + r;
                        break;
                    default:
                        r = "　　" + r;
                        break;
                }
            if (r == "　　" || r == "")
            {
                addmessagecount++;
                if (addmessagecount == 11)
                    r = GetRandomMessage();
            }
            else
            {
                if (addmessagecount > 10)
                    addmessagecount--;
            }
            if (r.EndsWith("[/align]"))
                bbcode += r;
            else
                bbcode += r + "\r\n";
        }

        return bbcode;
    }
    static Random random = new Random();
    static string GetRandomMessage()
    {
        if (additional_msg.Length == 0)
            return "";
        if (random.Next(0, 100) > 60) return "";
        string inserter = " 　_-^！!~～+='·.．`";
        string s = additional_msg[random.Next(0, additional_msg.Length)];
        char ins = inserter[random.Next(0, inserter.Length)];
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append("[color=white]");
        for (int i = 0; i < s.Length; i++)
        {
            if (random.Next(0, 100) > 60) stringBuilder.Append(ins);
            if (random.Next(0, 100) > 60) stringBuilder.Append(ins);
            stringBuilder.Append(s[i]);
            if (random.Next(0, 100) > 60) stringBuilder.Append(ins);
        }
        stringBuilder.Append("[/color]");
        return stringBuilder.ToString();
    }

}

