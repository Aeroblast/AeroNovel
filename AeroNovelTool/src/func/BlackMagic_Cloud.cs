using System;
using System.Text;
using System.Net;
using System.IO;
using System.Security.Cryptography;
using System.Web;
using System.Text.Json;
using System.Threading;
using System.Collections.Generic;

class BlackMagic_Cloud : TextTranslation
{
    // string app_key = "";
    // string app_id = "Demo";
    // int coolDownTime = 1000;
    // DateTime lastCall;
    // public BlackMagic_Cloud()
    // {
    //     if (!File.Exists("caiyun_fanyi_keys.txt"))
    //     {
    //         throw new Exception("Need caiyun_fanyi_keys.txt");
    //     }
    //     var t = File.ReadAllLines("caiyun_fanyi_keys.txt");
    //     app_key = t[0];
    // }

    public override string[] Translate(string[] rawLines)
    {
        throw new NotImplementedException();
        // Console.Write("(=゜ω゜)= ");

        // string[] doneLines = new string[rawLines.Length];
        // int magicCount = 0;
        // int linesPerCall = 30;
        // int start = 0;
        // while (start < rawLines.Length)
        // {
        //     if (start + linesPerCall >= rawLines.Length)
        //     {
        //         TranslateCall(rawLines, doneLines, start, rawLines.Length - start);
        //         magicCount++;
        //     }
        //     else
        //     {
        //         TranslateCall(rawLines, doneLines, start, linesPerCall);
        //         magicCount++;
        //     }
        //     start += linesPerCall;
        // }
        // for (int i = 0; i < doneLines.Length; i++)
        // {
        //     if (Util.Trim(doneLines[i]) == "")
        //         doneLines[i] = "";
        //     else
        //     {
        //         doneLines[i] = "w⚠w " + doneLines[i].Replace("“", "「").Replace("”", "」").Replace("‘", "『").Replace("’", "』");
        //     }
        // }

        // Console.WriteLine();
        // Log.Info("Black Magic [Cloud] was shot " + magicCount + " times.");
        // return doneLines;

    }
    // void TranslateCall(string[] rawLines, string[] doneLines, int start, int length)
    // {
    //     
    //     Console.Write("—☆ ");
    //     DateTime lastestVaild = lastCall.AddMilliseconds(coolDownTime);
    //     while (lastestVaild > DateTime.Now)
    //     {
    //         Thread.Sleep(100);
    //     }
    //     string url = "http://api.interpreter.caiyunai.com/v1/translator";
    //     HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
    //     request.Method = "POST";
    //     request.ContentType = "application/json";
    //     request.UserAgent = null;
    //     request.Timeout = 6000;
    //     request.Headers.Set("x-authorization", "token " + app_key);
    //     RequestObject src = new RequestObject();
    //     src.request_id = app_id;
    //     src.trans_type = "ja2zh";
    //     src.detect = true;
    //     src.source = new string[length];
    //     for (int i = 0; i < length; i++)
    //     {
    //         src.source[i] = rawLines[start + i];
    //     }
    //     string srcJson = JsonSerializer.Serialize(src);

    //     using (var stm = request.GetRequestStream())
    //     using (var writer = new StreamWriter(stm))
    //     {
    //         writer.Write(srcJson);
    //     }
    //     HttpWebResponse response = (HttpWebResponse)request.GetResponse();
    //     using (var stm = response.GetResponseStream())
    //     using (var rdr = new StreamReader(stm, Encoding.UTF8))
    //     {
    //         string responseBody = rdr.ReadToEnd();
    //         lastCall = DateTime.Now;
    //         ResponceObject results = JsonSerializer.Deserialize<ResponceObject>(responseBody);
    //         for (int i = 0; i < length; i++)
    //         {
    //             doneLines[start + i] = results.target[i];
    //         }
    //     }
    // }
    class RequestObject
    {
        public string[] source { get; set; }
        public string trans_type { get; set; }
        public string request_id { get; set; }
        public bool detect { get; set; }
    }
    class ResponceObject
    {
        public string[] target { get; set; }
        public float confidence { get; set; }
        public float rc { get; set; }
    }
}