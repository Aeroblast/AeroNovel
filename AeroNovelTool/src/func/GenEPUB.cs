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
        string spine = "";
        string items = "";
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
                Log.log("[Info]Chinese Convert: T2S");
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

            GenFileNames();
            GenContent();
            GetImage();
            GetCss();

            string meta = File.ReadAllText(Path.Combine(dir, "meta.txt"));
            meta = meta.Replace("{urn:uuid}", uid);
            meta = meta.Replace("{date}", DateTime.Today.ToString("yyyy-MM-dd"));
            if (cc != null)
            {
                meta = cc.Convert(meta);
            }
            if (cc_option == ChineseConvertOption.T2S)
            {
                meta = meta.Replace("<dc:language>zh-tw</dc:language>", "<dc:language>zh</dc:language>", true, null);
            }
            string title = Regex.Match(meta, "<dc:title.*?>(.*?)</dc:title>").Groups[1].Value;

            TextEpubItemFile toc = epub.GetFile<TextEpubItemFile>("OEBPS/toc.ncx");
            toc.text = GenTOC(File.ReadAllLines(Path.Combine(dir, "toc.txt")), uid, title, toc.text);

            TextEpubItemFile opf = epub.GetFile<TextEpubItemFile>("OEBPS/content.opf");
            opf.text = string.Format(opf.text, meta, items, spine);

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
                if (Regex.Match(txtname, "^[a-zA-Z0-9]*$").Success)
                {
                    name = "atxt" + txtname + ".xhtml";
                }

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
                foreach (var k in name_dic)
                {
                    if (txtname.Contains(k.Key))
                    {
                        name = "atxt" + no + "" + k.Value + ".xhtml"; break;
                    }
                }
                {
                    var m_num = Regex.Match(txtname, "^第([一二三四五六七八九十百零]{1,10})");
                    if (m_num.Success)
                    {
                        name = "atxt" + no + "_chapter" + Util.FromChineseNumber(m_num.Groups[1].Value) + ".xhtml";
                    }
                    else
                    {
                        m_num = Regex.Match(txtname, "([一二三四五六七八九十百零]{1,10})\\s");
                        if (m_num.Success)
                        {
                            name = "atxt" + no + "_chapter" + Util.FromChineseNumber(m_num.Groups[1].Value) + ".xhtml";
                        }

                    }
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
                                if (c == 0) Log.log("[Warn]Cannot Find cc patch target:" + xx[2]);
                                else Log.log(string.Format("[Info]CC patched {0} times for {1}", c, xx[2]));
                            }
                            else
                            { Log.log("[Warn]Bad Line Number:" + xx[1]); }
                        }
                    }


                }
                string body = genHtml.Gen(lines);
                if (f.EndsWith("info.txt") || f.EndsWith("info.atxt"))
                {
                    body = Regex.Replace(body, "<p>(.*?：)", "<p class=\"keyvalue\">$1");
                    body = "<div class=\"info\" epub:type=\"acknowledgements\">" + body + "<p>AeroNovelTool EPUB生成器by AE " + DateTime.Now + "</p>" +
                    "<p class=\"keyvalue\">推荐使用阅读器:<br/>Apple Books<br/>Kindle(使用Kindlegen 转换)<br/>AeroEpubViewer<br/></p>" +
                    "</div>";
                    //File.WriteAllText("info.txt",body);
                }
                string xhtml = xhtml_temp.Replace("{❤title}", txt_titles[i]).Replace("{❤body}", body);
                TextEpubItemFile item = new TextEpubItemFile("OEBPS/Text/" + xhtml_names[i], xhtml);
                epub.items.Add(item);
                Log.log("[Info]Add xhtml: " + item + " (title:" + txt_titles[i] + ")");
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
                        items += string.Format("    <item id=\"{0}\" href=\"Images/{0}\" media-type=\"{1}\"/>\n", fn, imgtype[ext]);
                        if (!img_names.Contains(fn))
                        {
                            Log.log("[Warn]Unrefered image: " + fn);
                        }
                        Log.log("[Info]Add image: " + fn);
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
                                Log.log("[Info]T2S:Image Replaced:" + fn);
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
                Log.log("[Info]Css added:" + css[0]);
            }
        }
        public string GenTOC(string[] lines, string uid, string title, string template)
        {
            //string temp=File.ReadAllText("template/toc.txt");
            string r = "";
            List<string> label = new List<string>();
            int depth = 1;
            int count = 0;
            foreach (string line in lines)
            {
                Match m = Regex.Match(line, "\\[(.*?)\\]");
                if (m.Success)
                {
                    string tag = m.Groups[1].Value;
                    if (tag[0] == '/')
                    {
                        label.RemoveAt(label.Count - 1);
                        r += "</navPoint>\n";
                    }
                    else
                    {
                        label.Add(tag);
                        if (depth < label.Count + 1) { depth = label.Count + 1; }
                        count++;
                        r += string.Format("<navPoint id=\"navPoint-{0}\" playOrder=\"{0}\"><navLabel><text>{1}</text></navLabel><content src=\"dummylink\"/>\n", count, tag);

                        m = Regex.Match(line.Substring(m.Index + m.Length), "([0-9][0-9])");
                        if (m.Success)
                        {
                            int index = txt_nums.IndexOf(m.Groups[1].Value);
                            string link = "Text/" + xhtml_names[index];
                            r = r.Replace("dummylink", link);
                        }
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
                    r += string.Format("<navPoint id=\"navPoint-{0}\" playOrder=\"{0}\"><navLabel><text>{1}</text></navLabel><content src=\"{2}\"/></navPoint>\n", count, navTitle, link);
                    r = r.Replace("dummylink", link);
                }

            }
            return string.Format(template, uid, depth, title, r);
        }
    }

}