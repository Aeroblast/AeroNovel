using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Xml;

class Util
{
    public static void Packup(string src, string outputfullpath)
    {
        if (File.Exists(outputfullpath))
        {
            File.Delete(outputfullpath);
        }
        ZipFile.CreateFromDirectory(src, outputfullpath);
        Log.log("Saved:" + outputfullpath);
    }
    public static void Unzip(string archive_path, string output_dir)
    {
        if (Directory.Exists(output_dir))
        {
            Directory.CreateDirectory(output_dir);
        }
        ZipArchive archive = ZipFile.OpenRead(archive_path);
        archive.ExtractToDirectory(output_dir);
    }
    public static void DeleteDir(string path)
    {
        if (!Directory.Exists(path)) return;
        foreach (string p in Directory.GetFiles(path)) File.Delete(p);
        foreach (string p in Directory.GetDirectories(path)) DeleteDir(p);
        Directory.Delete(path);
    }
    public static void DeleteEmptyDir(string path)
    {
        if (!Directory.Exists(path)) return;
        foreach (string p in Directory.GetDirectories(path)) DeleteEmptyDir(p);
        if (Directory.GetDirectories(path).Length == 0 && Directory.GetFiles(path).Length == 0)
            Directory.Delete(path);
    }


    public static string ReferPath(string filename, string refPath)
    {
        string r = Path.GetDirectoryName(filename);
        string[] parts = refPath.Replace('/', '\\').Split('\\');
        foreach (string p in parts)
        {
            if (p == "") continue;

            if (p == "..") { r = Path.GetDirectoryName(r); continue; }
            r = Path.Combine(r + "/", p);
        }
        return r;
    }
    public static string Trim(string str)
    {
        int s = 0, e = str.Length - 1;
        for (; s < str.Length; s++) { if (str[s] == ' ' || str[s] == '\t' || str[s] == '\n' || str[s] == '\r') { } else break; }
        for (; e >= 0; e--) { if (str[e] == ' ' || str[e] == '\t' || str[e] == '\n' || str[e] == '\r') { } else break; }
        if (s <= e) return str.Substring(s, e - s + 1);
        else return "";
    }
    public static string TrimTag(string str)
    {
        Regex regex=new Regex("<.*?>");
        return regex.Replace(str,"");
    }
    public static string Number(int number, int length = 4)
    {
        string r = number.ToString();
        for (int j = length - r.Length; j > 0; j--) r = "0" + r;
        return r;
    }
    ///<remarks>
    ///只实现到百
    ///</remarks>
    public static int FromChineseNumber(string s)
    {
        if (s == "零") return 0;
        int r = 0;
        string dic = "零一二三四五六七八九";
        int i = s.Length - 1;
        if (dic.Contains(s[i]))
        {
            r += dic.IndexOf(s[i]);
        }
        i = s.IndexOf('十');
        if (i > 0)//几十几
        {
            r += dic.IndexOf(s[i - 1]) * 10;
        }
        else if (i == 0)//十几
        {
            r += 10;
        }
        i = s.IndexOf('百');
        if (i > 0)
        {
            r += dic.IndexOf(s[i - 1]) * 100;
        }
        if (i == 0)
        {
            throw new Exception("第一个字不能是百。");
        }
        return r;
    }
    public static int CountMatches(string s, string target)
    {
        int counter = 0;
        int startIndex = -1;
        while ((startIndex = (s.IndexOf(target, startIndex + 1))) != -1)
            counter++;
        return counter;
    }
    public static string UrlDecode(string s)
    {
        return Uri.UnescapeDataString(s);
    }
    public static bool Contains(string[] c, string s) { if (c != null) foreach (string x in c) if (x == s) return true; return false; }

}


