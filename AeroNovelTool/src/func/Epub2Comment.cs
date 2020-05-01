using System.IO;
using System.Xml;
using System.Text.RegularExpressions;
public class Epub2Comment
{
    const string output_path="output_epub2comment/";
    public static void Proc(string path)
    {
        Log.log("[Info]Epub2Comment");
        Directory.CreateDirectory(output_path);
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
                {
                    var t = (TextItem)i;
                    var txt = Html2Comment.ProcXHTML(t.data);
                    var p = output_path + Path.GetFileNameWithoutExtension(i.fullName) + ".txt";
                    File.WriteAllText(p, txt);
                    Log.log("[Info]" + p);
                }
            }
            );
    }

}