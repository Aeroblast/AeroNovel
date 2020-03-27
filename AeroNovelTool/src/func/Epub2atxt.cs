using System.IO;
using System.Text.RegularExpressions;
public class Epub2Atxt
{
    const string output_dir = "epub2atxt_output/";
    public static void Proc(string path)
    {
        Directory.CreateDirectory(output_dir);
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
        foreach (var p in f.parts)
        {
            if (p.GetType() == typeof(XText))
            {
                string trimed = Util.Trim(p.originalText);
                txt += trimed;
                counter += trimed;
                continue;
            }
            if (p.GetType() == typeof(XTag))
            {
                XTag p0 = (XTag)p;
                if (p.type == PartType.tag_start)
                {
                    switch (p0.tagname)
                    {
                        case "h1":
                        case "h2":
                        case "h3":
                        case "h4":
                        case "h5":
                        case "h6":
                            txt += "[" + p0.tagname + "]";
                            continue;
                        case "body":
                            continue;
                    }
                }
                if (p.type == PartType.tag_end)
                {
                    switch (p0.tagname)
                    {
                        case "h1":
                        case "h2":
                        case "h3":
                        case "h4":
                        case "h5":
                        case "h6":
                            txt += "[/" + p0.tagname + "]\r\n";
                            continue;
                        case "body":
                            continue;
                    }
                }
                if (p.type == PartType.tag_end && p0.tagname == "div") { txt += "</div>\r\n"; continue; }
                if (p.type == PartType.tag_start && p0.tagname == "p") { continue; }
                if (p.type == PartType.tag_end && p0.tagname == "p") { txt += "\r\n"; continue; }
                txt += p0.originalText;
            }
        }
        if (Util.Trim(counter).Length > 0)
            File.WriteAllText(output_dir + name + ".txt", txt);


    }


}