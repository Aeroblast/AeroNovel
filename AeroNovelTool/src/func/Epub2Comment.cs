using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AeroEpub.Epub;
public class Epub2Comment
{
    string output_path = "output_epub2comment/";
    public EpubFile epub;

    public List<TextTranslation> textTranslation = new List<TextTranslation>();
    public Epub2Comment(string path)
    {
        if (!File.Exists(path))
        {
            throw new Exception("File not exits!");
        }
        epub = new EpubFile(path);
        output_path = "output_epub2comment_" + Util.FilenameCheck(epub.title) + "/";
    }
    public void Proc()
    {
        Log.Note("Epub2Comment");


        if (textTranslation.Count != 0)
        {
            var names = textTranslation.Select(x => x.ToString());
            Log.Note("Text Translation Method: " + string.Join(", ", names));
        }



        Directory.CreateDirectory(output_path);
        try
        {
            if (epub.toc.mediaType == "application/x-dtbncx+xml")
            {
                Parse2();
            }
            else
            {
                Parse3();
            }
        }
        catch (Exception)
        {
            Log.Warn("尝试序列化失败。");
            tocTree = null;
        }

        var plain = GetPlainStruct();
        for (int i = 0; i < plain.Length; i++)
        {
            var t = epub.spine[i].item.GetFile() as TextEpubItemFile;
            var txt = Html2Comment.ProcXHTML(t.text, textTranslation);
            var p = output_path + "i" + Util.Number(i, 2) + "_" + Path.GetFileNameWithoutExtension(t.fullName) + Util.FilenameCheck(plain[i]) + ".txt";
            File.WriteAllText(p, txt);
            Log.Note(p);
        }
    }
    static TocItem tocTree;
    static string tocPath;
    void Parse2()
    {
        var f = epub.toc.GetFile() as TextEpubItemFile;
        tocPath = f.fullName;
        XmlDocument xml = new XmlDocument();
        xml.LoadXml(f.text);
        var root = xml.GetElementsByTagName("navMap")[0];
        tocTree = new TocItem(epub);
        tocTree.children = new List<TocItem>();
        Parse2Helper(root, tocTree);

    }
    void Parse2Helper(XmlNode px, TocItem pt)
    {
        foreach (XmlNode e in px.ChildNodes)
        {
            switch (e.Name)
            {
                case "navLabel":
                    {
                        pt.name = e.InnerText;
                    }
                    break;
                case "content":
                    {
                        pt.url = Util.ReferPath(tocPath, e.Attributes["src"].Value);
                    }
                    break;
                case "navPoint":
                    {
                        var n = pt.AddChild();
                        Parse2Helper(e, n);
                    }
                    break;
            }
        }
    }
    //http://idpf.org/epub/30/spec/epub30-contentdocs.html#sec-xhtml-nav-def-model
    public void Parse3()
    {
        var f = epub.toc.GetFile() as TextEpubItemFile;

        tocPath = f.fullName;
        XmlDocument xml = new XmlDocument();
        xml.LoadXml(f.text);
        var navs = xml.GetElementsByTagName("nav");
        foreach (XmlElement nav in navs)
        {
            if (nav.GetAttribute("epub:type") == "toc")
            {
                tocTree = new TocItem(epub);
                tocTree.children = new List<TocItem>();
                var root = nav.GetElementsByTagName("ol")[0];
                Parse3Helper(root, tocTree);
                return;
            }
        }
        //We have <nav>, but no epub:type is toc, so last try:
        if (navs.Count > 0)
        {
            var nav = navs[0] as XmlElement;
            tocTree = new TocItem(epub);
            tocTree.children = new List<TocItem>();
            var root = nav.GetElementsByTagName("ol")[0];
            Parse3Helper(root, tocTree);
        }
    }
    void Parse3Helper(XmlNode px, TocItem pt)
    {
        foreach (XmlNode e in px.ChildNodes)
            if (e.Name == "li")
            {
                var node = pt.AddChild();
                foreach (XmlNode a in e.ChildNodes)
                {
                    if (a.Name == "a" && node.name == "")
                    {
                        node.name = a.InnerText;
                        node.url = Util.ReferPath(tocPath, ((XmlElement)a).GetAttribute("href"));
                        continue;
                    }
                    if (a.Name == "span" && node.name == "")
                    {
                        node.name = a.InnerText;
                        continue;
                    }
                    if (a.Name == "ol")
                    {
                        Parse3Helper(a, node);
                    }
                }
            }
    }
    public string[] GetPlainStruct()
    {
        List<string> urls = new List<string>();
        foreach (SpineItemref i in epub.spine)
        {
            if (!i.linear) continue;
            urls.Add(i.href);
        }
        string[] plain = new string[urls.Count];
        if (tocTree == null)
        {
            for (int i = 0; i < plain.Length; i++) plain[i] = "";
            return plain;
        }
        GetPlainStructHelper(urls, tocTree, ref plain);
        return plain;
    }
    static void GetPlainStructHelper(List<string> urls, TocItem p, ref string[] plain, string intro = "")
    {
        foreach (TocItem i in p.children)
        {
            if (i.url != null)
            {
                string u = i.url.Split('#')[0];
                int index = urls.IndexOf(u);
                if (index >= 0)
                {
                    if (plain[index] == null)
                        plain[index] = intro + i.name;
                }
            }
            if (i.children != null)
                GetPlainStructHelper(urls, i, ref plain, intro + i.name + " > ");
        }
    }

    class TocItem
    {
        EpubFile belongTo;
        public TocItem(EpubFile epub)
        {
            belongTo = epub;
        }
        public List<TocItem> children;
        public TocItem parent;
        string _name = "";
        public string name
        {
            get { return _name; }
            set { _name = Util.Trim(value); }
        }
        string _url;
        public string url
        {
            set
            {
                _url = value;
                int i = 0;
                var spl = _url.Split('#');
                var path = spl[0];
                foreach (SpineItemref itemref in belongTo.spine)
                {
                    if (itemref.href == path)
                    {
                        docIndex = i;
                        return;
                    }
                    i++;
                }
                throw new EpubErrorException("Error at parse toc");
            }
            get { return _url; }
        }
        public int docIndex;
        public TocItem AddChild()
        {
            if (children == null) children = new List<TocItem>();
            TocItem n = new TocItem(belongTo);
            n.parent = this;
            children.Add(n);
            return n;
        }
        public override string ToString()
        {
            string s = name;
            if (parent != null)
            {
                var t = parent.ToString();
                if (t.Length > 0)
                    s = parent.ToString() + " > " + s;
            }
            return s;
        }
    }

}