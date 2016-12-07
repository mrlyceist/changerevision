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
        /// <summary>
        /// Full path to GIT executable
        /// </summary>
        private static string _gitFile;

        static void Main(string[] args)
        {
            CheckGit();

            if (!CheckGit()) return;
            try
            {
                var countArguments = @"rev-list master --count";
                var branchArguments = @"branch";

                Process countProcess = new Process
                {
                    StartInfo =
                    {
                        WorkingDirectory = Environment.CurrentDirectory,
                        FileName = _gitFile,
                        Arguments = countArguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                Process branchProcess = new Process()
                {
                    StartInfo =
                    {
                        WorkingDirectory = Environment.CurrentDirectory,
                        FileName = _gitFile,
                        Arguments = branchArguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                var branchOutput = GetProcessOutput(branchProcess);
                if (!IsOnMaster(branchOutput))
                {
                    Console.WriteLine("Not on master, won't change revision.");
                    return;
                }

                var output = GetProcessOutput(countProcess);

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
            catch (Exception ex)
            {
                Console.WriteLine($"Version incrementing failed: {ex.Message}");
                Console.WriteLine("");
                Console.WriteLine(ex.StackTrace);
                Console.ReadLine();
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="output"></param>
        /// <returns></returns>
        private static bool IsOnMaster(StringBuilder output)
        {
            return output.ToString().Split('\r').Any(s => s == "* master");
        }

        /// <summary>
        /// Starts <c>process</c> with arguments and reads it's output
        /// </summary>
        /// <param name="process"><c>Process</c> to start</param>
        /// <returns>Output of <c>process</c></returns>
        private static StringBuilder GetProcessOutput(Process process)
        {
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
                    {
                        output.AppendLine(e.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();

                if (!process.WaitForExit(timeout) || !outputWaitHandle.WaitOne(timeout)) return output;
                return output;
            }

        }

        /// <summary>
        /// Looks for GIT in <ui>%PATH%</ui>
        /// </summary>
        /// <returns>If there is GIT client installed</returns>
        private static bool CheckGit()
        {
            string gitPath = CheckPath("\\git", "git.exe");
            if (string.IsNullOrEmpty(gitPath))
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

        /// <summary>
        /// Searches for mention of something in <ui>%PATH%</ui> by dividing it on substrings.
        /// 
        /// </summary>
        /// <param name="searchFor">Keyword</param>
        /// <param name="binaryName">Binary file name</param>
        /// <returns>Full path to the sought binary</returns>
        private static string CheckPath(string searchFor, string binaryName = null)
        {
            string sysPath = string.Empty;
            string subLine = string.Empty;
            try
            {
                sysPath = System.Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine);
            }
            catch (SecurityException ex)
            {
                Console.WriteLine($"Can\'t locate \"{searchFor}\": {ex.Message}");
                Console.ReadLine();
            }
            if (string.IsNullOrEmpty(sysPath)) return null;

            if (string.IsNullOrEmpty(binaryName))
                foreach (
                    var line in
                        from line in sysPath.Split(';')
                        let found = line.ToLower().Contains(searchFor)
                        where found
                        select line)
                    subLine = line;
            else
                foreach (string line in sysPath.Split(';').Where(l => l.ToLower().Contains(searchFor)).Where(line => File.Exists(Path.Combine(line, binaryName))))
                    subLine = line;

            return subLine;
        }
    }
}
