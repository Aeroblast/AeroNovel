using System.IO;
using System.Text.RegularExpressions;

namespace AeroNovelEpub
{
    class AtxtChineseConvert
    {

        public static void ProcT2C(string path, bool replaceOriginal)
        {
            string[] lines = File.ReadAllLines(path);
            string outpath;
            if (replaceOriginal)
                outpath = path;
            else
                outpath = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + "_cc" + Path.GetExtension(path));
            ChineseConvert cc = new ChineseConvert();
            cc.Prepare();
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("##")) continue;
                lines[i] = cc.Convert(lines[i]);
            }
            File.WriteAllLines(outpath, lines);
            Log.Note("Output: " + outpath);
        }

    }
}