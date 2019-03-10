using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ChangeRevision
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Not enough arguments provided. Please, refer to documentation");
                return;
            }

            var buildConfigName = args[0];
            var projectName = args[1];

            var gitFile = Utilities.CheckGit();

            if (string.IsNullOrEmpty(gitFile))
                return;

            try
            {
                var countArguments = @"rev-list master --count";
                var branchArguments = @"branch";

                var countProcess = new Process
                {
                    StartInfo =
                    {
                        WorkingDirectory = Environment.CurrentDirectory,
                        FileName = gitFile,
                        Arguments = countArguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                var branchProcess = new Process()
                {
                    StartInfo =
                    {
                        WorkingDirectory = Environment.CurrentDirectory,
                        FileName = gitFile,
                        Arguments = branchArguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                var branchOutput = Utilities.GetProcessOutput(branchProcess);
                //if (!Utilities.IsOnMaster(branchOutput))
                //{
                //    Console.WriteLine("Not on master, won't change revision.");
                //    return;
                //}

                var versionStringBuilder = Utilities.GetProcessOutput(countProcess);

                var projectDir = new DirectoryInfo(".\\");

                if (projectDir.FullName.Contains($@"bin\{buildConfigName}"))
                    projectDir = projectDir.Parent?.Parent;

                if (projectDir == null)
                    throw new ArgumentException("No project directory found");

                var projectFile = projectDir.GetFiles($@".\{projectName}.csproj").FirstOrDefault();

                var isNewStandard = IsNewStandard(projectFile);

                string newVersion = null;
                if (isNewStandard)
                    newVersion = UpdateProjectFile(buildConfigName, projectFile, versionStringBuilder);
                else
                {
                    var assemblyInfoPath = $@"..\..\..\{projectName}\Properties\AssemblyInfo.cs";
                    newVersion = UpdateAssemblyInfo(buildConfigName, projectName, assemblyInfoPath,
                        versionStringBuilder);
                }

                Console.WriteLine($"Success version increment. New version number is {newVersion}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Version incrementing failed: {ex.Message}");
                Console.WriteLine("");
                Console.WriteLine(ex.StackTrace);
                Console.ReadLine();
            }
        }

        private static string UpdateProjectFile(string buildConfigName, FileInfo projectFile, StringBuilder versionStringBuilder)
        {
            var xDoc = XDocument.Load(projectFile.FullName);

            var assemblyVersionElement = xDoc
                .Descendants()
                .FirstOrDefault(x => x.Name.LocalName == "AssemblyVersion");
            var fileVersionElement = xDoc
                .Descendants()
                .FirstOrDefault(x => x.Name.LocalName == "FileVersion");
            if (assemblyVersionElement == null || fileVersionElement == null)
                throw new Exception("Fill \"Assembly version\" and \"Assembly file Version\" fields first");

            var generatePackageElement = xDoc
                .Descendants()
                .FirstOrDefault(x => x.Name.LocalName == "GeneratePackageOnBuild");
            XElement packageVersionElement = null;
            if (generatePackageElement != null && generatePackageElement.Value == "true")
                packageVersionElement = xDoc
                    .Descendants()
                    .FirstOrDefault(x => x.Name.LocalName == "Version");

            var oldVersion = new Version(assemblyVersionElement.Value);

            var isRelease = buildConfigName == "Release";
            var minor = isRelease ? oldVersion.Minor + 1 : oldVersion.Minor;
            var build = Convert.ToInt16(versionStringBuilder.ToString().Trim());
            var newVersion = new Version(oldVersion.Major, minor, build);
            var newVersionString = isRelease ? newVersion.ToString() : newVersion.ToString() + "-pre";

            assemblyVersionElement.Value = newVersion.ToString();
            fileVersionElement.Value = newVersion.ToString();
            if (packageVersionElement != null)
            {
                packageVersionElement.Value = newVersionString;
                UpdateNuspec(projectFile, newVersionString);
            }

            xDoc.Save(projectFile.FullName);

            return newVersionString;
        }

        private static void UpdateNuspec(FileInfo projectFile, string newVersionString)
        {
            var projectDir = projectFile.Directory;
            var nuspecFile = projectDir?.GetFiles("*.nuspec").FirstOrDefault();
            Console.WriteLine($"debug: {nuspecFile}");
            if (nuspecFile == null)
                return;

            var xDoc = XDocument.Load(nuspecFile.FullName);

            var versionElement = xDoc.Descendants("version").FirstOrDefault();
            if (versionElement == null)
                throw new Exception("Wrong nuspec file format");
            versionElement.Value = newVersionString;

            xDoc.Save(nuspecFile.FullName);
        }

        private static bool IsNewStandard(FileInfo projectFile)
        {
            var xdoc = XDocument.Load(projectFile.FullName);
            if (xdoc == null)
                throw new ArgumentException(nameof(projectFile));

            var oldTargetFrameworkElement = xdoc.Descendants()
                .Where(x => x.Name.LocalName == "TargetFrameworkVersion")
                .ToArray();
            var newTargetFrameworkElement = xdoc.Descendants("TargetFramework")
                .ToArray();

            if (oldTargetFrameworkElement.Any())
                return false;
            if (newTargetFrameworkElement.Any())
                return true;

            throw new ArgumentException(nameof(projectFile));
        }

        private static string UpdateAssemblyInfo(string buildConfigName, string projectName, string assemblyInfoPath, StringBuilder output)
        {
            var text = File.ReadAllText(assemblyInfoPath);

            var match = new Regex("AssemblyVersion\\(\"(.*?)\"\\)").Match(text);
            var ver = new Version(match.Groups[1].Value);
            var minor = buildConfigName == "Release" ? ver.Minor + 1 : ver.Minor;
            var newVer = new Version(ver.Major, minor, Convert.ToInt16(output.ToString().Trim()));

            text = Regex.Replace(text, @"AssemblyVersion\((.*?)\)", "AssemblyVersion(\"" + newVer.ToString() + "\")");
            text = Regex.Replace(text, @"AssemblyFileVersionAttribute\((.*?)\)", "AssemblyFileVersionAttribute(\"" + newVer.ToString() + "\")");
            text = Regex.Replace(text, @"AssemblyFileVersion\((.*?)\)", "AssemblyFileVersion(\"" + newVer.ToString() + "\")");

            File.WriteAllText(assemblyInfoPath, text);
            return newVer.ToString();
        }
    }
}
