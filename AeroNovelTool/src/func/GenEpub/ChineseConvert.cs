using System;
using System.IO;
using System.Text.RegularExpressions;
using System.IO.Compression;
using System.Collections.Generic;
namespace AeroNovelEpub
{
    public enum ChineseConvertOption
    {
        None = 0,
        T2S

    }
    public class ChineseConvert
    {
        string t2s_c_path = @"dictionary/TSCharacters.txt";
        string t2s_p_path = @"dictionary/TSPhrases.txt";
        Dictionary<string, string> t2s_c_dic = new Dictionary<string, string>();
        Dictionary<string, string> t2s_p_dic = new Dictionary<string, string>();
        bool t2s_ready = false;
        public void Prepare()
        {
            LoadDic(t2s_c_dic, t2s_c_path);
            LoadDic(t2s_p_dic, t2s_p_path);
            t2s_ready = true;
        }
        private void LoadDic(Dictionary<string, string> dic, string path)
        {
            using (var fs = File.OpenRead(path))
            using (StreamReader sr = new StreamReader(fs))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    string[] kv = line.Split('	');
                    if (kv.Length > 1)
                    {
                        dic.Add(kv[0], kv[1].Split(' ')[0]);
                    }
                }
            }
        }
        public string Convert(string s)
        {
            if(!t2s_ready)throw new Exception();
            string r = s;
            foreach (var kw in t2s_p_dic)
            {
                r = r.Replace(kw.Key, kw.Value);
            }
            foreach (var kw in t2s_c_dic)
            {
                r = r.Replace(kw.Key, kw.Value);
            }
            return r;
        }

    }
}