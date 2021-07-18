using System;
using System.Text;
using System.Net;
using System.IO;
using System.Security.Cryptography;
using System.Web;
using System.Text.Json;
using System.Threading;
using System.Collections.Generic;

class BlackMagic_Dog : TextTranslation
{
    string appId;
    string secretKey;

    int coolDownTime = 1000;
    DateTime lastCall;
    public BlackMagic_Dog()
    {
        if (!File.Exists("baidu_fanyi_keys.txt"))
        {
            throw new Exception("Need baidu_fanyi_keys.txt");
        }
        var t = File.ReadAllLines("baidu_fanyi_keys.txt");
        appId = t[0];
        secretKey = t[1];
    }

    public override string[] Translate(string[] rawLines)
    {
        Console.Write("(=゜ω゜)= ");

        string[] doneLines = new string[rawLines.Length];
        int start = 0, end = 0;
        string rawTemp = "";
        int magicCount = 0;
        for (; end < rawLines.Length;)
        {
            string t = rawLines[end] + "\n";
            if (rawTemp.Length + t.Length > 2000 || end == rawLines.Length - 1)
            {
                magicCount++;
                if (end == rawLines.Length - 1)
                {
                    end++;
                    rawTemp += t;
                }
                //范围index：start <= i < end
                var d = TranslateCall(rawTemp);
                for (int i = start; i < end; i++)
                {
                    //对应回去，解决黑魔法API消除换行符号问题
                    if (rawLines[i] == "")
                    {
                        doneLines[i] = "";
                        continue;
                    }
                    if (d.ContainsKey(rawLines[i]))
                    {
                        doneLines[i] = d[rawLines[i]];
                    }
                    else
                    {
                        Log.Warn("未找到转换结果：" + rawLines[i]);
                    }
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
                doneLines[i] = "w⚠w " + doneLines[i].Replace("“", "「").Replace("”", "」");
            }

        }

        Console.WriteLine();
        Log.Info("Black Translation Magic was shot " + magicCount + " times.");
        return doneLines;

    }

    Dictionary<string, string> TranslateCall(string input)
    {
        Console.Write("—☆ ");
        DateTime lastestVaild = lastCall.AddMilliseconds(coolDownTime);
        while (lastestVaild > DateTime.Now)
        {
            Thread.Sleep(100);
        }

        Dictionary<string, string> result = new Dictionary<string, string>();
        string q = input;
        string from = "jp";
        string to = "zh";
        Random rd = new Random();
        string salt = rd.Next(100000).ToString();
        string sign = EncryptString(appId + q + salt + secretKey);
        string url = "http://api.fanyi.baidu.com/api/trans/vip/translate?";
        url += "q=" + HttpUtility.UrlEncode(q);
        url += "&from=" + from;
        url += "&to=" + to;
        url += "&appid=" + appId;
        url += "&salt=" + salt;
        url += "&sign=" + sign;
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "POST";
        request.ContentType = "text/html;charset=UTF-8";
        request.UserAgent = null;
        request.Timeout = 6000;
        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        using (var stm = response.GetResponseStream())
        using (var rdr = new StreamReader(stm, Encoding.UTF8))
        {
            string responseBody = rdr.ReadToEnd();
            lastCall = DateTime.Now;
            JsonDocument json = JsonDocument.Parse(responseBody);
            JsonElement transResults = json.RootElement.GetProperty("trans_result");
            foreach (JsonElement transResult in transResults.EnumerateArray())
            {
                JsonElement dst = transResult.GetProperty("dst");
                JsonElement src = transResult.GetProperty("src");
                result.TryAdd(src.ToString(), dst.ToString());
            }
            return result;
        }
    }
    static string EncryptString(string str)
    {
        MD5 md5 = MD5.Create();
        // 将字符串转换成字节数组
        byte[] byteOld = Encoding.UTF8.GetBytes(str);
        // 调用加密方法
        byte[] byteNew = md5.ComputeHash(byteOld);
        // 将加密结果转换为字符串
        StringBuilder sb = new StringBuilder();
        foreach (byte b in byteNew)
        {
            // 将字节转换成16进制表示的字符串，
            sb.Append(b.ToString("x2"));
        }
        // 返回加密的字符串
        return sb.ToString();
    }
}