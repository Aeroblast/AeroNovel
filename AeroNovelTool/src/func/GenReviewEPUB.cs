using System;
using System.IO;
using System.Text.RegularExpressions;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AeroEpub.Epub;
namespace AeroNovelEpub
{
    public class GenReviewEpub
    {
        public List<AtxtSource> srcs = new List<AtxtSource>();

        public List<string> src_paths = new List<string>();
        public string dir;
        public Dictionary<string, string> macros;


        string spine = "";
        string items = "";
        string version = "2.0";
        string title = "";
        EpubFile epub = new EpubFile("template.zip");
        string uid = "urn:uuid:" + Guid.NewGuid().ToString();
        string xhtml_temp;

        public string img_path
        {
            get { return Path.Combine(dir, "Images"); }
        }
        public string fnt_path
        {
            get { return Path.Combine(dir, "Fonts"); }
        }
        public List<string> img_names = new List<string>();
        public ChineseConvertOption cc_option;
        public ConfigValue indentAdjust = 0;
        public ConfigValue addInfo = 0;
        ChineseConvert cc;
        public ProjectConfig config;
        public GenReviewEpub()
        {
            TextEpubItemFile t = epub.GetFile<TextEpubItemFile>("OEBPS/Text/template.xhtml");
            xhtml_temp = t.text;
            epub.items.Remove(t);
        }
        public EpubFile Gen(string dir)
        {

            if (File.Exists(Path.Combine(dir, "config.txt")))
            {
                config = new ProjectConfig(File.ReadAllLines(Path.Combine(dir, "config.txt")));
                Log.Info("Read config.txt");
                indentAdjust = Util.GetConfigValue(indentAdjust, config.indentAdjust);
                addInfo = Util.GetConfigValue(addInfo, config.addInfo);
            }
            if (cc_option == ChineseConvertOption.T2S)
            {
                Log.Note("Chinese Convert: T2S");
                cc = new ChineseConvert();
                cc.Prepare();
            }
            if (indentAdjust == ConfigValue.disable)
                Log.Note("Option: No indent adjustion.");
            if (addInfo == ConfigValue.disable)
                Log.Note("Option: Do not add generation info.");

            this.dir = dir;

            string metaPath = Path.Combine(dir, "meta.txt");
            if (File.Exists(Path.Combine(dir, "meta3.txt")))
            {
                metaPath = Path.Combine(dir, "meta3.txt");
                version = "3.0";
                xhtml_temp = Regex.Replace(xhtml_temp, "<!DOCTYPE html([\\s\\S]*?)>", "<!DOCTYPE html>");
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
                    macros.Add(s[0], s[1]);
                }

            }

            string meta = File.ReadAllText(metaPath);
            meta = meta.Replace("\r\n", "\n");
            meta = meta.Replace("{urn:uuid}", uid);
            uid = Regex.Match(meta, "<dc:identifier id=\"BookId\">(.*?)</dc:identifier>").Groups[1].Value;
            meta = meta.Replace("{date}", DateTime.Today.ToString("yyyy-MM-ddT00:00:00Z"));
            if (cc != null)
            {
                meta = cc.Convert(meta);
            }
            if (cc_option == ChineseConvertOption.T2S)
            {
                meta = meta.Replace("<dc:language>zh-tw</dc:language>", "<dc:language>zh</dc:language>", true, null);
            }
            if (!meta.Contains("<meta property=\"ibooks:specified-fonts\">true</meta>"))
            {
                Match m = Regex.Match(meta, "\n.*?</metadata>");
                meta = meta.Insert(m.Index + 1, "    <meta property=\"ibooks:specified-fonts\">true</meta>\n");
            }
            title = Regex.Match(meta, "<dc:title.*?>(.*?)</dc:title>").Groups[1].Value;

            CollectSource();
            if (config.autoSpace == ConfigValue.active)
            {
                foreach (var src in srcs)
                {
                    AutoSpace.ProcAtxt(src);
                }
            }
            GenContent();
            GetImage();
            GetCss();

