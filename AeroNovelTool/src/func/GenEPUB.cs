using System;
using System.IO;
using System.Text.RegularExpressions;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using AeroEpub.Epub;
namespace AeroNovelEpub
{
    public class GenEpub
    {
        protected AtxtProject project;
        protected string spine = "";
        protected string items = "";
        protected string title = "";
        protected EpubFile epub = new EpubFile("template.zip");
        protected string uid = "urn:uuid:" + Guid.NewGuid().ToString();
        protected string xhtml_temp;

        protected string dir;

        public string img_path
        {
            get { return Path.Combine(dir, "Images"); }
        }
        public string fnt_path
        {
            get { return Path.Combine(dir, "Fonts"); }
        }
        public List<string> img_names = new List<string>();

        public ConfigValue indentAdjust
        {
            get
            {
                if (project.config == null) return ConfigValue.active;
                return project.config.indentAdjust;
            }
        }
        public ConfigValue addInfo
        {
            get
            {
                if (project.config == null) return ConfigValue.active;
                return project.config.addInfo;
            }
        }

        public Dictionary<string, string> macros
        {
            get { return project.macros; }
        }
        public List<AtxtSource> srcs
        {
            get { return project.srcs; }
        }
        public string version
        {
            get { return project.epubVersion; }
        }
        public GenEpub(string dir)
        {
            this.dir = dir;
            project = new AtxtProject(dir);

            TextEpubItemFile t = epub.GetFile<TextEpubItemFile>("OEBPS/Text/template.xhtml");
            xhtml_temp = t.text;
            epub.items.Remove(t);
            project.TryLoadMacro(AtxtProject.MacroMode.Epub);
            project.TryLoadConfig();
            project.TryLoadEpubMeta();
            if (project.epubVersion == "3.0")
            {
                xhtml_temp = Regex.Replace(xhtml_temp, "<!DOCTYPE html([\\s\\S]*?)>", "<!DOCTYPE html>");
            }
            BuildMeta();
            title = Regex.Match(meta, "<dc:title.*?>(.*?)</dc:title>").Groups[1].Value;
        }
        public EpubFile Gen()
        {
            if (indentAdjust == ConfigValue.disable)
                Log.Note("Option: No indent adjustion.");
            if (addInfo == ConfigValue.disable)
                Log.Note("Option: Do not add generation info.");

            project.CollectSource();
            project.ApplyAutoSpace();

            GenContent();
            GetImage();
            GetFont();
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

            BuildSpine();

            TextEpubItemFile opf = epub.GetFile<TextEpubItemFile>("OEBPS/content.opf");
            opf.text = string.Format(opf.text, meta, items, spine, version);

            epub.ReadMeta();
            return epub;
        }

        protected void BuildSpine()
        {
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
            }
        }
        protected string meta;
        protected void BuildMeta()
        {
            meta = project.epubMeta;
            meta = meta.Replace("{urn:uuid}", uid);
            meta = meta.Replace("{tool}", Version.Sign());
            uid = Regex.Match(meta, "<dc:identifier id=\"BookId\">(.*?)</dc:identifier>").Groups[1].Value;
            meta = meta.Replace("{date}", DateTime.Today.ToString("yyyy-MM-ddT00:00:00Z"));

            if (!meta.Contains("<meta property=\"ibooks:specified-fonts\">true</meta>"))
            {
                Match m = Regex.Match(meta, "\n.*?</metadata>");
                meta = meta.Insert(m.Index + 1, "    <meta property=\"ibooks:specified-fonts\">true</meta>\n");
            }
            title = Regex.Match(meta, "<dc:title.*?>(.*?)</dc:title>").Groups[1].Value;
        }

        public virtual void GenContent()
        {
            GenHtml genHtml = new GenHtml(this);
            string patchfile_path = Path.Combine(dir, "patch_t2s/patch.csv");
            string[] patches = new string[0];
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

                    string body = genHtml.Gen(lines);
                    if (src.path.EndsWith("info.txt") || src.path.EndsWith("info.atxt"))
                    {
                        body = Regex.Replace(body, "<p>(.*?：)", "<p class=\"atxt_keyvalue\">$1");
                        body = "<div class=\"atxt_info\" epub:type=\"acknowledgements\">\n" + body;
                        if (addInfo != ConfigValue.disable)
                            body += $"<p>AeroNovelTool v{Version.date} EPUB 生成器 生成于 {DateTime.Now}</p>";
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

        protected void GetImage()
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
        protected void GetFont()
        {
            if (Directory.Exists(fnt_path))
            {
                Dictionary<string, string> fnttype = new Dictionary<string, string>
                {
                    {".otf","application/font-sfnt"},
                };
                foreach (var f in Directory.GetFiles(fnt_path))
                {
                    string ext = Path.GetExtension(f.ToLower());
                    string fn = Path.GetFileName(f);
                    if (fnttype.ContainsKey(ext))
                    {
                        EpubItemFile i = new EpubItemFile("OEBPS/Fonts/" + fn, File.ReadAllBytes(f));
                        epub.items.Add(i);
                        string properties = "";
                        items += $"    <item id=\"{fn}\" href=\"Fonts/{fn}\" media-type=\"{fnttype[ext]}\"{properties}/>\n";
                        Log.Info("Add font: " + fn);
                    }

                }
            }

        }
        protected virtual void GetCss()
        {
            var css = Directory.GetFiles(dir, "*.css");
            if (css.Length > 0)
            {
                TextEpubItemFile cssi = epub.GetFile<TextEpubItemFile>("OEBPS/Styles/Style.css");
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
            var reg_toc = new Regex($"^([0-9]{{{project.id_length}}})(.*)");

            Match m;
            foreach (string line in lines)
            {
                if (line[0] == '[')
                {
                    m = Regex.Match(line, "^\\[(.*?)\\]");
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
                        var id = line.Substring(m.Index + m.Length);
                        m = reg_toc.Match(id);
                        if (!m.Success) throw new Exception($"Expect {project.id_length}-digit ID after '[Group]': " + line);
                        int index = srcs.FindIndex(src => src.id == m.Groups[1].Value);
                        string link = "Text/" + srcs[index].xhtmlName;
                        if (refered.IndexOf(link) < 0) { refered.Add(link); }
                        r += $"<navPoint id=\"navPoint-{count}\" playOrder=\"{refered.IndexOf(link) + 1}\"><navLabel><text>{tag}</text></navLabel><content src=\"{link}\"/>\n";
                        if (template3 != "") r3 += $"<li><a href=\"{link}\">{tag}</a><ol>\n";
                    }
                    continue;
                }

                m = reg_toc.Match(line);
                if (m.Success)
                {
                    count++;
                    int index = srcs.FindIndex(src => src.id == m.Groups[1].Value);
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