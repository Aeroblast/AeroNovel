using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
class GlossaryImportation : TextTranslation
{
    public string docPath;
    protected CharNode tree = new CharNode('\0');

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
        var r = new string[lines.Length];
        for (int i = 0; i < lines.Length; i++)
        {
            r[i] = TranslateLine(lines[i]);
        }
        return r;
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
                    string tryGetOutput = GetOutput(temp[i]);
                    if (!string.IsNullOrEmpty(tryGetOutput))
                    {
                        TryAddSpace(ref result, tryGetOutput);
                        result += tryGetOutput;
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

    protected CharNode FindNodeByChar(List<CharNode> nodes, char c)
    {
        foreach (var r in nodes)
        {
            if (r.v == c) return r;
        }
        return null;
    }

    protected string GetOutput(CharNode node)
    {
        do
        {
            if (!string.IsNullOrEmpty(node.output))
            {
                return node.output;
            }
            node = node.parent;
        } while (node.parent != null);
        return null;
    }

    protected void TryAddSpace(ref string result, string added)
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


    protected class CharNode
    {
        public char v;
        public CharNode parent = null;
        public CharNode(char c) { v = c; }
        public CharNode AddChild(char c)
        {
            CharNode child = new CharNode(c);
            child.parent = this;
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