            TextEpubItemFile toc = epub.GetFile<TextEpubItemFile>("OEBPS/toc.ncx");
            TextEpubItemFile nav = epub.GetFile<TextEpubItemFile>("OEBPS/nav.xhtml");
            if (version == "2.0")
            {
                epub.items.Remove(nav);
                var tocDocuments = GenTOC(File.ReadAllLines(Path.Combine(dir, "toc.txt")), uid, title, toc.text);
                toc.text = tocDocuments.Item1;
            }
            else
            {
                var tocDocuments = GenTOC(File.ReadAllLines(Path.Combine(dir, "toc.txt")), uid, title, toc.text, nav.text);
                toc.text = tocDocuments.Item1;
                nav.text = tocDocuments.Item2;
                items += "    <item id=\"nav.xhtml\" href=\"nav.xhtml\" media-type=\"application/xhtml+xml\" properties=\"nav\"/>";
            }

            TextEpubItemFile opf = epub.GetFile<TextEpubItemFile>("OEBPS/content.opf");
            opf.text = string.Format(opf.text, meta, items, spine, version);

            epub.ReadMeta();
            return epub;
        }

        void CollectSource()
        {
            string[] files = Directory.GetFiles(dir);
            foreach (string f in files)
            {
                Match m = Regex.Match(Path.GetFileName(f), AeroNovel.regStr_filename);
                if (!m.Success)
                {
                    m = Regex.Match(Path.GetFileName(f), AeroNovel.regStr_filename_xhtml);
                    if (!m.Success) { continue; }
                }
                src_paths.Add(f);
            }
            src_paths.Sort();

            foreach (string txt_path in src_paths)
            {
                var src = new AtxtSource(txt_path);
                srcs.Add(src);
            }

            foreach (var src in srcs)
            {
                if (src.title.StartsWith("SVG"))
                {
                    items += string.Format("    <item id=\"{0}\" href=\"Text/{0}\" media-type=\"application/xhtml+xml\" properties=\"svg\"/>\n", src.xhtmlName);
                }
                else
                {
                    items += string.Format("    <item id=\"{0}\" href=\"Text/{0}\" media-type=\"application/xhtml+xml\"/>\n", src.xhtmlName);
                }

                spine += string.Format("    <itemref idref=\"{0}\"/>\n", src.xhtmlName);

                if (cc != null)
                {
                    src.title = cc.Convert(src.title);
                }
            }
        }

        void GenContent()
        {
            int i = 0;
            foreach (var src in srcs)
            {
                string xhtml;
                if (src.ext == ".xhtml")
                {
                    xhtml = src.content;
                }
                else
                {
                    string[] lines = src.lines;
                    string body = $"<p>{src.path}</p>\n" + GenHTML(lines);
                    if (src.path.EndsWith("info.txt") || src.path.EndsWith("info.atxt"))
                    {
                        body = Regex.Replace(body, "<p>(.*?：)", "<p class=\"atxt_keyvalue\">$1");
                        body = "<div class=\"atxt_info\" epub:type=\"acknowledgements\">\n" + body;
                        if (addInfo != ConfigValue.disable)
                            body += "<p>AeroNovelTool 审阅用 EPUB 生成于" + DateTime.Now + "</p>";
                        body += "</div>";
                    }
                    if (src.path.EndsWith("EOB.txt") || src.path.EndsWith("EOB.atxt"))
                    {
                        body = "<div class=\"atxt_info\">" + body +
                        "</div>";
                    }
                    xhtml = xhtml_temp.Replace("{❤title}", title).Replace("{❤body}", body);
                }

                TextEpubItemFile item = new TextEpubItemFile("OEBPS/Text/" + srcs[i].xhtmlName, xhtml);
                epub.items.Add(item);
                Log.Info("Add xhtml: " + item.fullName + " (title:" + srcs[i].title + ")");
                i++;
            }

        }

        Regex reg_comment_seperator = new Regex("##[—]+$");


