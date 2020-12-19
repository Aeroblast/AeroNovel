using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Text.RegularExpressions;
public class Html2Comment
{
    public static void Proc(string path)
    {
        string html = File.ReadAllText(path);
        string atxt = ProcXHTML(html);
        File.WriteAllText("output_html2comment.txt", atxt);
        Log.Note("HTML2Comment Saved");
    }
    static string[] notOutputClassNames = new string[]{
        "tcy",//合并竖排标点符号
        "upright",//GAGAGA 似乎调整字的竖排对齐的
        "word-break-break-all",
        "main",
        "line-break-loose word-break-break-all"  //角川系，长省略号破折号
        };
    public static string ProcXHTML(string html, TextTranslation textTranslation = null)
    {
        XmlDocument doc = new XmlDocument();
        doc.LoadXml(html);
        var body = doc.GetElementsByTagName("body")[0];
        string comment = "";
        string lineTemp = "", rubyTemp = "";
        string pureText = "";//for translate
        XmlNode p = body.FirstChild;

        bool closingElement = false;
        List<bool> normalTagOutput = new List<bool>();
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
                        comment += "##" + lineTemp + rubyTemp + "\n" + RemainSigns(lineTemp) + "\n" + "##————————————————\n";
                        lineTemp = "";
                        rubyTemp = "";
                        pureText += "\n";
                        break;
                    default:
                        bool tagOutput = normalTagOutput[normalTagOutput.Count - 1];
                        normalTagOutput.RemoveAt(normalTagOutput.Count - 1);
                        if (tagOutput)
                            lineTemp += "</" + p.Name + ">";
                        break;
                }
            }
            else
                switch (p.NodeType)
                {
                    case XmlNodeType.Text:
                        lineTemp += p.Value;
                        pureText += p.Value;
                        break;
                    case XmlNodeType.Element:
                        switch (p.Name)
                        {
                            case "ruby":
                                string a = "", b = "";
                                Ruby2Text((XmlElement)p, ref a, ref b);
                                lineTemp += a;
                                pureText += a;
                                rubyTemp += "|" + b;
                                toNext = true;
                                break;
                            case "p":
                                break;
                            case "img":
                                lineTemp += $"<img src=\"{((XmlElement)p).GetAttribute("src")}\" alt={((XmlElement)p).GetAttribute("alt")}>";
                                break;
                            case "image":
                                lineTemp += $"<image href=\"{((XmlElement)p).GetAttribute("href", "http://www.w3.org/1999/xlink")}\">";
                                break;
                            default:
                                {
                                    string classTemp = ((XmlElement)p).GetAttribute("class");
                                    bool tagOutput = true;
                                    foreach (string classname in notOutputClassNames)
                                    {
                                        if (classTemp == classname)
                                        {
                                            tagOutput = false;
                                            break;
                                        }
                                    }
                                    if (p.HasChildNodes)
                                        normalTagOutput.Add(tagOutput);
                                    if (tagOutput)
                                    {
                                        if (classTemp.Length > 0) classTemp = " class=" + classTemp;

                                        if (p.HasChildNodes)
                                            lineTemp += "<" + p.Name + classTemp + ">";
                                        else
                                            lineTemp += "<" + p.Name + classTemp + "/>";
                                    }

                                    break;
                                }
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
        comment += lineTemp;
        if (textTranslation != null)
            if (Util.Trim(pureText).Length != 0)
            {
                string[] commentLines = comment.Split('\n');
                while (pureText[pureText.Length - 1] == '\n')
                {
                    pureText = pureText.Substring(0, pureText.Length - 1);
                }
                string[] pureTextLines = pureText.Split('\n');
                var s = textTranslation.Translate(pureTextLines);
                for (int i = 0; i < s.Length; i++)
                {
                    if (s[i] == "") continue;
                    commentLines[i * 3 + 1] = s[i];
                }
                comment = string.Join('\n', commentLines);
            }
        return comment;
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

public abstract class TextTranslation
{
    abstract public string[] Translate(string[] text);
}