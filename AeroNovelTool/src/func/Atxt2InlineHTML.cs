using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
class Atxt2InlineHTML
{
    public Atxt2InlineHTML(string dir)
    {
        ReadConfig(dir);
        ReadWebImages(dir);
    }
    public static string Convert(string path)
    {
        string dir = Path.GetDirectoryName(path);
        var inst = new Atxt2InlineHTML(dir);
        return inst.Process(path);
    }
    public static void ConvertSave(string path, string outputPath)
    {
        string dir = Path.GetDirectoryName(path);
        var inst = new Atxt2InlineHTML(dir);
        string r = inst.Process(path);
        File.WriteAllText(outputPath, r);
        Log.Note("Saved: " + outputPath);
    }
    public static void ConvertSaveDir(string path, string outputDir)
    {
        var inst = new Atxt2InlineHTML(path);
        foreach (var f in Directory.GetFiles(path))
        {
            if (AeroNovel.isIndexedTxt(f))
            {
                string r = inst.Process(f);
                var outputPath = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(f) + ".txt");
                File.WriteAllText(outputPath, r);
                Log.Note("Saved: " + outputPath);
            }
        }

    }
    public string Process(string path)
    {
        string[] atxt = File.ReadAllLines(path);
        return "<span style=\"white-space:normal\">" + Gen(atxt) + "</span>";
    }
    public string Gen(string[] txt)
    {
        List<string> notes = new List<string>();
        const string reg_noteref = "\\[note\\]";
        const string reg_notecontent = "\\[note=(.*?)\\]";
        const string reg_img = "\\[img\\](.*?)\\[\\/img\\]";
        const string reg_illu = "^\\[illu\\](.*?)\\[\\/illu\\]$";
        const string reg_illu2 = "^#illu:(.*)";
        const string reg_imgchar = "\\[imgchar\\](.*?)\\[\\/imgchar\\]";
        const string reg_class = "\\[class=(.*?)\\](.*?)\\[\\/class\\]";
        const string reg_chapter = "\\[chapter=(.*?)\\](.*?)\\[\\/chapter\\]";
        Dictionary<string, string> reg_dic = new Dictionary<string, string>
            {
                
                ///优先去除注释
                {"/\\*.*?\\*/",""},
                {"///.*",""},

                {"^#center:(.*)","<span style=\"display:block;text-align:center;\">$1</span>"},
                {"^#right:(.*)","<span style=\"display:block;text-align:right;\">$1</span>"},
                {"^#left:(.*)","<span style=\"display:block;text-align:left;\">$1</span>"},
                {reg_noteref,""},
                {reg_notecontent,""},
                {reg_img,""},
                {reg_illu2,""},
                {reg_imgchar,""},
                {reg_class,""},
                {reg_chapter,""},
                {"\\[b\\](.*?)\\[\\/b\\]","<b>$1</b>"},
                {"^#title:(.*)","<span style=\"display:block;text-align:center;font-size:2em;font-weight:bold\">$1</span>"},
                {"\\[ruby=(.*?)\\](.*?)\\[\\/ruby\\]","<ruby>$2<rt>$1</rt></ruby>"},
                {"^\\[pagebreak\\]$","<p class=\"atxt_pagebreak\"><br/></p>"},
                {"\\[emphasis\\](.*?)\\[\\/emphasis\\]","<span style=\"-webkit-text-emphasis: dot filled;-webkit-text-emphasis-position: under;\">$1</span>"},
                {"\\[s\\](.*?)\\[\\/s\\]","<s>$1</s>"},
                {"\\[i\\](.*?)\\[\\/i\\]","<i>$1</i>"},
                {"\\[color=(.*?)\\](.*?)\\[\\/color\\]","<span style=\"color:$1\">$2</span>"},
                {"\\[size=(.*?)\\](.*?)\\[\\/size\\]","<span style=\"font-size:$1em\">$2</span>"},
                {"^#h1:(.*)","<h1>$1</h1>"},
                {"^#h2:(.*)","<h2>$1</h2>"},
                {"^#h3:(.*)","<h3>$1</h3>"},
                {"^#h4:(.*)","<h4>$1</h4>"},
                {"^#h5:(.*)","<h5>$1</h5>"},
                {"^#h6:(.*)","<h6>$1</h6>"},
                {"^#class:(.*)","<div class='$1'>"},
                {"^#/class","</div>"},
                {"\\[font\\](.*?)\\[\\/font\\]","<span class=\"atxt_font\">$1</span>"},
                {"\\[url=(.*?)\\](.*?)\\[\\/url\\]","<a href=\"$1\">$2</a>"},

                //字符处理
                //{"(?<!<span class=\"atxt_breakall\">)(?<!…)[…]{3,99}","<span class=\"atxt_breakall\">$0</span>"},
                //{"(?<!<span class=\"atxt_breakall\">)(?<!—)[—]{3,99}","<span class=\"atxt_breakall\">$0</span>"}
            };


        string html = "";
        foreach (string line in txt)
        {
            if (line.StartsWith("##")) continue;

            string r = EncodeHTML(line);
            Match m = Regex.Match("", "1");

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

            if (r.StartsWith("#HTML:"))
            {
                html += r.Substring("#HTML:".Length) + "\n";
                continue;
            }

            do
            {
                foreach (var pair in reg_dic)
                {
                    m = Regex.Match(r, pair.Key);
                    if (m.Success)
                    {
                        Regex reg = new Regex(pair.Key);
                        switch (pair.Key)
                        {
                            case reg_illu:
                            case reg_illu2:
                                {
                                    var a = m.Groups[1].Value;
                                    if (web_images.ContainsKey(a))
                                    {
                                        r = r.Replace(m.Value, "<span style=\"display:block;text-align:center\"><img src=\"" + web_images[a] + "\"></span>");
                                    }
                                    else
                                    {
                                        if (pair.Key == reg_illu2 || pair.Key == reg_illu)
                                            r = r.Replace(m.Value, "【没传图床的图片：" + a + "】");
                                        r = r.Replace(m.Value, "");
                                        Log.Warn("没传图床的图片：" + a);
                                    }
                                }
                                break;
                            case reg_img:
                            case reg_imgchar:
                                {
                                    var a = m.Groups[1].Value;
                                    if (web_images.ContainsKey(a))
                                    {
                                        r = r.Replace(m.Value, "<img src=\"" + web_images[a] + "\">");
                                    }
                                    else
                                    {
                                        if (pair.Key == reg_illu2 || pair.Key == reg_illu)
                                            r = r.Replace(m.Value, "【没传图床的图片：" + a + "】");
                                        r = r.Replace(m.Value, "");
                                        Log.Warn("没传图床的图片：" + a);
                                    }
                                }
                                break;
                            case reg_class://class
                                {

                                    if (m.Index == 0 && m.Length == r.Length)
                                    {
                                        r = reg.Replace(r, "<p class=\"$1\">$2</p>");

                                    }
                                    else
                                    {
                                        r = reg.Replace(r, "<span class=\"$1\">$2</span>");
                                    }
                                }
                                break;
                            default:
                                r = reg.Replace(r, pair.Value);
                                break;
                        }
                        break;
                    }

                }
            } while (m.Success);
            if (r.Length == 0) { r = "<br/>"; }
            bool addp = true;
            string[] dont_addp_list = new string[]
            {"p","div","/div","h1","h2","h3","h4","h5","h6","span"};
            foreach (var a in dont_addp_list)
                if (Regex.Match(r, "<" + a + ".*>").Success)
                    addp = false;
            if (addp)
            {
                var temptrimed = Util.TrimTag(r);
                var first = (temptrimed.Length > 0) ? temptrimed[0] : '\0';
                if (first == '（' || first == '「' || first == '『' || first == '〈' || first == '【' || first == '《')
                {
                    r = "<span style=\"display:block;text-indent:1.5em;line-height:1.5;\">" + r + "</span>";
                }
                else
                    r = "<span style=\"display:block;text-indent:2em;line-height:1.5;\">" + r + "</span>";
            }
            html += r + "\n";
        }

        return html;
    }
    static Regex entityWarn = new Regex("&[#0-9a-z]*?;");
    public static string EncodeHTML(string s)
    {
        if (s.Contains("&"))
        {
            if (!entityWarn.Match(s).Success)
            {
                Log.Warn("entity charater '&' detect.");
                s = s.Replace("&", "&amp;");
                //这里十分不严谨
            }
        }
        return s;
    }
    Dictionary<string, string> web_images;
    void ReadWebImages(string dir)
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
    Dictionary<string, string> macros;
    void ReadConfig(string dir)
    {
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


}