        public string GenHTML(string[] txt)
        {
            var context = this;
            string noteref_temp = "<a class=\"atxt_note_ref\" epub:type=\"noteref\" href=\"#note{0}\" id=\"note_ref{0}\"><sup>[注]</sup></a>";
            int note_count = 0;
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
                {"^#center:(.*)","<p class=\"atxt_align_center\">$1</p>"},
                {"^#right:(.*)","<p class=\"atxt_align_right\">$1</p>"},
                {"^#left:(.*)","<p class=\"atxt_align_left\">$1</p>"},
                {reg_noteref,""},
                {reg_notecontent,""},
                {reg_img,""},
                {reg_illu2,""},
                {reg_imgchar,""},
                {reg_class,""},
                {reg_chapter,""},
                {"\\[b\\](.*?)\\[\\/b\\]","<b>$1</b>"},
                {"^#title:(.*)","<p class=\"atxt_title\">$1</p>"},
                {"\\[ruby=(.*?)\\](.*?)\\[\\/ruby\\]","<ruby>$2<rp>(</rp><rt>$1</rt><rp>)</rp></ruby>"},
                {"^\\[pagebreak\\]$","<p class=\"atxt_pagebreak\"><br/></p>"},
                {"\\[emphasis\\](.*?)\\[\\/emphasis\\]","<span class=\"atxt_emph\">$1</span>"},
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
                {"^#class:(.*)","<div class=\"$1\">"},
                {"^#/class","</div>"},
                {"\\[font\\](.*?)\\[\\/font\\]","<span class=\"atxt_font\">$1</span>"},
                {"\\[url=(.*?)\\](.*?)\\[\\/url\\]","<a href=\"$1\">$2</a>"},

                //字符处理
                {"(?<!<span class=\"atxt_breakall\">)(?<!…)[…]{3,99}","<span class=\"atxt_breakall\">$0</span>"},
                {"(?<!<span class=\"atxt_breakall\">)(?<!—)[—]{3,99}","<span class=\"atxt_breakall\">$0</span>"}
            };

