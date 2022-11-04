using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;
public partial class AtxtSource
{
    public string lastModificationTime, lastComment;

    static Regex gitLogParttern = new Regex("^([0-9]{4}-[0-1][0-9]-[0-3][0-9]) ([0-9]{2}:[0-9]{2}:[0-9]{2})(.*)");

    public void GetHistory()
    {
        var gitStatus = GetGitStatus();
        if (GetGitStatus() != GitStatus.OK)
        {
            lastModificationTime = File.GetLastWriteTime(path).ToString("yyyy-MM-dd HH:mm:ss");
            lastComment = "fs | GitStatus: " + gitStatus;
            return;
        }
        ProcessStartInfo gitCmd = new ProcessStartInfo();
        gitCmd.CreateNoWindow = false;
        gitCmd.UseShellExecute = false;
        gitCmd.FileName = "git";
        gitCmd.Arguments = $"log -1 --date=format:\"%Y-%m-%d %H:%M:%S\" --pretty=format:\"%cd %s\" -- \"{filename}\"";
        gitCmd.RedirectStandardOutput = true;
        gitCmd.WorkingDirectory = Path.GetDirectoryName(path);
        gitCmd.StandardOutputEncoding = System.Text.Encoding.UTF8;
        try
        {
            using (Process p = Process.Start(gitCmd))
            {
                p.WaitForExit();
                var gitOutput = p.StandardOutput.ReadToEnd().Trim();
                Match m = gitLogParttern.Match(gitOutput);
                if (m.Success)
                {
                    lastModificationTime = gitOutput.Substring(0, "XXXX-XX-XX 00:00:00".Length);
                    lastComment = "git msg '" + gitOutput.Substring("XXXX-XX-XX 00:00:00".Length).Trim() + "'";
                }

            }
        }
        catch
        {
            Log.Warn("Git应该不会输出别的吧。不行算了。");
        }
    }

    public enum GitStatus
    {
        OK,
        Untracked,
        HasNotStagedChange,
        Unknown
    }

    public GitStatus GetGitStatus()
    {
        var args = $"status -- \"{filename}\"";
        var gitCmd = ProcessStartHelper("git", args);
        using (Process p = Process.Start(gitCmd))
        {
            p.WaitForExit();
            var gitOutput = p.StandardOutput.ReadToEnd().Trim();
            if (gitOutput.Contains("nothing to commit, working tree clean"))
            {
                return GitStatus.OK;
            }
            else if (gitOutput.Contains("Untracked file"))
            {
                return GitStatus.Untracked;
            }
            else if (gitOutput.Contains("Changes not staged for commit:"))
            {
                return GitStatus.HasNotStagedChange;
            }
        }
        throw new Exception("Unknown Git Output");

    }
    string rawCommit = null;

    public void GitVerifyRawUntouchedOnHistory()
    {
        GitVerityFirstCommitRaw();
        if (rawCommit == null) return;

        var args = $"--no-pager log -G^## --pretty=format:\"%h %s\" -- \"{filename}\"";
        var gitCmd = ProcessStartHelper("git", args);
        var flagOK = true;

        using (Process p = Process.Start(gitCmd))
        {
            var gitOutput = p.StandardOutput.ReadToEnd().Trim();
            p.WaitForExit();
            var lines = gitOutput.Split('\n');
            foreach (var line in lines)
            {
                if (!line.StartsWith(rawCommit))
                {
                    Log.Warn($"'{filename}' - '{line}':");
                    GitPrintCommit(line.Substring(0, 7));
                    flagOK = false;
                }
            }
        }
        if (flagOK)
            Log.Info($"'{filename}' has 'raw' init {rawCommit} and is OK.");
    }

    public void GitVerifyRawUntouchedOnUncommitted()
    {
        GitVerityFirstCommitRaw();
        if (rawCommit == null) return;
        var args = $"--no-pager diff -- \"{filename}\"";
        var gitCmd = ProcessStartHelper("git", args);

        using (Process p = Process.Start(gitCmd))
        {
            var gitOutput = p.StandardOutput.ReadToEnd();
            if (gitOutput.Trim() == "")
            {
                Log.Info($"'{filename}': 'raw' init, up-to-date.");
            }
            else
            {
                var r = PrintChangesHelper(gitOutput, "^##", () => Log.Warn($"'{filename}':"));
                if (!r) Log.Info($"'{filename}': 'raw' init, uncommited, OK.");
            }
            p.WaitForExit();
        }
    }
    void GitVerityFirstCommitRaw()
    {
        var args = $"--no-pager log --reverse --date=format:\"%Y-%m-%d %H:%M:%S\" --pretty=format:\"%cd %h %s\" -- \"{filename}\"";
        var gitCmd = ProcessStartHelper("git", args);

        using (Process p = Process.Start(gitCmd))
        {
            p.WaitForExit();
            var gitOutput = p.StandardOutput.ReadToEnd(); //Like 2022-06-17 17:19:08 raw
            var m = gitLogParttern.Match(gitOutput.Trim().Split('\n')[0]);
            if (m.Success)
            {
                var msgHash = m.Groups[3].Value.Trim();

                var hash = msgHash.Substring(0, 7);
                var msg = msgHash.Substring(7);
                if (msg.Contains("raw", StringComparison.OrdinalIgnoreCase))
                {
                    rawCommit = hash;
                }
                else
                {
                    Log.Info($"'{filename}' is tracked, but no raw init.");
                }
            }
            else
            {
                Log.Info($"'{filename}' is not tracked.");
            }

        }
    }

    void GitPrintCommit(string hash, string searchPattern = "^##")
    {
        var args = $"--no-pager show {hash} -- \"{filename}\"";
        var gitCmd = ProcessStartHelper("git", args);

        using (Process p = Process.Start(gitCmd))
        {
            var gitOutput = p.StandardOutput.ReadToEnd();
            PrintChangesHelper(gitOutput, searchPattern);
            p.WaitForExit();
        }
    }

    bool PrintChangesHelper(string gitOutput, string searchPattern, Action aboutToFirstPrint = null)
    {
        bool printAnyThing = false;
        var lines = gitOutput.Split('\n');
        int state = 0;
        var regex = new Regex(searchPattern);
        foreach (var line in lines)
        {
            if (line.Length == 0) continue;
            ConsoleColor c = 0;
            switch (line[0])
            {
                case '@':
                    state = 1;
                    break;
                case ' ':
                    break;
                case '+':
                    c = ConsoleColor.Green;
                    break;
                case '-':
                    c = ConsoleColor.Red;
                    break;
                default: break;
            }
            if (c != 0 && state != 0)
            {
                var m = regex.Match(line.Substring(1));
                if (m.Success)
                {
                    if (!printAnyThing && aboutToFirstPrint != null)
                    {
                        aboutToFirstPrint();
                    }
                    Console.ForegroundColor = c;
                    Console.WriteLine(line);
                    printAnyThing = true;
                }
            }

        }
        return printAnyThing;
    }


    ProcessStartInfo ProcessStartHelper(string filename, string args)
    {
        ProcessStartInfo info = new ProcessStartInfo();
        info.CreateNoWindow = false;
        info.UseShellExecute = false;
        info.FileName = filename;
        info.Arguments = args;
        info.RedirectStandardOutput = true;
        info.WorkingDirectory = Path.GetDirectoryName(path);
        info.StandardOutputEncoding = System.Text.Encoding.UTF8;
        return info;
    }
}