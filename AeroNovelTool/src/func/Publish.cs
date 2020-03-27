using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class Publish
{
    public static void Restore(string dir)
    {
        string[] files = Directory.GetFiles(dir);
        string contents="";
        List<string> items=new List<string>();
        List<string> num=new List<string>();
        foreach (string f in files)
        {
            Match m = Regex.Match(Path.GetFileName(f), AeroNovel.filename_reg);
            if (!m.Success) continue;
            contents+=Path.GetFileName(f)+"\r\n";
            string no = m.Groups[1].Value;
            items.Add(Path.GetFileName(f));
            num.Add(no);
        }
        File.WriteAllText(Path.Combine(dir,"contents.txt"),contents);
        //string toc_path=Path.Combine(dir,"toc.txt");

    }
}