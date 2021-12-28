using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.IO.Compression;
using System.Collections.Generic;
class GenBbcode
{

    AtxtProject project;
    static string[] additional_msg = new string[] { };

    public GenBbcode(string dir)
    {
        project = new AtxtProject(dir);
        project.LoadMacro(AtxtProject.MacroMode.Bbcode);
        project.LoadWebImages();
        project.CollectSource();
    }
    public static void ConvertFile(string path, string outputPath)
    {
        string dir = Path.GetDirectoryName(path);
        var inst = new GenBbcode(dir);
        var r = inst.GenBody(File.ReadAllLines(path));
        File.WriteAllText(outputPath, r);
        Log.Note("Saved: " + outputPath);
    }
    public static void ConvertDir(string path, string outputPath)
    {
        var inst = new GenBbcode(path);
        string r = inst.GenSingle();
        File.WriteAllText(outputPath, r);
        Log.Note("Saved: " + outputPath);
    }
    public string GenSingle()
    {
        Log.Note("bbcode single file generation.");
        StringBuilder stringBuilder = new StringBuilder();
        foreach (var f in project.srcs)
        {
            if (f.title == "EOB")
            {
                Log.Info("Skip " + f.title + " (End of Book)");
                continue;
            }
            if (f.ext.EndsWith("html"))
            {
                Log.Info("Skip HTML File: " + f.title);
                continue;
            }
            Log.Info("Processing " + f.title);
            string body = GenBody(f.lines);
            if (f.title == "info")
            {
                body = Regex.Replace(body, "(^|[\\n])　　", "$1");
            }
            stringBuilder.Append(body);
            stringBuilder.Append("======================\r\n");
        }
        return stringBuilder.ToString();
    }

