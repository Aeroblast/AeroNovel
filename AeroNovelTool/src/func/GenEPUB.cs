using System;
using System.IO;
using System.Text.RegularExpressions;
using System.IO.Compression;
using System.Collections.Generic;
using AeroEpubViewer.Epub;
namespace AeroNovelEpub
{
    public class GenEpub
    {
        public List<string> txt_nums = new List<string>();
        public List<string> txt_titles = new List<string>();
        public List<string> xhtml_names = new List<string>();
        public List<string> txt_paths = new List<string>();
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
        public List<string> img_names = new List<string>();
        ChineseConvertOption cc_option;
        ChineseConvert cc;
        public GenEpub(ChineseConvertOption cc_option = ChineseConvertOption.None)
        {
            this.cc_option = cc_option;
            if (cc_option == ChineseConvertOption.T2S)
            {
                Log.Note("Chinese Convert: T2S");
                cc = new ChineseConvert();
                cc.Prepare();
            }

            TextEpubItemFile t = epub.GetFile<TextEpubItemFile>("OEBPS/Text/template.xhtml");
            xhtml_temp = t.text;
            epub.items.Remove(t);
        }
        public EpubFile Gen(string dir)
        {
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
            title = Regex.Match(meta, "<dc:title.*?>(.*?)</dc:title>").Groups[1].Value;

            GenFileNames();
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

        void GenFileNames()
        {
            string[] files = Directory.GetFiles(dir);
            foreach (string f in files)
            {
                Match m = Regex.Match(Path.GetFileName(f), AeroNovel.filename_reg);
                if (!m.Success) { continue; }
                string no = m.Groups[1].Value;
                string chaptitle = m.Groups[2].Value;
                string name = "atxt" + no + ".xhtml";
                string txtname = Path.GetFileNameWithoutExtension(f);
                chaptitle = Util.UrlDecode(chaptitle);

                Dictionary<string, string> name_dic = new Dictionary<string, string>
                    {
                        {"序章","prologue"},
                        {"终章","epilogue"},
                        {"終章","epilogue"},
                        {"序幕","prologue"},
                        {"尾声","epilogue"},
                        {"简介","summary"},
                        {"簡介","summary"},
                        {"後記","postscript"},
                        {"后记","postscript"},
                        {"目錄","toc"},
                        {"目录","toc"}
                    };
                bool nameDicSuccess = false;
                foreach (var k in name_dic)
                {
                    if (txtname.Contains(k.Key))
                    {
                        name = "atxt" + no + "_" + k.Value + ".xhtml";
                        nameDicSuccess = true;
                        break;
                    }
                }
                if (!nameDicSuccess)
                {
                    string t = chaptitle;
                    string[] chapterNumberPatterns = new string[]{
                        "^第([一二三四五六七八九十百零0-9]{1,10})",
                        "([一二三四五六七八九十百零0-9]{1,10})\\s",
                        "([一二三四五六七八九十百零0-9]{1,10})章"
                        };
                    foreach (string pattern in chapterNumberPatterns)
                    {
                        var m_num = Regex.Match(t, pattern);
                        if (m_num.Success)
                        {
                            string chapterNumber = m_num.Groups[1].Value;
                            if (!char.IsDigit(chapterNumber[0])) chapterNumber = "" + Util.FromChineseNumber(chapterNumber);

                            t = t.Remove(m_num.Index, m_num.Length).Insert(m_num.Index, "_chapter" + chapterNumber + ' ');

                            break;
                        }
                    }
                    name = "_";
                    for (int i = 0; i < t.Length; i++)
                    {
                        if (t[i] < 128)
                        {
                            if (t[i] == ' ')
                            {
                                if (i == t.Length - 1) continue;
                                if (name.EndsWith('_')) continue;
                                name += '_'; continue;
                            }
                            if (t[i] == '_' && name.EndsWith('_')) continue;
                            name += t[i];
                        }
                    }
                    if (name.EndsWith('_')) name = name.Substring(0, name.Length - 1);
                    name = "atxt" + no + name + ".xhtml";
                }

                items += string.Format("    <item id=\"{0}\" href=\"Text/{0}\" media-type=\"application/xhtml+xml\"/>\n", name);
                spine += string.Format("    <itemref idref=\"{0}\"/>\n", name);

                txt_nums.Add(no);
                txt_titles.Add(chaptitle);
                xhtml_names.Add(name);
                txt_paths.Add(f);
            }
            if (cc != null)
            {
                for (int i = 0; i < txt_titles.Count; i++)
                    txt_titles[i] = cc.Convert(txt_titles[i]);
            }

        }
        void GenContent()
        {
            GenHtml genHtml = new GenHtml(this);
            string patchfile_path = Path.Combine(dir, "patch_t2s/patch.csv");
            string[] patchs = new string[0];
            if (cc_option == ChineseConvertOption.T2S && File.Exists(patchfile_path))
            {
                patchs = File.ReadAllLines(patchfile_path);
            }
            for (int i = 0; i < txt_nums.Count; i++)
            {
                string f = txt_paths[i];
                string[] lines = File.ReadAllLines(f);
                if (cc != null)
                {
                    for (int j = 0; j < lines.Length; j++)
                        lines[j] = cc.Convert(lines[j]);
                    foreach (var patch in patchs)
                    {
                        string[] xx = patch.Split(',');
                        if (xx[0] == txt_nums[i])
                        {
                            int line_num;
                            if (
                                int.TryParse(xx[1], out line_num)
                             && line_num <= lines.Length
                             && line_num > 0
                             )
                            {
                                string target = cc.Convert(xx[2]);
                                int c = Util.CountMatches(lines[line_num - 1], target);
                                lines[line_num - 1] = lines[line_num - 1].Replace(target, xx[3]);
                                if (c == 0) Log.Warn("Cannot Find cc patch target:" + xx[2]);
                                else Log.Info(string.Format("CC patched {0} times for {1}", c, xx[2]));
                            }
                            else
                            { Log.Warn("Bad Line Number:" + xx[1]); }
                        }
                    }


                }
                string body = genHtml.Gen(lines);
                if (f.EndsWith("info.txt") || f.EndsWith("info.atxt"))
                {
                    body = Regex.Replace(body, "<p>(.*?：)", "<p class=\"atxt_keyvalue\">$1");
                    body = "<div class=\"atxt_info\" epub:type=\"acknowledgements\">" + body + "<p>AeroNovelTool EPUB生成器 by AE 生成于" + DateTime.Now + "</p>" +
                    //"<p class=\"keyvalue\">已验证阅读器:<br/>Apple Books<br/>Kindle(使用Kindlegen 转换)<br/>AeroEpubViewer<br/></p>" +
                    "</div>";
                    //File.WriteAllText("info.txt",body);
                }
                if (f.EndsWith("EOB.txt") || f.EndsWith("EOB.atxt"))
                {
                    body = "<div class=\"atxt_info\">" + body +
                    "</div>";
                }
                string xhtml = xhtml_temp.Replace("{❤title}", title).Replace("{❤body}", body);
                TextEpubItemFile item = new TextEpubItemFile("OEBPS/Text/" + xhtml_names[i], xhtml);
                epub.items.Add(item);
                Log.Info("Add xhtml: " + item.fullName + " (title:" + txt_titles[i] + ")");
            }

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
            if (cc_option == ChineseConvertOption.T2S)
            {
                string patch_dir = Path.Combine(dir, "patch_t2s");
                string patch_img_dir = Path.Combine(patch_dir, "Images");
                if (Directory.Exists(patch_dir))
                    if (Directory.Exists(patch_img_dir))
                        foreach (var f in Directory.GetFiles(patch_img_dir))
                        {
                            string fn = Path.GetFileName(f);
                            EpubItemFile img = epub.GetFile<EpubItemFile>("OEBPS/Images/" + fn);
                            if (img != null)
                            {
                                img.data = File.ReadAllBytes(f);
                                Log.Info("T2S:Image Replaced:" + fn);
                            }
                        }

            }

        }
        void GetCss()
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
            string r3 = "<ol>";
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
                        int index = txt_nums.IndexOf(m.Groups[1].Value);
                        string link = "Text/" + xhtml_names[index];
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
                    int index = txt_nums.IndexOf(m.Groups[1].Value);
                    string link = "Text/" + xhtml_names[index];
                    string navTitle = Util.Trim(m.Groups[2].Value);
                    if (navTitle.Length == 0)
                        navTitle = txt_titles[index];
                    if (refered.IndexOf(link) < 0) { refered.Add(link); }
                    r += $"<navPoint id=\"navPoint-{count}\" playOrder=\"{refered.IndexOf(link) + 1}\"><navLabel><text>{navTitle}</text></navLabel><content src=\"{link}\"/></navPoint>\n";
                    if (template3 != "")
                    {
                        r3 += $"<li><a href=\"{link}\">{navTitle}</a></li>\n";
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