using System.IO;
using System.Xml;
using System.Text.RegularExpressions;
public class Html2Comment
{
    public static void Proc(string path)
    {
        string html=File.ReadAllText(path);
        string atxt=ProcXHTML(html);
        File.WriteAllText("output_html2comment.txt",atxt);
        Log.log("[Info]HTML2Comment Saved");
    }
    public static string ProcXHTML(string html)
    {
        XmlDocument doc = new XmlDocument();
        doc.LoadXml(html);
        var body = doc.GetElementsByTagName("body")[0];
        string txt = "";
        string counter = "";
        string lineTemp = "", rubyTemp = "";
        XmlNode p = body.FirstChild;

        bool closingElement = false;
        while (true)
        {
            bool toNext = false;
            if (closingElement)
            {
                closingElement = false;
                toNext = true;
                switch (p.Name)
                {
                    case "p":
                        txt += "##" + lineTemp + rubyTemp + "\n" + RemainSigns(lineTemp) + "\n" + "##————————————————\n";
                        counter += lineTemp;
                        lineTemp = "";
                        rubyTemp = "";
                        break;
                    default:
                        lineTemp += "</" + p.Name + ">";
                        break;
                }
            }
            else
                switch (p.NodeType)
                {
                    case XmlNodeType.Text:
                        lineTemp += p.Value;
                        break;
                    case XmlNodeType.Element:
                        switch (p.Name)
                        {
                            case "ruby":
                                string a = "", b = "";
                                Ruby2Text((XmlElement)p, ref a, ref b);
                                lineTemp += a;
                                rubyTemp += "|" + b;
                                toNext = true;
                                break;
                            case "p":
                                break;
                            case "img":
                                lineTemp += $"<img src=\"{((XmlElement)p).GetAttribute("src")}\" alt={((XmlElement)p).GetAttribute("alt")}>";
                                break;
                            case "image":
                                lineTemp += $"<image href=\"{((XmlElement)p).GetAttribute("href", "xlink")}\">";
                                break;
                            default:
                                if (p.HasChildNodes)
                                    lineTemp += "<" + p.Name + ">";
                                else
                                    lineTemp += "<" + p.Name + "/>";
                                break;
                        }
                        break;
                }
            //move to next
            if (p.HasChildNodes && !toNext)
            {
                p = p.FirstChild;
            }
            else
            if (p.NextSibling == null)
            {
                p = p.ParentNode;
                closingElement = true;
                if (p == body) break;
            }
            else p = p.NextSibling;
        }
        txt += lineTemp;
        return txt;
    }
    static void Ruby2Text(XmlElement ruby, ref string textonly, ref string textwithruby)
    {
        XmlNode p = ruby.FirstChild;
        textonly = "";
        textwithruby = "";
        bool inrt = false;
        bool closingElement = false;
        while (true)
        {
            bool toNext = false;
            if (closingElement)
            {
                closingElement = false;
                toNext = true;
                switch (p.Name)
                {
                    case "rt":
                        textwithruby += ")";
                        inrt = false;
                        break;
                }
            }
            else
                switch (p.NodeType)
                {
                    case XmlNodeType.Text:
                        if (!inrt) textonly += p.Value;
                        textwithruby += p.Value;
                        break;
                    case XmlNodeType.Element:
                        switch (p.Name)
                        {
                            case "rt":
                                textwithruby += "(";
                                inrt = true;
                                break;
                        }
                        break;
                }
            //move to next
            if (p.HasChildNodes && !toNext)
            {
                p = p.FirstChild;
            }
            else
            if (p.NextSibling == null)
            {
                p = p.ParentNode;
                closingElement = true;
                if (p == ruby) break;
            }
            else p = p.NextSibling;
        }
    }

    static string RemainSigns(string s)
    {
        string r = "";
        foreach (char c in s)
        {
            switch (c)
            {
                case '「':
                case '」':
                case '『':
                case '』':
                    r += c;
                    break;
            }
        }
        return r;
    }


}