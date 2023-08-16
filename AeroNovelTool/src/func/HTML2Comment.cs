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
        "line-break-loose word-break-break-all","word-break-break-all line-break-loose",  //角川系，长省略号破折号
        "koboSpan",
        "tcy unified-e-q","tcy unified-e-e"//星海社
        };
    public static string ProcXHTML(string html, List<TextTranslation> textTranslation = null)
    {
        XmlDocument doc = new XmlDocument();
        doc.LoadXml(html);
        var body = doc.GetElementsByTagName("body")[0];
        string comment = "";
        string lineTemp = "", rubyTemp = "";
        string pureText = "";//for translate
        XmlNode p = body.FirstChild;

        bool closingElement = false;
        List<string> normalTagEndOutput = new List<string>();
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
                    case "em":
                        lineTemp += "[/em]";
                        break;
                    default:
                        string tagOutput = normalTagEndOutput[normalTagEndOutput.Count - 1];
                        normalTagEndOutput.RemoveAt(normalTagEndOutput.Count - 1);
                        lineTemp += tagOutput;
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
                            case "em":
                                lineTemp += "[em]";
                                break;
                            default:
                                {
                                    //一般tag：输出带className
                                    //检测到已知tag：不输出tag
                                    //检测到重点号：[b]
                                    //输出加入normalTagEndOutput
                                    string classTemp = ((XmlElement)p).GetAttribute("class");
                                    string tagEndOutput = "";
                                    bool needOutput = true;
                                    foreach (string classname in notOutputClassNames)
                                    {
                                        if (classTemp == classname)
                                        {
                                            needOutput = false;
                                            break;
                                        }
                                    }

                                    if (needOutput)
                                    {
                                        if (classTemp == "em-sesame")
                                        {
                                            lineTemp += "[em]";
                                            tagEndOutput = "[/em]";
                                        }
                                        else if (classTemp == "bold")
                                        {
                                            lineTemp += "[b]";
                                            tagEndOutput = "[/b]";
                                        }
                                        else
                                        {
                                            tagEndOutput = $"</{((XmlElement)p).Name}>";
                                            if (classTemp.Length > 0)
                                            {
                                                classTemp = " class=\"" + classTemp + "\"";
                                            }
                                            if (p.HasChildNodes)
                                            {
                                                lineTemp += "<" + p.Name + classTemp + ">";
                                            }
                                            else
                                                lineTemp += "<" + p.Name + classTemp + "/>";
                                        }
                                    }

                                    if (p.HasChildNodes)
                                        normalTagEndOutput.Add(tagEndOutput);

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
        comment += "## " + lineTemp;
        if (textTranslation != null)
            if (Util.Trim(pureText).Length != 0)
            {
                string[] commentLines = comment.Split('\n');
                for (int i = 0; i * 3 + 1 < commentLines.Length; i++)
                {
                    commentLines[i * 3 + 1] = "";
                }
                pureText = pureText.TrimEnd();
                string[] pureTextLines = pureText.Split('\n');
                foreach (var trans in textTranslation)
                {
                    var s = trans.Translate(pureTextLines);
                    for (int i = 0; i < s.Length; i++)
                    {
                        if (!string.IsNullOrEmpty(commentLines[i * 3 + 1])
                            && !string.IsNullOrEmpty(s[i].Trim()))
                        { commentLines[i * 3 + 1] += "/"; }
                        commentLines[i * 3 + 1] += s[i];
                    }
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
                case '〈':
                case '〉':
                    r += c;
                    break;
                case '《':
                    r += '〔';
                    break;
                case '》':
                    r += '〕';
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