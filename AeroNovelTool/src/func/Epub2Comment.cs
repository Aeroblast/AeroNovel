using System.IO;
using System.Text.RegularExpressions;
public class Epub2Comment
{
    public static void Proc(string path)
    {
        Directory.CreateDirectory("epub2comment_output");
        if (!File.Exists(path))
        {
            Log.log("[Error]File not exits!");
            return;
        }
        Epub e = new Epub(path);
        e.items.ForEach(
            (i) =>
            {
                if (typeof(TextItem) == i.GetType() && i.fullName.EndsWith(".xhtml"))
                { ProcXHTML((TextItem)i); }
            }
            );
    }
    static void ProcXHTML(TextItem i)
    {
        Log.log("[Info]" + i.fullName);
        string name = Path.GetFileNameWithoutExtension(i.fullName);
        string r = i.data.Replace("\r", "").Replace("\n", "");
        Match m = Regex.Match(r, "<body(.*)</body>");
        if (!m.Success) { Log.log("[Error]body?"); return; }
        r = m.Groups[0].Value;
        XFragment f = new XFragment(r, 0);
        string txt = "";
        string counter = "";
        string temp = "";
        foreach (var p in f.parts)
        {
            if (p.GetType() == typeof(XText))
            {
                string trimed = Util.Trim(p.originalText);
                txt += trimed;
                counter += trimed;
                if (trimed.Length > 0)
                {
                    temp = "";
                    if (trimed.Contains('「'))
                        temp += "「」";
                    if (trimed.Contains('『'))
                        temp += "『』";
                }
                else
                {
                    temp = "";
                }
            }
            if (p.GetType() == typeof(XTag))
            {
                XTag p0 = (XTag)p;
                if (p0.tagname == "img") { txt += p0.originalText; }
                if (p.type == PartType.tag_start && p0.tagname == "rt") { txt += "("; }
                if (p.type == PartType.tag_end && p0.tagname == "rt") { txt += ")"; }
                if (p.type == PartType.tag_start && p0.tagname == "p") { txt += "##"; }
                if (p.type == PartType.tag_end && p0.tagname == "p") { txt += "\r\n" + temp + "\r\n##——————\r\n"; }
                if (p.type == PartType.tag_end && p0.tagname == "div") { txt += "\r\n\r\n##——————\r\n"; }
            }
        }
        if (Util.Trim(counter).Length > 0)
            File.WriteAllText("epub2comment_output/" + name + ".txt", txt);


    }


}