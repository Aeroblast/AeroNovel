using System.Net.Http;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;


class WebSource
{
    const string xhtml = 
@"<?xml version=""1.0"" encoding=""utf-8""?>
<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.1//EN""
  ""http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd\"">
<html xmlns=""http://www.w3.org/1999/xhtml\"">
<head>
</head>
<body>
{0}
</body>
</html>";
    public static string KakuyomuEpisodes(string url)
    {
        string raw = GetSource(url);
        Regex regex = new Regex("<div class=\"widget-episodeBody js-episode-body[\\s\\S]*?</div>");
        string part = regex.Match(raw).Value;
        Log.log("[Info]Got text.");
        return string.Format(xhtml, part);
    }
    static string GetSource(string url)
    {
        Log.log("[Info]Try dl "+url);
        HttpWebRequest req = HttpWebRequest.CreateHttp(url);
        using (var res = req.GetResponse())
        using (var s = res.GetResponseStream())
        using (var reader = new StreamReader(s))
        {
            return reader.ReadToEnd();
        }
    }
}