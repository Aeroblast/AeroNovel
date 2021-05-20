using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
class GlossaryImportation : TextTranslation
{
    string docPath;
    CharNode tree = new CharNode('\0');

    // 迫真字典（笑）
    Dictionary<string, string> dictionary = new Dictionary<string, string>();

    public GlossaryImportation(string docPath)
    {
        this.docPath = docPath;
        string text = File.ReadAllText(docPath);
        foreach (Match m in Regex.Matches(text, "{(.*?),(.*?)}"))
        {
            string key = m.Groups[1].Value;
            string value = m.Groups[2].Value;
            dictionary.TryAdd(key, value);
        }
        dictionary.TryAdd("「", "「");
        dictionary.TryAdd("」", "」");
        dictionary.TryAdd("『", "『");
        dictionary.TryAdd("』", "』");
        dictionary.TryAdd("（", "（");
        dictionary.TryAdd("）", "）");
        dictionary.TryAdd("《", "〔");
        dictionary.TryAdd("》", "〕");
        CreateTree();

    }
    public override string[] Translate(string[] lines)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            lines[i] = TranslateLine(lines[i]);
        }
        return lines;
    }
    public override string ToString()
    {
        return "GlossaryImportation(" + docPath + ")";
    }

    void CreateTree()
    {
        foreach (var kv in dictionary)
        {
            var n = tree;
            for (int i = 0; i < kv.Key.Length; i++)
            {
                var t = FindNodeByChar(n.children, kv.Key[i]);
                if (t == null)
                {
                    n = n.AddChild(kv.Key[i]);
                }
                else { n = t; }

            }
            n.output = kv.Value;
        }
    }
    string TranslateLine(string line)
    {
        List<CharNode> temp = new List<CharNode>();
        string result = "";
        foreach (char c in line)
        {

            for (int i = 0; i < temp.Count; i++)
            {
                var t1 = FindNodeByChar(temp[i].children, c);
                if (t1 != null) { temp[i] = t1; }
                else
                {
                    if (!string.IsNullOrEmpty(temp[i].output))
                    {
                        TryAddSpace(ref result, temp[i].output);
                        result += temp[i].output;
                        temp.Clear();
                        break;
                    }
                    temp.RemoveAt(i);
                    i--;
                }
            }
            var t2 = FindNodeByChar(tree.children, c);
            if (t2 != null)
            {
                temp.Add(t2);
            }
        }
        for (int i = 0; i < temp.Count; i++)
        {
            TryAddSpace(ref result, temp[i].output);
            result += temp[i].output;
        }
        return result;
    }

    CharNode FindNodeByChar(List<CharNode> nodes, char c)
    {
        foreach (var r in nodes)
        {
            if (r.v == c) return r;
        }
        return null;
    }

    void TryAddSpace(ref string result, string added)
    {
        if (result != "")
        {
            if (isBracket(result[result.Length - 1]) || (added == "" || isBracket(added[0])))
            { }
            else
            { result += " "; }
        }
    }
    bool isBracket(char c)
    {
        switch (c)
        {
            case '「':
            case '」':
            case '『':
            case '』':
            case '（':
            case '）':
            case '〔':
            case '〕':
                return true;
        }
        return false;
    }


    private class CharNode
    {
        public char v;
        public CharNode(char c) { v = c; }
        public CharNode AddChild(char c)
        {
            CharNode child = new CharNode(c);
            children.Add(child);
            return child;
        }
        public List<CharNode> children = new List<CharNode>();
        public string output = "";

        public override string ToString()
        {
            return $"\"{v}\"";
        }

    }
}