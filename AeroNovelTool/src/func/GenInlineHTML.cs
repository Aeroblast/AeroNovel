using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
class GenInlineHTML
{
    AtxtProject project;
    public GenInlineHTML(string dir)
    {
        project = new AtxtProject(dir);
        project.LoadMacro();
        project.LoadWebImages();
        project.CollectSource();
    }
    public static void ConvertFile(string path, string outputPath)
    {
        string dir = Path.GetDirectoryName(path);
        var inst = new GenInlineHTML(dir);
        string r = inst.GenContent(File.ReadAllLines(path));
        File.WriteAllText(outputPath, r);
        Log.Note("Saved: " + outputPath);
    }
    public static void ConvertDir(string path, string outputDir)
    {
        var inst = new GenInlineHTML(path);
        foreach (var f in inst.project.srcs)
        {
            if (f.title == "EOB") { continue; }
            if (f.title.StartsWith("SVG")) { continue; }
            string r = inst.GenContent(f.lines);
            var outputPath = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(f.xhtmlName) + ".txt");
            File.WriteAllText(outputPath, r);
            Log.Note("Saved: " + outputPath);

        }
    }
    public string GenContent(string[] txt)
    {
        return "<div style=\"line-height:1.5;text-align:justify;\">\n" + GenBody(txt) + "</div>";
    }
    public string GenBody(string[] txt)
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

                {"^#center:(.*)","<p style=\"text-align:center;margin:0;\">$1</p>"},
                {"^#right:(.*)","<p style=\"text-align:right;margin:0;\">$1</p>"},
                {"^#left:(.*)","<p style=\"text-align:left;margin:0;\">$1</p>"},
                {reg_noteref,"<span style=\"vertical-align:super;font-size:smaller;\">[注]</span>"},
                {reg_notecontent,"<p style=\"width:50%;margin-left:40%\">$1</p>"},
                {reg_img,""},
                {reg_illu2,""},
                {reg_imgchar,""},
                {reg_class,""},
                {reg_chapter,"$2"},
                {"\\[b\\](.*?)\\[\\/b\\]","<b>$1</b>"},
                {"^#title:(.*)","<p style=\"text-align:center;font-size:1.6em;font-weight:bold\">$1</p>"},
                {"\\[ruby=(.*?)\\](.*?)\\[\\/ruby\\]","<ruby>$2<rt>$1</rt></ruby>"},
                {"^\\[pagebreak\\]$","<p class=\"atxt_pagebreak\"><br/></p>"},
                {"\\[em\\](.*?)\\[\\/em\\]","<span style=\"-webkit-text-emphasis: dot filled;-webkit-text-emphasis-position: under;\">$1</span>"},
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
                {"(?<!<span style=\"word-wrap:break-word;word-break:break-all;\">)(?<!…)[…]{3,99}","<span style=\"word-wrap:break-word;word-break:break-all;\">$0</span>"},
                {"(?<!<span style=\"word-wrap:break-word;word-break:break-all;\">)(?<!—)[—]{3,99}","<span style=\"word-wrap:break-word;word-break:break-all;\">$0</span>"}
            };


        string html = "";
        foreach (string line in txt)
        {
            if (line.StartsWith("##")) continue;

            string r = EncodeHTML(line);
            Match m = Regex.Match("", "1");

            if (project.macros != null)
            {
                do
                {
                    foreach (var pair in project.macros)
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
                                    if (project.web_images.ContainsKey(a))
                                    {
                                        r = r.Replace(m.Value, "<p style=\"text-align:center\"><img src=\"" + project.web_images[a] + "\" style=\"max-width:100%;max-height:90vh\"></p>");
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
                                    if (project.web_images.ContainsKey(a))
                                    {
                                        r = r.Replace(m.Value, "<img src=\"" + project.web_images[a] + "\">");
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
            {"p","div","/div","h1","h2","h3","h4","h5","h6"};
            foreach (var a in dont_addp_list)
                if (Regex.Match(r, "^<" + a + ".*>").Success)
                    addp = false;
            if (addp)
            {
                var temptrimed = Util.TrimTag(r);
                var first = (temptrimed.Length > 0) ? temptrimed[0] : '\0';
                if (Util.IsNeedAdjustIndent(first))
                {
                    r = "<p style=\"text-indent:1.5em;margin:0;\">" + r + "</p>";
                }
                else
                    r = "<p style=\"text-indent:2em;margin:0;\">" + r + "</p>";
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
}