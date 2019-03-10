using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;

namespace ChangeRevision
{
    public static class Utilities
    {

        /// <summary>
        /// Looks for GIT in <ui>%PATH%</ui>
        /// </summary>
        /// <returns>If there is GIT client installed</returns>
        public static string CheckGit()
        {
            const string gitExecutable = "git.exe";

            var gitPath = CheckPath("\\git", gitExecutable);

            if (!string.IsNullOrEmpty(gitPath))
                return Path.Combine(gitPath, gitExecutable);

            Console.WriteLine("No GIT found! Version autoincrement failed");
            Console.ReadLine();
            return null;
        }

        /// <summary>
        /// Searches for mention of something in <ui>%PATH%</ui> by dividing it on substrings. 
        /// </summary>
        /// <param name="searchFor">Keyword</param>
        /// <param name="binaryName">Binary file name</param>
        /// <returns>Full path to the sought binary</returns>
        private static string CheckPath(string searchFor, string binaryName = null)
        {
            var sysPath = string.Empty;
            var subLine = string.Empty;

            try
            {
                sysPath = System.Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine);
            }
            catch (SecurityException ex)
            {
                Console.WriteLine($"Can\'t locate \"{searchFor}\": {ex.Message}");
                Console.ReadLine();
            }

            if (string.IsNullOrEmpty(sysPath))
                return null;

            if (string.IsNullOrEmpty(binaryName))
            {
                var paths = sysPath.Split(';')
                    .Select(line => new { line, found = line.ToLower().Contains(searchFor) })
                    .Where(x => x.found)
                    .Select(x => x.line);

                foreach (var line in paths)
                    subLine = line;
            }
            else
            {
                var paths = sysPath.Split(';')
                    .Where(l => l.ToLower().Contains(searchFor))
                    .Where(line => File.Exists(Path.Combine(line, binaryName)));

                foreach (var line in paths)
                    subLine = line;
            }

            return subLine;
        }

        /// <summary>
        /// Starts <c>process</c> with arguments and reads it's output
        /// </summary>
        /// <param name="process"><c>Process</c> to start</param>
        /// <returns>Output of <c>process</c></returns>
        public static StringBuilder GetProcessOutput(Process process)
        {
            var output = new StringBuilder();
            const int timeout = 10000;

            using (var outputWaitHandle = new AutoResetEvent(false))
            using (var errorWaitHandle = new AutoResetEvent(false))
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

                if (!process.WaitForExit(timeout) || !outputWaitHandle.WaitOne(timeout))
                    return output;

                return output;
            }

        }

        public static bool IsOnMaster(StringBuilder output)
        {
            return output.ToString()
                .Split('\r')
                .Any(s => s == "* master");
        }
    }
}