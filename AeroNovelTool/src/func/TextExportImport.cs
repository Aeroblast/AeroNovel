using System;
using System.Text;
using System.Net.Http;
using System.IO;
using System.Web;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;

/// 导出导入文本后合并
abstract class TextSpliteProcess : TextTranslation
{

    public int maxLengthPerCall = 4500;
    public override string[] Translate(string[] rawLines)
    {
        string[] doneLines = new string[rawLines.Length];
        int start = 0, end = 0;
        string rawTemp = "";
        int magicCount = 0;
        for (; end < rawLines.Length;)
        {
            string t = rawLines[end] + "\n";
            if (rawTemp.Length + t.Length > maxLengthPerCall || end == rawLines.Length - 1)
            {
                magicCount++;
                if (end == rawLines.Length - 1)
                {
                    end++;
                    rawTemp += t;
                }
                //范围index：start <= i < end
                var r = TranslateCall(rawTemp);
                if (r.Length != end - start)
                {
                    Log.Warn($"Length not match: {r.Length}-{end - start}");
                }
                for (int i = start; i < start + r.Length; i++)
                {
                    // set result
                    doneLines[i] = r[i - start];
                }

                //准备下一批
                start = end;
                rawTemp = "";
            }
            rawTemp += t;
            end++;
        }
        for (int i = 0; i < doneLines.Length; i++)
        {
            if (doneLines[i] == null)
                doneLines[i] = "";
            else
            {
                doneLines[i] = doneLines[i].Trim();

                doneLines[i] = (!String.IsNullOrEmpty(doneLines[i]) ? "w⚠w" : "")
                 + doneLines[i]
                .Replace("“", "「")
                .Replace("”", "」")
                .Replace("?", "？")
                .Replace("!", "！")
                .Replace("(", "（")
                .Replace(")", "）");
            }

        }

        Console.WriteLine();
        return doneLines;

    }

    public abstract string[] TranslateCall(string content);

}

class TextExport : TextSpliteProcess
{
    public static string i2name(int i, string dir)
    {
        return Path.Combine(dir, $"{i.ToString().PadLeft(3, '0')}.txt");
    }
    string dir = "temp_textexport";
    int fileCount;

    public TextExport()
    {
        fileCount = 0;
    }

    public override string[] TranslateCall(string content)
    {
        File.WriteAllText(i2name(fileCount, dir), content);
        fileCount++;
        return new string[content.Trim().Split("\n").Length];
    }

}

class TextImport : TextSpliteProcess
{
    string dir = "temp_textimport";
    int fileCount;

    public TextImport()
    {
        fileCount = 0;
    }
    public override string[] TranslateCall(string content)
    {
        var path = TextExport.i2name(fileCount, dir);
        fileCount++;
        if (!File.Exists(path))
        {
            Log.Warn("Not exist: " + path);
            return new string[content.Trim().Split("\n").Length];
        }
        var r = File.ReadAllLines(path);
        for (int i = 0; i < r.Length; i++)
        {
            if (r[i].Trim().Length > 0)
            {
                r[i] = r[i];
            }
        }
        return r;
    }

}