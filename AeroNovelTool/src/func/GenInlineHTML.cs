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
        project.TryLoadMacro(AtxtProject.MacroMode.InlineHTML);
        project.LoadWebImages();
        project.CollectSource();
        project.ApplyAutoSpace();
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
            if (f.title == "info") { r = r.Replace("text-indent:1.5em;", "").Replace("text-indent:2em;", ""); }
            var outputPath = Path.Combine(outputDir, f.id + f.title + ".txt");
            if (inst.project.config != null && inst.project.config.addSourceInfo == ConfigValue.active)
            {
                var msg = $"源：{f.id}{f.title}{(f.majorVersionTime != null ? "｜成稿：" + f.majorVersionTime : "")}｜更改：{f.lastModificationTime} - {f.lastComment}";
                Console.WriteLine(msg);
                r += $"\n<div style=\"font-family:monospace;font-size:0.5em;color:#efefef;line-height:1;\">{msg}</div>";
            }
            File.WriteAllText(outputPath, r);
            Log.Note("Saved: " + outputPath);

        }
    }
    public string GenContent(string[] txt)
    {
        var wrapperStyle = "line-height:1.5;text-align:justify;";
        if (project.config != null && !string.IsNullOrEmpty(project.config.inlinehtmlWrapperStyle))
        {
            wrapperStyle = project.config.inlinehtmlWrapperStyle;
        }
        return $"<div style=\"{wrapperStyle}\">\n{GenBody(txt)}</div>";
    }
    public string GenBody(string[] txt)
    {
        List<string> notes = new List<string>();
        Stack<string> classRegionNames = new Stack<string>();
        //const string reg_noteref = "\\[note\\]";
        const string reg_notecontent = "\\[note=(.*?)\\]";
        const string reg_img = "\\[img\\](.*?)\\[\\/img\\]";
        const string reg_illu = "^\\[illu\\](.*?)\\[\\/illu\\]$";
        const string reg_illu2 = "^#illu:(.*)";
        const string reg_imgchar = "\\[imgchar\\](.*?)\\[\\/imgchar\\]";
        const string reg_class = "\\[class=(.*?)\\](.*?)\\[\\/class\\]";
        const string reg_chapter = "\\[chapter=(.*?)\\](.*?)\\[\\/chapter\\]";
        const string reg_class_region_start = "^#class:(.*)";
        const string reg_class_region_end = "^#/class";
        bool no_indent = false;
        Dictionary<string, string> reg_dic_comment = new Dictionary<string, string>{
                {"/\\*.*?\\*/",""},
                {"///.*",""},
        };
        Dictionary<string, string> reg_dic = new Dictionary<string, string>
            {
                {"^#center:(.*)","<p style=\"text-align:center;margin:0;\">$1</p>"},
                {"^#right:(.*)","<p style=\"text-align:right;margin:0;\">$1</p>"},
                {"^#left:(.*)","<p style=\"text-align:left;margin:0;\">$1</p>"},
                // {reg_noteref,"<span class=\"ae_noteref\" style=\"vertical-align:super;font-size:x-small;\">[注]</span>"},
                {reg_notecontent,"<span class=\"ae_notecontent\" style=\"display:block;text-indent:0;max-width:90vw;width:15em;margin-right:0%;margin-left:auto;\">$1</span>"},
                {reg_img,""},
                {reg_illu2,""},
                {reg_imgchar,""},
                {reg_class,""},
                {reg_chapter,"$2"},
                {"\\[b\\](.*?)\\[\\/b\\]","<b>$1</b>"},
                {"^#title:(.*)","<p style=\"text-align:center;font-size:1.6em;font-weight:bold\">$1</p>"},
                {"\\[ruby=(.*?)\\](.*?)\\[\\/ruby\\]","<ruby>$2<rp>(</rp><rt>$1</rt><rp>)</rp></ruby>"},
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
                {reg_class_region_start,"<div class=\"$1\">"},
                {reg_class_region_end,"</div>"},
                {"\\[font\\](.*?)\\[\\/font\\]","<span class=\"atxt_font\">$1</span>"},
                {"\\[url=(.*?)\\](.*?)\\[\\/url\\]","<a href=\"$1\">$2</a>"},

                //字符处理
                {"(?<!<span style=\"word-wrap:break-word;line-break:anywhere;\">)(?<!…)[…]{3,99}","<span style=\"word-wrap:break-word;line-break:anywhere;\">$0</span>"},
                {"(?<!<span style=\"word-wrap:break-word;line-break:anywhere;\">)(?<!—)[—]{3,99}","<span style=\"word-wrap:break-word;line-break:anywhere;\">$0</span>"}
            };


        string html = "";
        foreach (string line in txt)
        {
            if (line.StartsWith("##")) continue;

            string r = EncodeHTML(line);

            Match m = Regex.Match("", "1");
            do
            {
                foreach (var pair in reg_dic_comment)
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

            if (project.macros != null)
            {
                int executionCount = 0;
                do
                {
                    string safeCheck = r;
                    foreach (var pair in project.macros)
                    {
                        m = Regex.Match(r, pair.Key);
                        if (m.Success)
                        {
                            Regex reg = new Regex(pair.Key);
                            r = reg.Replace(r, pair.Value);
                            executionCount++;
                            if (r == safeCheck) continue;
                            break;
                        }
                    }
                    if (r == safeCheck) break;
                    if (executionCount > 100)
                    {
                        Log.Error("Macro: Max count");
                        Log.Error(r);
                        break;
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
                                        r = r.Replace(m.Value, "【没传图床的图片：" + a + "】");
                                        Log.Warn("没传图床的图片：" + a);
                                    }
                                }
                                break;
                            case reg_img:
                                {
                                    var a = m.Groups[1].Value;
                                    if (project.web_images.ContainsKey(a))
                                    {
                                        r = r.Replace(m.Value, "<img src=\"" + project.web_images[a] + "\">");
                                    }
                                    else
                                    {
                                        r = r.Replace(m.Value, "【没传图床的图片：" + a + "】");
                                        Log.Warn("没传图床的图片：" + a);
                                    }
                                }
                                break;
                            case reg_imgchar:
                                {
                                    var a = m.Groups[1].Value;
                                    if (project.web_images.ContainsKey(a))
                                    {
                                        r = r.Replace(m.Value, "<img class=\"atxt_imgchar\" src=\"" + project.web_images[a] + "\">");
                                    }
                                    else
                                    {
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
                            case reg_notecontent:
                                {
                                    notes.Add(m.Groups[1].Value);
                                    r = reg.Replace(r, pair.Value);
                                }
                                break;
                            case reg_class_region_start:
                                {
                                    var classname = m.Groups[1].Value;
                                    r = reg.Replace(r, pair.Value);
                                    classRegionNames.Push(classname);
                                    if (classname == "no_indent")
                                    {
                                        no_indent = true;
                                    }
                                }
                                break;
                            case reg_class_region_end:
                                {
                                    r = reg.Replace(r, pair.Value);
                                    var classname = classRegionNames.Pop();
                                    if (classname == "no_indent")
                                    {
                                        no_indent = false;
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
                if (no_indent)
                {
                    r = "<p style=\"text-indent:0;margin:0;\">" + r + "</p>";
                }
                else if (Util.IsNeedAdjustIndent(first))
                {
                    r = "<p style=\"text-indent:1.5em;margin:0;\">" + r + "</p>";
                }
                else
                {
                    r = "<p style=\"text-indent:2em;margin:0;\">" + r + "</p>";
                }
            }
            html += r + "\n";
        }

        {
            string noteref_temp = "<span class=\"ae_noteref\" style=\"vertical-align:super;font-size:x-small;white-space:nowrap;\">[注]</span>";
            string noteref_expression = "[note]";
            int pos = html.IndexOf(noteref_expression);
            int i = 0;
            while (pos > 0 && i < notes.Count)
            {
                var t = "注";
                var content = notes[i];
                var colonIndex = content.IndexOf("：");
                if (colonIndex > 0 && colonIndex <= 4)
                {
                    t = content.Substring(0, colonIndex);
                }
                var noteref_html = noteref_temp.Replace("注", t);
                html = html.Remove(pos, noteref_expression.Length).Insert(pos, noteref_html);
                i++;
                pos = html.IndexOf(noteref_expression, pos + noteref_html.Length);
            }
            if (i != notes.Count)
            {
                Log.Warn("注释的引用和内容数量不匹配。");
            }
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