using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;

public class AtxtProject
{
    public static Regex reg_filename = new Regex("^([0-9]+)(.+)");
    public ProjectConfig config;
    public string dir;
    public List<AtxtSource> srcs = new List<AtxtSource>();

    public List<string> src_paths = new List<string>();
    public bool force_skip_git = false;
    public int id_length = 2;
    bool id_length_changed = false;
    public AtxtProject(string dir, bool loadConfigOnCreation = true)
    {
        this.dir = dir;
        if (loadConfigOnCreation)
        {
            TryLoadConfig();
        }
    }
    public void TryLoadConfig()
    {
        if (File.Exists(Path.Combine(dir, "config.txt")))
        {
            config = new ProjectConfig(File.ReadAllLines(Path.Combine(dir, "config.txt")));
            Log.Info("Read config.txt");
        }
    }
    bool willRunGit
    {
        get
        {
            return !force_skip_git && config != null && config.addSourceInfo == ConfigValue.active;
        }
    }
    public void CollectSource()
    {
        var files = new List<string>(Directory.GetFiles(dir));
        string[] acceptExt = new string[] { ".txt", ".atxt", ".xhtml" };
        files.Sort();
        foreach (string f in files)
        {
            string ext = Path.GetExtension(f);
            string filename = Path.GetFileNameWithoutExtension(f);
            if (!acceptExt.Contains(ext)) continue;
            Match m = reg_filename.Match(filename);
            if (!m.Success) { continue; }
            var id = m.Groups[1].Value;
            if (!id_length_changed && int.Parse(id) == 0)
            {
                if (id.Length != 2)
                {
                    Log.Info($"The atxt ID length has changed to {id.Length} by {filename}");
                    id_length = id.Length;
                    id_length_changed = true;
                }
            }
            if (id_length != id.Length)
            {
                Log.Warn($"Expect id length {id_length} but got {filename}");
                continue;
            }
            src_paths.Add(f);
        }

        if (willRunGit)
        {
            Log.Info("读 git 记录会有点慢，别急");
        }
        foreach (string txt_path in src_paths)
        {
            var src = new AtxtSource(txt_path);
            if (willRunGit)
            {
                src.GetHistory(config.gitMessageRegexMajor);
            }
            srcs.Add(src);
        }
        if (config != null && config.joinCommands.Count > 0)
        {
            Log.Info("Combine Source. Join blank lines: " + config.joinBlankLine);
            foreach (var cmbcmd in config.joinCommands)
            {
                Log.Info($"{cmbcmd}");
                int startIndex = srcs.FindIndex(0, s => s.id.CompareTo(cmbcmd.start) == 0);
                int endIndex = srcs.FindIndex(0, s => s.id.CompareTo(cmbcmd.end) == 0);
                if (startIndex < 0 || endIndex < 0)
                {
                    Log.Error("Failure: " + cmbcmd.ToString());
                    continue;
                }
                string blankLines = new string('\n', config.joinBlankLine);
                var srcsToJoin = srcs.GetRange(startIndex, endIndex - startIndex + 1);
                var contentToJoin = srcsToJoin.Select(s => s.content);
                string content = string.Join(blankLines, contentToJoin);
                srcsToJoin.Sort((s1, s2) => -String.Compare(s1.lastModificationTime, s2.lastModificationTime));
                // to-do check sort
                AtxtSource combined = new AtxtSource($"{cmbcmd.start}.atxt", cmbcmd.start, cmbcmd.title, content);
                combined.lastModificationTime = srcsToJoin[0].lastModificationTime;
                combined.lastComment = srcsToJoin[0].lastComment + $" from {srcsToJoin[0].id} of {cmbcmd.start}-{cmbcmd.end}";
                srcsToJoin.Sort((s1, s2) => -String.Compare(s1.majorVersionTime + ".", s2.majorVersionTime + "."));
                combined.majorVersionTime = srcsToJoin[0].majorVersionTime;
                srcs.RemoveRange(startIndex, endIndex - startIndex + 1);
                srcs.Insert(startIndex, combined);
            }
        }
    }
    public void ApplyAutoSpace()
    {
        if (config == null) return;
        if (config.autoSpace != ConfigValue.active) return;
        foreach (var atxt in srcs)
        {
            if (atxt.title == "info") { continue; }
            AutoSpace.ProcAtxt(atxt);
        }
        Log.Info("Apply autoSpace.");
    }
    public enum MacroMode
    {
        Epub,
        Bbcode,
        InlineHTML
    }