    string GenBody(string[] txt)
    {

        const string reg_noteref = "\\[note\\]";
        const string reg_notecontent = "\\[note=(.*?)\\]";
        const string reg_img = "\\[img\\]((?!http).*?)\\[\\/img\\]";
        const string reg_illu = "\\[illu\\]((?!http).*?)\\[\\/illu\\]";
        const string reg_illu2 = "^#illu:(.*)";
        const string reg_imgchar = "\\[imgchar\\]((?!http).*?)\\[\\/imgchar\\]";
        const string reg_class = "\\[class=(.*?)\\](.*?)\\[\\/class\\]";
        const string reg_chapter = "\\[chapter=(.*?)\\](.*?)\\[\\/chapter\\]";
        Dictionary<string, string> reg_dic_comment = new Dictionary<string, string>{
                {"/\\*.*?\\*/",""},
                {"///.*",""},
            };
        Dictionary<string, string> reg_dic = new Dictionary<string, string>
            {
                //{"\\[align=(.*?)\\](.*?)\\[\\/align\\]","<p class=\"aligned\" style=\"text-align:$1\">$2</p>"},
                {"^\\[center\\](.*?)\\[\\/center\\]$","[align=center]$1[/align]"},
                {"^\\[right\\](.*?)\\[\\/right\\]$","[align=right]$1[/align]"},
                {"^\\[left\\](.*?)\\[\\/left\\]$","[align=left]$1[/align]"},
                {"^#left:(.*)","[align=left]$1[/align]"},
                {"^#center:(.*)","[align=center]$1[/align]"},
                {"^#right:(.*)","[align=right]$1[/align]"},
                {reg_noteref,"[color=#00ffff][ruby=注][size=1]　[/size][/ruby][/color]"},
                {reg_notecontent,"\r\n[align=right][size=1][color=#00ffff]$1[/color][/size][/align]"},
                {reg_img,""},
                {reg_illu,""},
                {reg_illu2,""},
                {reg_imgchar,""},
                {reg_class,"$2"},
                {reg_chapter,"$2"},
                //{"\\[b\\](.*?)\\[\\/b\\]","<b>$1</b>"},
                {"\\[title\\](.*?)\\[\\/title\\]","[size=5]$1[/size]"},
                {"^#title:(.*)","[align=center][size=5]$1[/size][/align]\n"},
                //{"\\[ruby=(.*?)\\](.*?)\\[\\/ruby\\]","<ruby>$2<rt>$1</rt></ruby>"},
                {"\\[pagebreak\\]",""},
                {"\\[emphasis\\](.*?)\\[\\/emphasis\\]","[b]$1[/b]"},
                //{"\\[s\\](.*?)\\[\\/s\\]","<s>$1</s>"},
                //{"\\[i\\](.*?)\\[\\/i\\]","<i>$1</i>"},
                //{"\\[color=(.*?)\\](.*?)\\[\\/color\\]","<span style=\"color:$1\">$2</span>"},
                //{"\\[size=(.*?)\\](.*?)\\[\\/size\\]","<span style=\"font-size:$1em\">$2</span>"}
                {"^#class:(.*)",""},
                {"^#/class",""},
                {"^#h1:(.*)","[size=3][b]$1[/b][/size]"},
                {"^#h2:(.*)","[size=3][b]$1[/b][/size]"},
                {"^#h3:(.*)","[size=4][b]$1[/b][/size]"},
                {"^#h4:(.*)","[size=4][b]$1[/b][/size]"},
                {"^#h5:(.*)","[size=5][b]$1[/b][/size]"},
                {"^#h6:(.*)","[size=5][b]$1[/b][/size]"},
            };
        string bbcode = "";
        int addmessagecount = 0;
        foreach (string line in txt)
        {
            if (line.StartsWith("##")) continue;
            if (line.StartsWith("#HTML")) continue;
            string r = line;
            Match m = Regex.Match("", "1");

            do
            {
                foreach (var pair in reg_dic_comment)
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
            //macros
            if (project.macros != null)
            {
                int executionCount = 0;
                do
                {
                    string safeCheck = r;
                    foreach (var pair in project.macros)
                    {
                        m = Regex.Match(r, pair.Key);
                        if (m.Success)
                        {
                            Regex reg = new Regex(pair.Key);
                            r = reg.Replace(r, pair.Value);
                            executionCount++;
                            if (r == safeCheck) continue;
                            break;
                        }
                    }
                    if (r == safeCheck) break;
                    if (executionCount > 100)
                    {
                        Log.Error("Macro: Max count");
                        Log.Error(r);
                        break;
                    }
                } while (m.Success);
            }
            if (r.StartsWith("##")) continue;
            if (r.StartsWith("#HTML")) continue;

            //regular
            do
            {
                foreach (var kv in reg_dic)
                {
                    m = Regex.Match(r, kv.Key);
                    if (m.Success)
                    {
                        Regex reg = new Regex(kv.Key);
                        switch (kv.Key)
                        {
                            case reg_img:
                            case reg_illu:
                            case reg_illu2:
                            case reg_imgchar:
                                {
                                    var a = m.Groups[1].Value;
                                    if (project.web_images.ContainsKey(a))
                                    {
                                        r = r.Replace(m.Value, "[img]" + project.web_images[a] + "[/img]");
                                    }
                                    else
                                    {
                                        if (kv.Key == reg_illu2 || kv.Key == reg_illu)
                                            r = r.Replace(m.Value, "【没传图床的图片：" + a + "】");
                                        r = r.Replace(m.Value, "");
                                        Log.Warn("没传图床的图片：" + a);
                                    }
                                }
                                break;
                            default:
                                r = reg.Replace(r, kv.Value);
                                break;
                        }
                        break;
                    }

                }
            } while (m.Success);
            string checkhead = Regex.Replace(r, "\\[.*?\\]", "");
            if (checkhead.Length > 0)
            {
                if (Util.IsNeedAdjustIndent(checkhead[0]))
                {
                    r = "　" + r;
                }
                else
                {
                    r = "　　" + r;
                }
            }
            if (r == "　　" || r == "")
            {
                addmessagecount++;
                if (addmessagecount == 11)
                    r = GetRandomMessage();
            }
            else
            {
                if (addmessagecount > 10)
                    addmessagecount--;
            }
            if (r.EndsWith("[/align]"))
                bbcode += r;
            else
                bbcode += r + "\r\n";
        }

        return bbcode;
    }
    static Random random = new Random();
    static string GetRandomMessage()
    {
        if (additional_msg.Length == 0)
            return "";
        if (random.Next(0, 100) > 60) return "";
        string inserter = " 　_-^！!~～+='·.．`";
        string s = additional_msg[random.Next(0, additional_msg.Length)];
        char ins = inserter[random.Next(0, inserter.Length)];
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append("[color=white]");
        for (int i = 0; i < s.Length; i++)
        {
            if (random.Next(0, 100) > 60) stringBuilder.Append(ins);
            if (random.Next(0, 100) > 60) stringBuilder.Append(ins);
            stringBuilder.Append(s[i]);
            if (random.Next(0, 100) > 60) stringBuilder.Append(ins);
        }
        stringBuilder.Append("[/color]");
        return stringBuilder.ToString();
    }

}

