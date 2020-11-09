using System;
using System.IO;

public class Log
{
    static string t = "";
    public static string level = "";
    static void log(string s)
    {
        t += level + s + "\r\n";
        Console.WriteLine(level + s);
    }
    public static void Warn(string s)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        log("[Warn] " + s);
        Console.ForegroundColor = ConsoleColor.White;
    }
    public static void Note(string s)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        log("[Note] " + s);
        Console.ForegroundColor = ConsoleColor.White;
    }
    public static void Info(string s)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        log("[Info] " + s);
        Console.ForegroundColor = ConsoleColor.White;
    }
    public static void Error(string s)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        log("[Error] " + s);
        Console.ForegroundColor = ConsoleColor.White;
    }
    public static void Save(string path)
    {
        File.WriteAllText(path, t);
    }

}