    public Dictionary<string, string> macros;
    public void TryLoadMacro(MacroMode mode)
    {
        int replacement = 1;
        switch (mode)
        {
            case MacroMode.Bbcode: replacement = 2; break;
            case MacroMode.InlineHTML: replacement = 3; break;
        }
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
                else
                {
                    if (s.Length <= replacement)
                    {
                        macros.Add(s[0], s[1]);//fallback to epub mode
                    }
                    else
                    {
                        macros.Add(s[0], s[replacement]);
                    }
                }
            }
        }
    }

    public Dictionary<string, string> web_images;
    public void LoadWebImages()
    {
        web_images = new Dictionary<string, string>();
        string path = Path.Combine(dir, "web_images");
        if (File.Exists(path + ".md"))
        {
            Log.Note("图床链接配置文档读取成功：" + path + ".md");
            string[] a = File.ReadAllLines(path + ".md");
            Regex md_img = new Regex("\\[(.+?)\\]\\((.+?)\\)");
            foreach (var x in a)
            {
                var b = md_img.Match(x);
                if (b.Success)
                {
                    web_images.Add(b.Groups[1].Value, b.Groups[2].Value);
                }
            }
        }
        if (File.Exists(path + ".txt"))
        {
            Log.Note("图床链接配置文档读取成功：" + path + ".txt");
            string[] a = File.ReadAllLines(path + ".txt");
            foreach (var x in a)
            {
                var b = x.Split(' ');
                if (b.Length > 1)
                {
                    web_images.Add(b[0], b[1]);
                }
            }
        }

    }
    public string epubVersion = "3.0";
    public string epubMeta;

    public void TryLoadEpubMeta()
    {
        var metaPath = Path.Combine(dir, "meta3.txt");
        if (File.Exists(metaPath))
        {
            epubVersion = "3.0";
        }
        else
        {
            metaPath = Path.Combine(dir, "meta3.txt");
            if (!File.Exists(metaPath)) throw new Exception("No meta[3].txt!");
            epubVersion = "2.0";
        }
        string meta = File.ReadAllText(metaPath);
        epubMeta = meta.Replace("\r\n", "\n");
    }
}

public partial class AtxtSource
{
    public string content, path, filename;
    public string id, title, ext, xhtmlName;
    public string[] lines
    {
        get
        {
            return content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        }
    }
    public AtxtSource(string path)
    {
        this.path = path;
        filename = Path.GetFileName(path);
        Match m = AtxtProject.reg_filename.Match(Path.GetFileNameWithoutExtension(filename));
        ext = Path.GetExtension(path);
        id = m.Groups[1].Value;
        title = Util.UrlDecode(m.Groups[2].Value);
        xhtmlName = MapFileName(id, title);
        content = File.ReadAllText(path);
    }
    public AtxtSource(string dummy_path, string id, string title, string content)
    {
        this.path = dummy_path;
        ext = ".atxt";
        this.id = id;
        this.title = Util.UrlDecode(title);
        xhtmlName = MapFileName(id, title);
        this.content = content;
    }
    public override string ToString()
    {
        return $"{id}{title}{ext}";
    }
    string MapFileName(string id, string readableName)
    {
        string lowered = readableName;
        string numberMap = "１①Ⅰ";
        foreach (char c in numberMap)
        {
            int block = (int)c - 1;
            for (int i = 0; i <= 9; i++)
            {
                char numberChar = Convert.ToChar(block + i);
                lowered = lowered.Replace(numberChar, (char)('0' + i));
            }
        }
        string trimmed = lowered.Replace("　", "");
        //Name dic start
        Dictionary<string, string> name_dic = new Dictionary<string, string>
                    {
                        {"序章","prologue"},
                        {"终章","epilogue"},
                        {"終章","epilogue"},
                        {"序幕","prologue"},
                        {"尾声","epilogue"},
                        {"简介","summary"},
                        {"簡介","summary"},
                        {"後記","postscript"},
                        {"后记","postscript"},
                        {"目錄","toc"},
                        {"目录","toc"},
                        {"间章","interlude"},
                        {"幕间","interlude"}
                    };

        foreach (var k in name_dic)
        {
            if (trimmed.Contains(k.Key))
            {
                return "atxt" + id + "_" + k.Value + ".xhtml";

            }
        }
        //name dic end

        //chapter number
        {
            string t = trimmed;
            string[] chapterNumberPatterns = new string[]{
                        "^第([一二三四五六七八九十百零\\d]{1,10})",
                        "([一二三四五六七八九十百零\\d]{1,10})\\s",
                        "([一二三四五六七八九十百零\\d]{1,10})章"
                        };
            foreach (string pattern in chapterNumberPatterns)
            {
                var m_num = Regex.Match(t, pattern);
                if (m_num.Success)
                {
                    string chapterNumber = m_num.Groups[1].Value;
                    if (!char.IsDigit(chapterNumber[0])) chapterNumber = "" + Util.FromChineseNumber(chapterNumber);

                    return
                        "atxt" + id + "_chapter"
                        + (chapterNumber.Length == 1 ? "0" : "") + chapterNumber
                        + ".xhtml";
                }
            }
        }
        //chapter numder end

        //just keep ascii
        {
            string t = lowered;
            string name = "_";
            for (int i = 0; i < t.Length; i++)
            {
                if (t[i] < 128)
                {
                    if (t[i] == ' ')
                    {
                        if (i == t.Length - 1) continue;
                        if (name.EndsWith('_')) continue;
                        name += '_'; continue;
                    }
                    if (t[i] == '_' && name.EndsWith('_')) continue;
                    name += t[i];
                }
            }
            if (name.EndsWith('_')) name = name.Substring(0, name.Length - 1);
            return "atxt" + id + name + ".xhtml";
        }
    }
}



