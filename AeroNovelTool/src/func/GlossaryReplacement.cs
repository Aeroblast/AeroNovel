using System.Collections.Generic;
using System.IO;
using System;
using System.Text;
using System.Text.RegularExpressions;

class GlossaryReplacement : GlossaryImportation
{
    public GlossaryReplacement(string docPath) : base(docPath)
    {


    }

    public string TranslateLine(string line)
    {
        List<CharNode> temp = new List<CharNode>();

        StringBuilder result = new StringBuilder();
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
                        result.Append(tryGetOutput);
                        temp.Clear();
                        break;
                    }
                    if (i == 0)
                    {
                        CharNode tn = temp[i];
                        string recover = "";
                        while (tn.v != '\0')
                        {
                            recover = tn.v + recover;
                            tn = tn.parent;
                        }
                        string compare = "";
                        if (i + 1 < temp.Count)
                        {
                            tn = temp[i + 1];
                            while (tn.v != '\0')
                            {
                                compare = tn.v + compare;
                                tn = tn.parent;
                            }
                        }
                        result.Append(recover.Substring(0, recover.Length - compare.Length));
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

            if (temp.Count == 0)
            {
                result.Append(c);
            }
        }
        for (int i = 0; i < temp.Count; i++)
        {
            result.Append(temp[i].output);
        }
        return result.ToString();
    }
}