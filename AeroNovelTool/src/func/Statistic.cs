using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
class Statistic
{
    public static void AnalyzeProject(string dir, int start = 0)
    {
        Console.WriteLine($"Analyze '{dir}' start from file {start}");
        string[] files = Directory.GetFiles(dir);
        List<string> atxts = new List<string>();
        foreach (string f in files)
        {
            Match m = Regex.Match(Path.GetFileName(f), AeroNovel.regStr_filename);
            if (!m.Success) continue;
            string no = m.Groups[1].Value;
            int chapter = int.Parse(no);
            if (chapter < start) continue;
            //string chaptitle = m.Groups[2].Value;
            atxts.Add(f);

        }
        atxts.Sort();
        int totalLineCount = 0, totalCharCount = 0, totalRawCharCount = 0;
        Console.WriteLine($"     line |  char |   raw");
        foreach (string f in atxts)
        {
            string[] lines = File.ReadAllLines(f);
            var (lineCount, charCount, rawCharCount) = Analyse(lines);
            Console.WriteLine($"{Path.GetFileName(f).Substring(0, 2)}:{lineCount.ToString().PadLeft(7, ' ')}|{charCount.ToString().PadLeft(7, ' ')}|{rawCharCount.ToString().PadLeft(7, ' ')}");
            totalLineCount += lineCount;
            totalCharCount += charCount;
            totalRawCharCount += rawCharCount;
        }
        Console.WriteLine($"All{totalLineCount.ToString().PadLeft(7, ' ')}|{totalCharCount.ToString().PadLeft(7, ' ')}|{totalRawCharCount.ToString().PadLeft(7, ' ')}");
    }

    public static (int lineCount, int charCount, int rawCharCount) Analyse(string[] lines)
    {
        int lineCount = 0, charCount = 0, rawCharCount = 0;

        const string reg_noteref = "\\[note\\]";
        const string reg_notecontent = "\\[note=(.*?)\\]";
        const string reg_img = "\\[img\\](.*?)\\[\\/img\\]";
        const string reg_illu = "^\\[illu\\](.*?)\\[\\/illu\\]$";
        const string reg_illu2 = "^#illu:(.*)";
        const string reg_imgchar = "\\[imgchar\\](.*?)\\[\\/imgchar\\]";
        const string reg_class = "\\[class=(.*?)\\](.*?)\\[\\/class\\]";
        const string reg_chapter = "\\[chapter=(.*?)\\](.*?)\\[\\/chapter\\]";
        Dictionary<string, string> reg_dic = new Dictionary<string, string>
            {
                {"^\\[align=(.*?)\\](.*?)\\[\\/align\\]$","$2"},
                {"^\\[center\\](.*?)\\[\\/center\\]$","$1"},
                {"^\\[right\\](.*?)\\[\\/right\\]$","$1"},
                {"^\\[left\\](.*?)\\[\\/left\\]$","$1"},
                {reg_illu,""},
                {"^\\[title\\](.*?)\\[\\/title\\]$","$1"},
                {"^\\[h1\\](.*?)\\[\\/h1\\]$","$1"},
                {"^\\[h2\\](.*?)\\[\\/h2\\]$","$1"},
                {"^\\[h3\\](.*?)\\[\\/h3\\]$","$1"},
                {"^\\[h4\\](.*?)\\[\\/h4\\]$","$1"},
                {"^\\[h5\\](.*?)\\[\\/h5\\]$","$1"},
                {"^\\[h6\\](.*?)\\[\\/h6\\]$","$1"},
                ///以上做旧版兼容，找个时机扫进垃圾堆
                
                ///优先去除注释
                {"/\\*.*?\\*/",""},
                {"///.*",""},

                {"^#center:(.*)","$1"},
                {"^#right:(.*)","$1"},
                {"^#left:(.*)","$1"},
                {reg_noteref,""},
                {reg_notecontent,"$1"},
                {reg_img,""},
                {reg_illu2,""},
                {reg_imgchar,""},
                {reg_class,""},
                {reg_chapter,"$2"},
                {"\\[b\\](.*?)\\[\\/b\\]","$1"},
                {"^#title:(.*)","$1"},
                {"\\[ruby=(.*?)\\](.*?)\\[\\/ruby\\]","$2$1"},
                {"^\\[pagebreak\\]$",""},
                {"\\[emphasis\\](.*?)\\[\\/emphasis\\]","$1"},
                {"\\[s\\](.*?)\\[\\/s\\]","$1"},
                {"\\[i\\](.*?)\\[\\/i\\]","$1"},
                {"\\[color=(.*?)\\](.*?)\\[\\/color\\]","$2"},
                {"\\[size=(.*?)\\](.*?)\\[\\/size\\]","$2"},
                {"^#h1:(.*)","$1"},
                {"^#h2:(.*)","$1"},
                {"^#h3:(.*)","$1"},
                {"^#h4:(.*)","$1"},
                {"^#h5:(.*)","$1"},
                {"^#h6:(.*)","$1"},
                {"^#class:(.*)",""},
                {"^#/class",""},
                {"\\[font\\](.*?)\\[\\/font\\]","$1"},
                {"\\[url=(.*?)\\](.*?)\\[\\/url\\]","$2"},
            };
        string cleaned = "";
        foreach (string line in lines)
        {
            if (line.StartsWith("##"))
            {
                string s = line.Replace("—", "");
                s = Regex.Replace(s, "<.*?>", "");
                int start = 2;
                if (s.Length < 3) continue;
                if (s[2] == '　') start++;
                int end = s.IndexOf('|');
                if (end < 0) end = s.Length - 1;
                lineCount++;
                rawCharCount += (end - start);
                continue;
            }

            Match m = Regex.Match("", "1");
            string r = line;
            if (macros != null)
            {
                do
                {
                    foreach (var pair in macros)
                    {
                        m = Regex.Match(r, pair.Key);
                        if (m.Success)
                        {
                            Regex reg = new Regex(pair.Key);
                            r = reg.Replace(r, pair.Value);
                            break;
                        }
                    }
                } while (m.Success);
            }

            if (r.StartsWith("#HTML:"))
            {
                cleaned += r.Substring("#HTML:".Length) + "\n";
                continue;
            }

            do
            {
                foreach (var pair in reg_dic)
                {
                    m = Regex.Match(r, pair.Key);
                    if (m.Success)
                    {
                        Regex reg = new Regex(pair.Key);
                        switch (pair.Key)
                        {
                            default:
                                r = reg.Replace(r, pair.Value);
                                break;
                        }
                        break;
                    }

                }
            } while (m.Success);
            cleaned += r + "\n";
            charCount += r.Length;
        }
        return (lineCount, charCount, rawCharCount);
    }

    static Dictionary<string, string> macros;
    static void ReadConfig(string dir)
    {
        if (File.Exists(Path.Combine(dir, "macros.txt")))
        {
            Log.Info("Read macros.txt");
            string[] macros_raw = File.ReadAllLines(Path.Combine(dir, "macros.txt"));
            macros = new Dictionary<string, string>();
            foreach (string macro in macros_raw)
            {
                string[] s = macro.Split('\t');
                if (s.Length < 2)
                {
                    Log.Warn("Macro defination is not complete. Use tab to separate: " + macro);
                }
                else if (s.Length == 2)
                {
                    macros.Add(s[0], s[1]);
                }
                else//length>2
                {
                    macros.Add(s[0], s[2]);
                }

            }

        }
    }
}