using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
public class Atxt2Comment
{
    public TextTranslation textTranslation = null;
    public void ProcessDir(string dir, string outputDir)
    {
        var paths = Directory.GetFiles(dir, "*.atxt");
        foreach (var p in paths)
        {
            ProcessFile(p, Path.Combine(outputDir, Path.GetFileName(p)));
        }
    }
    public void ProcessFile(string path, string outputPath)
    {
        var lines = File.ReadAllLines(path);
        var r = Process(lines);
        File.WriteAllText(outputPath, r);
        Log.Info("wrote: " + outputPath);
    }

    public string Process(string[] lines)
    {
        string[] trans = null;
        if (textTranslation != null)
        {
            trans = textTranslation.Translate(lines);
        }
        StringBuilder sb = new StringBuilder();
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.StartsWith("#"))
            {
                sb.Append(line);
                sb.Append("\n");
            }
            else
            {
                sb.Append("##" + line + "\n");
                if (trans != null)
                {
                    sb.Append(trans[i]);
                }
                sb.Append("\n");

                sb.Append("##————————————————\n");
            }
        }
        return sb.ToString();
    }
}