            string html = "";
            foreach (string line in txt)
            {
                if (line.StartsWith("##"))
                {
                    if (reg_comment_seperator.Match(line).Success)
                    { html += "<p class=\"review_comment\"><br/></p>"; }
                    else
                    {
                        html += $"<p class=\"review_comment\">{line.Replace("<", "&lt;").Replace(">", "&gt;")}</p>\n";
                    }

                    continue;
                }

                string r = EncodeHTML(line);
                Match m = Regex.Match("", "1");

                if (context != null & context.macros != null)
                {
                    int executionCount = 0;
                    do
                    {
                        string safeCheck = r;
                        foreach (var pair in context.macros)
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
                                case reg_noteref://noteref
                                    r = reg.Replace(r, string.Format(noteref_temp, note_count), 1);
                                    note_count++;
                                    break;
                                case reg_notecontent://note
                                    notes.Add(m.Groups[1].Value);
                                    r = reg.Replace(r, "", 1);
                                    break;
                                case reg_img://img
                                    {
                                        string img_name = Path.GetFileName(m.Groups[1].Value);
                                        if (File.Exists(Path.Combine(context.img_path, img_name)))
                                        {
                                            Log.Info("Image used:" + img_name);
                                            if (!context.img_names.Contains(img_name))
                                            {
                                                context.img_names.Add(img_name);
                                            }
                                        }
                                        else
                                        {
                                            Log.Warn("Cannot find " + img_name);
                                        }
                                        string src = "../Images/" + img_name;
                                        string img_temp = "<img src=\"{0}\" alt=\"\"/>";
                                        r = reg.Replace(r, string.Format(img_temp, src), 1);
                                    }

                                    break;
                                case reg_illu2:
                                case reg_illu://illu
                                    {
                                        string img_name = Path.GetFileName(m.Groups[1].Value);
                                        if (File.Exists(Path.Combine(context.img_path, img_name)))
                                        {
                                            Log.Info("Illustation used:" + img_name);
                                            if (!context.img_names.Contains(img_name))
                                            {
                                                context.img_names.Add(img_name);
                                            }
                                        }
                                        else
                                        {
                                            Log.Warn("Cannot find " + img_name);
                                        }
                                        string src = "../Images/" + img_name;
                                        string img_temp = "<div class=\"atxt_aligned atxt_illu\"><img class=\"atxt_illu\" src=\"{0}\" alt=\"\"/></div>";
                                        r = reg.Replace(r, string.Format(img_temp, src), 1);
                                    }
                                    break;
                                case reg_imgchar:
                                    {
                                        string img_name = Path.GetFileName(m.Groups[1].Value);
                                        if (File.Exists(Path.Combine(context.img_path, img_name)))
                                        {
                                            Log.Info("Imagechar used:" + img_name);
                                            if (!context.img_names.Contains(img_name))
                                            {
                                                context.img_names.Add(img_name);
                                            }
                                        }
                                        else
                                        {
                                            Log.Warn("Cannot find " + img_name);
                                        }
                                        string src = "../Images/" + img_name;
                                        string img_temp = "<img class=\"atxt_imgchar\" src=\"{0}\" alt=\"\"/>";
                                        r = reg.Replace(r, string.Format(img_temp, src), 1);
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
                                case reg_chapter://chapter
                                    {
                                        string chapnum_s = m.Groups[1].Value;
                                        int chapnum;
                                        if (!int.TryParse(chapnum_s, out chapnum)) { Log.Error("Bad chapter string:" + chapnum_s); continue; }

                                        int index = context.srcs.FindIndex(0, (src) => int.Parse(src.no) == chapnum);
                                        if (index < 0) { Log.Error("Bad chapter number:" + chapnum); continue; }
                                        string path = context.srcs[index].xhtmlName;
                                        r = reg.Replace(r, "<a href=\"" + path + "\">$2</a>");
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
                        if (context == null || context.indentAdjust != ConfigValue.disable)
                            r = "<p class=\"atxt_drawout\">" + r + "</p>";
                    }
                    else
                        r = "<p>" + r + "</p>";
                }
                CheckUnprocessedTag(r);
                html += r + "\n";
            }
            if (notes.Count > 0)
            {
                html += "<aside class=\"atxt_note_section\" epub:type=\"footnote\">注释<br/>\n";
                string note_temp = "<aside epub:type=\"footnote\" id=\"note{0}\"><p class=\"atxt_note_p\"><a href=\"#note_ref{0}\">{2}</a>{1}</p></aside>\n";
                int count = 0;
                foreach (string note in notes)
                {
                    int div = note.IndexOf('：');
                    if (div > 0)
                    {
                        //to-do: 改进效率……暂时懒得改，也不影响

                        string noteref_text = note.Substring(0, div);
                        html = html.Replace(string.Format(noteref_temp, count), string.Format(noteref_temp.Replace("注", noteref_text), count));
                        string note_content = note.Substring(div + 1);
                        html += string.Format(note_temp, count, note_content, noteref_text + "：");
                    }
                    else
                        html += string.Format(note_temp, count, note, "注：");
                    count++;
                }
                html += "</aside>";
            }

            return html;
        }

        Regex reg_tag = new Regex("\\[(.{1,20}?)\\]");
        void CheckUnprocessedTag(string s)
        {
            var ms = reg_tag.Matches(s);
            foreach (Match m in ms)
            {
                if (Regex.Match(m.Groups[1].Value, "^[a-zA-Z0-9=]{1,20}$").Success)
                {
                    Log.Warn("Unprocessed tag in line:“" + s + "”");
                }
            }


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
        void GetImage()
        {
            if (Directory.Exists(img_path))
            {
                Dictionary<string, string> imgtype = new Dictionary<string, string>
                {
                    {".jpg","image/jpeg"},
                    {".png","image/png"}
                };
                foreach (var f in Directory.GetFiles(img_path))
                {
                    string ext = Path.GetExtension(f.ToLower());
                    string fn = Path.GetFileName(f);
                    if (imgtype.ContainsKey(ext))
                    {
                        EpubItemFile i = new EpubItemFile("OEBPS/Images/" + fn, File.ReadAllBytes(f));
                        epub.items.Add(i);
                        string properties = "";
                        if (fn == "cover.jpg") { properties = " properties=\"cover-image\""; }
                        items += $"    <item id=\"{fn}\" href=\"Images/{fn}\" media-type=\"{imgtype[ext]}\"{properties}/>\n";
                        if (!img_names.Contains(fn))
                        {
                            Log.Warn("Unrefered image: " + fn);
                        }
                        Log.Info("Add image: " + fn);
                    }

                }
            }
        }
        void GetCss()
        {
            TextEpubItemFile cssi = epub.GetFile<TextEpubItemFile>("OEBPS/Styles/Style.css");
            cssi.text += "\r\n\r\n .review_comment{text-indent:0; font-size:0.8em;color:#106600;page-break-after:avoid;} p{page-break-inside: avoid;}";
            var css = Directory.GetFiles(dir, "*.css");
            if (css.Length > 0)
            {
                cssi.text += "\r\n\r\n" + File.ReadAllText(css[0]);
                Log.Info("Css added:" + css[0]);
            }

        }
        public (string, string) GenTOC(string[] lines, string uid, string title, string template, string template3 = "")
        {
            //string temp=File.ReadAllText("template/toc.txt");
            string r = "";
            string r3 = "<ol>\n";
            List<string> label = new List<string>();
            int depth = 1;
            int count = 0;
            List<string> refered = new List<string>();//for playOrder

            Match m;
            foreach (string line in lines)
            {
                if (line[0] == '[')
                {
                    m = Regex.Match(line, "\\[(.*?)\\]");
                    if (!m.Success) throw new Exception("目录生成失败：");
                    string tag = m.Groups[1].Value;
                    if (tag[0] == '/')
                    {
                        label.RemoveAt(label.Count - 1);
                        r += "</navPoint>\n";
                        if (template3 != "") r3 += "</ol></li>\n";
                    }
                    else
                    {
                        label.Add(tag);
                        if (depth < label.Count + 1) { depth = label.Count + 1; }
                        count++;
                        m = Regex.Match(line.Substring(m.Index + m.Length), "([0-9][0-9])");
                        if (!m.Success) throw new Exception();
                        int index = srcs.FindIndex(src => src.no == m.Groups[1].Value);
                        string link = "Text/" + srcs[index].xhtmlName;
                        if (refered.IndexOf(link) < 0) { refered.Add(link); }
                        r += $"<navPoint id=\"navPoint-{count}\" playOrder=\"{refered.IndexOf(link) + 1}\"><navLabel><text>{tag}</text></navLabel><content src=\"{link}\"/>\n";
                        if (template3 != "") r3 += $"<li><a href=\"{link}\">{tag}</a><ol>\n";
                    }
                    continue;
                }

                m = Regex.Match(line, "([0-9][0-9])(.*)");
                if (m.Success)
                {
                    count++;
                    int index = srcs.FindIndex(src => src.no == m.Groups[1].Value);
                    string link = "Text/" + srcs[index].xhtmlName;
                    string navTitle = Util.Trim(m.Groups[2].Value);
                    if (navTitle.Length == 0)
                        navTitle = srcs[index].title;
                    if (refered.IndexOf(link) < 0) { refered.Add(link); }
                    r += $"<navPoint id=\"navPoint-{count}\" playOrder=\"{refered.IndexOf(link) + 1}\"><navLabel><text>{navTitle}</text></navLabel><content src=\"{link}\"/></navPoint>\n";
                    if (template3 != "")
                    {
                        r3 += $"  <li><a href=\"{link}\">{navTitle}</a></li>\n";
                    }
                }

            }
            r3 += "</ol>";
            return (
                string.Format(template, uid, depth, title, r),
                string.Format(template3, r3)
            );
        }
    }

}