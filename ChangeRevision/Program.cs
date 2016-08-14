using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ChangeRevision
{
    class Program
    {
        private static string _gitFile;

        static void Main(string[] args)
        {
            CheckGit();

            if (!CheckGit()) return;
            try
            {
                Process process = new Process
                {
                    StartInfo =
                    {
                        WorkingDirectory = Environment.CurrentDirectory,
                        FileName = _gitFile,
                        Arguments = @"rev-list master --count",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                StringBuilder output = new StringBuilder();
                const int timeout = 10000;

                using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
                using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
                {
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                            outputWaitHandle.Set();
                        else
                            output.AppendLine(e.Data);
                    };

                    process.Start();
                    process.BeginOutputReadLine();

                    if (!process.WaitForExit(timeout) || !outputWaitHandle.WaitOne(timeout)) return;
                    string text = File.ReadAllText(@"..\..\..\" + args[1] + @"\Properties\AssemblyInfo.cs");

                    Match match = new Regex("AssemblyVersion\\(\"(.*?)\"\\)").Match(text);
                    Version ver = new Version(match.Groups[1].Value);
                    int build = args[0] == "Release" ? ver.Build + 1 : ver.Build;
                    Version newVer = new Version(ver.Major, ver.Minor, build, Convert.ToInt16(output.ToString().Trim()));

                    text = Regex.Replace(text, @"AssemblyVersion\((.*?)\)", "AssemblyVersion(\"" + newVer.ToString() + "\")");
                    text = Regex.Replace(text, @"AssemblyFileVersionAttribute\((.*?)\)", "AssemblyFileVersionAttribute(\"" + newVer.ToString() + "\")");
                    text = Regex.Replace(text, @"AssemblyFileVersion\((.*?)\)", "AssemblyFileVersion(\"" + newVer.ToString() + "\")");

                    File.WriteAllText(@"..\..\..\" + args[1] + @"\Properties\AssemblyInfo.cs", text);
                    Console.WriteLine($"Success version increment. New version number is {newVer}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Version incrementing failed: {ex.Message}");
                Console.WriteLine("");
                Console.WriteLine(ex.StackTrace);
                Console.ReadLine();
            }
        }

        private static bool CheckGit()
        {
            string gitPath = CheckPath("\\git");
            if (gitPath == null)
            {
                Console.WriteLine("No GIT found! Version autoincrement failed");
                Console.ReadLine();
                return false;
            }
            else
            {
                _gitFile = Path.Combine(gitPath, "git.exe");
                return true;
            }
        }

        private static string CheckPath(string searchFor)
        {
            string sysPath = string.Empty;
            string subLine = string.Empty;
            try
            {
                sysPath = System.Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine);
            }
            catch (SecurityException ex)
            {
                Console.WriteLine($"Can\'t locate GIT: {ex.Message}");
                Console.ReadLine();
            }

            foreach (
                var line in
                    from line in sysPath.Split(';')
                    let found = line.ToLower().Contains(searchFor)
                    where found
                    select line)
                subLine = line;
            return subLine;
        }
    }
}
