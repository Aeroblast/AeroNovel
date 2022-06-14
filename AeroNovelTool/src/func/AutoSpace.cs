using System.Text;
using System.Collections.Generic;


class AutoSpace
{

    static Rune tagStart = new Rune('['), tagEnd = new Rune(']');

    public static void ProcAtxt(AtxtSource atxt)
    {
        StringBuilder sb = new StringBuilder();
        foreach (var line in atxt.lines)
        {
            if (line.StartsWith("##"))
            {
                sb.AppendLine(line);
                continue;
            }
            bool isInTag = false;
            CodeType last = CodeType.None;
            foreach (var rune in line.EnumerateRunes())
            {
                if (rune == tagStart)
                {
                    isInTag = true;
                    sb.Append(rune);
                    continue;
                }
                if (isInTag)
                {
                    sb.Append(rune);
                    if (rune == tagEnd)
                    {
                        isInTag = false;
                    }
                    continue;
                }
                var current = GetCodeType(rune);
                if (last == CodeType.CJKChar && current == CodeType.HWLetterNumber)
                {
                    sb.Append(' ');
                }
                else if (last == CodeType.HWLetterNumber && current == CodeType.CJKChar)
                {
                    sb.Append(' ');
                }

                sb.Append(rune);
                last = current;
            }
            sb.AppendLine();

        }
        atxt.content = sb.ToString();
    }

    // 也许用作排除可疑字符
    // HW - Half width, FW - Full width
    public enum CodeType
    {
        None,
        HWSpace,
        FWSpace,
        CJKChar,
        CJKChar_DontUse,
        HWLetterNumber,
        HWPunctuation,
        FWLetterNumber,
        FWPunctuation,
        Emoji,
        ManualHandled,
        Unhandled
    }
    const string specialSelection = "※☆—…“”‘’×♡♥";
    const int
    SV_HWSpace = ' ',//
    SV_FWSpace = '　';//U+3000

    static Dictionary<(int, int), CodeType> value2type = new Dictionary<(int, int), CodeType>{

        {(0x4E00,0x9FA5),CodeType.CJKChar}, ////CJK Unified Ideographs
        {(0x3001,0x303E),CodeType.FWPunctuation}, //CJK Symbols and Punctuation U+3000 - U+303F
        {(0x3040,0x30FF),CodeType.CJKChar}, // Hiragana Katagana

        {(0xff01,0xff0F),CodeType.FWPunctuation},// ！
        {(0xff10,0xff19),CodeType.FWLetterNumber},// ０-９
        {(0xff1a,0xff20),CodeType.FWPunctuation},// ：；@
        {(0xff21,0xff3a),CodeType.FWLetterNumber},// A-Z
        {(0xff3b,0xff40),CodeType.FWPunctuation},
        {(0xff41,0xff5a),CodeType.FWLetterNumber},// a-z
        {(0xff5b,0xff65),CodeType.FWPunctuation},

        {(0x21,0x2f),CodeType.HWPunctuation},
        {(0x30,0x39),CodeType.HWLetterNumber},//0-9
        {(0x3A,0x3f),CodeType.HWPunctuation},
        {(0x40,0x5A),CodeType.HWLetterNumber},// @ A-Z
        {(0x5B,0x60),CodeType.HWPunctuation},// [\]_^`
        {(0x61,0x7a),CodeType.HWLetterNumber},//a-z
        {(0x7b,0x7e),CodeType.HWPunctuation},

        {(0x1F300,0x1F5FF),CodeType.Emoji},
        {(0xF900,0xFAFF),CodeType.CJKChar_DontUse},//CJK Compatibility Ideographs 

    };
    public static CodeType GetCodeType(Rune rune)
    {
        int v = rune.Value;
        switch (v)
        {
            case SV_FWSpace: return CodeType.HWSpace;
            case SV_HWSpace: return CodeType.FWSpace;
        }
        if (specialSelection.Contains(rune.ToString()))
        {
            return CodeType.ManualHandled;
        }
        var result = CodeType.Unhandled;
        foreach (var kv in value2type)
        {
            var (rangeStart, rangeEnd) = kv.Key;
            if (v >= rangeStart && v <= rangeEnd)
            {
                result = kv.Value;
            }
        }
        // if (result == CodeType.CJKChar_DontUse)
        //     Log.Warn($"Detect CJK Don't Use: U+{v:X4}【{rune}】");
        // if (result == CodeType.Unhandled)
        //     Log.Warn($"Unhandled rune: U+{v:X4}【{rune}】");
        return result;

    }
}