public enum ConfigValue
{
    unset = 0,
    active = 1,
    disable = -1
}

public class ProjectConfig
{
    public List<JoinCommand> joinCommands = new List<JoinCommand>();
    public int joinBlankLine = 0;
    public ConfigValue indentAdjust;
    public ConfigValue addInfo;
    public ConfigValue autoSpace;
    public ConfigValue addSourceInfo;
    public string inlinehtmlWrapperStyle, gitMessageRegexMajor;

    public ProjectConfig(string[] content)
    {
        foreach (var line in content)
        {
            var sep = line.IndexOf(':');
            if (sep < 0) continue;
            var cmd = line.Substring(0, sep);
            var arg = line.Substring(sep + 1);
            switch (cmd)
            {
                case "join":
                    joinCommands.Add(new JoinCommand(arg));
                    break;
                case "join_blank_line":
                    int.TryParse(arg, out joinBlankLine);
                    break;
                case "indent_adjust":
                    indentAdjust = GetConfigValue(arg); break;
                case "add_info":
                    addInfo = GetConfigValue(arg); break;
                case "add_source_info":
                    addSourceInfo = GetConfigValue(arg);
                    Log.Info("AddSourceInfo: " + addSourceInfo);
                    break;
                case "auto_space":
                    autoSpace = GetConfigValue(arg);
                    Log.Info("AutoSpace: " + autoSpace);
                    break;
                case "inlinehtml_wrapper_style":
                    inlinehtmlWrapperStyle = arg.Trim();
                    Log.Info("Wrapper div style overrided.");
                    break;
                case "git_message_regex_major":
                    gitMessageRegexMajor = arg.Trim();
                    break;
            }
        }
        joinCommands.Sort((c1, c2) => c1.start.CompareTo(c2.start));
    }
    ConfigValue GetConfigValue(string s)
    {
        s = s.ToLower().Trim();
        switch (s)
        {
            case "true":
            case "yes":
            case "1":
            case "active":
            case "enable":
                return ConfigValue.active;
            case "false":
            case "no":
            case "0":
            case "disable":
                return ConfigValue.disable;
            default:
                return ConfigValue.unset;

        }
    }
}
public class JoinCommand
{
    public string start, end;
    public bool used = false;
    public string title;
    static Regex regex = new Regex("([0-9]{2})-([0-9]{2})(.*)");

    public JoinCommand(string cmd)
    {
        var r = regex.Match(cmd);
        if (!r.Success) throw new Exception("Join Command Fail: " + cmd);
        start = r.Groups[1].Value;
        end = r.Groups[2].Value;
        title = r.Groups[3].Value;
    }

    public override string ToString()
    {
        return $"JoinCommand: {start}-{end} {title}";
    }
}