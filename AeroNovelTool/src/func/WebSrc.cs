using System.Net.Http;
using System.Net;
using System;
using System.IO;
using System.Text.RegularExpressions;
using AeroEpubViewer.Epub;
using AeroEpubViewer.Xml;
class WebSource
{
    const string xhtml =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.1//EN""
  ""http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd"">
<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
{0}
</head>
<body>
{1}
</body>
</html>";

    public static TextEpubItemFile KakuyomuEpisode(string url)
    {
        Log.log("[Info]Kakuyomu Episode");
        string raw = GetSource(url);
        string sidebar = GetSource(url + "/episode_sidebar");
        Log.log("[Info]Got text.");
        Regex regex = new Regex("<div class=\"widget-episodeBody js-episode-body[\\s\\S]*?</div>");
        string part = regex.Match(raw).Value;
        XFragment info = XFragment.FindFragment("dl", sidebar);
        string title = info.root.childs[1].innerXHTML;
        string uploadDate = info.root.childs[5].childs[0].tag.GetAttribute("datetime");
        string updateDate = info.root.childs[7].childs[0].tag.GetAttribute("datetime");
        Log.log($"[Info]{title}, Upload:{uploadDate}, Update:{updateDate}");
        string meta = $"    <title>{title}</title>\n    <meta name=\"Source\" content=\"{url}\" />\n    <meta name=\"Upload Date\" content=\"{uploadDate}\"/>\n    <meta name=\"Update Date\" content=\"{updateDate}\"/>";
        return new TextEpubItemFile(Util.FilenameCheck(title) + ".xhtml", string.Format(xhtml, meta, part));
    }
    public static TextEpubItemFile[] KakuyomuWork(string url)
    {
        Log.log("[Info]Kakuyomu Work");
        string raw = GetSource(url);
        string title = (new XFragment(raw, raw.IndexOf("<h1 id=\"workTitle\">"))).root.childs[0].innerXHTML;
        Log.log("[Info]" + title);
        XFragment toc = new XFragment(raw, raw.IndexOf("<div id=\"table-of-contents\">"));
        var list = toc.root.childs[0].childs[1].childs[0];
        TextEpubItemFile[] xhtmls = new TextEpubItemFile[list.childs.Count];
        for (int i = 0; i < xhtmls.Length; i++)
        {
            xhtmls[i] = KakuyomuEpisode("https://kakuyomu.jp" + list.childs[i].childs[0].tag.GetAttribute("href"));
            xhtmls[i].fullName = $"[EP{Util.Number(i + 1)}]" + xhtmls[i].fullName;
        }
        return xhtmls;
    }
    public static TextEpubItemFile[] KakuyomuAuto(string url)
    {
        Uri uri = new Uri(url);
        string[] path = uri.AbsolutePath.Split('/');
        if (path.Length == 5 && path[3] == "episodes")
        {
            Log.log("[Info]Detected: Kakuyomu Single Episode");
            return new TextEpubItemFile[] { KakuyomuEpisode(url) };
        }
        else if (path.Length == 3 && path[1] == "works")
        {
            Log.log("[Info]Detected: Kakuyomu Works");
            return KakuyomuWork(url);
        }
        Log.log("[Warn]No content");
        return null;
    }
    static string GetSource(string url)
    {
        Log.log("[Info]Try dl " + url);
        HttpWebRequest req = HttpWebRequest.CreateHttp(url);
        using (var res = req.GetResponse())
        using (var s = res.GetResponseStream())
        using (var reader = new StreamReader(s))
        {
            return reader.ReadToEnd();
        }
    }
}